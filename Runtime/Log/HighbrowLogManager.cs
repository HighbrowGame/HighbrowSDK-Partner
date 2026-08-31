using System;
using System.Collections;
using System.Collections.Generic;
using Highbrow.Core;
using Highbrow.Core.Network;
using Highbrow.Core.Utils;
using UnityEngine;

namespace Highbrow.Log
{
    /// <summary>
    /// Core Log Module Manager for Highbrow SDK.
    /// Handles log generation, auto-injection of environment metadata, session heartbeat coroutines,
    /// HTTP transmission via UnityWebRequest, and offline PlayerPrefs retry caching.
    /// </summary>
    public class HighbrowLogManager : IHighbrowModule
    {
        private const string PrefsFirstLaunchKey = "HIGHBROW_SDK_FIRST_LAUNCH_FLAG";
        private static HighbrowLogManager instance;

        public static HighbrowLogManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new HighbrowLogManager();
                    HighbrowSDK.RegisterModule(instance);
                }
                return instance;
            }
        }

        public string ModuleName => "HighbrowLog";
        public bool IsInitialized { get; private set; }

        private HighbrowConfig config;
        private HighbrowHttpClient httpClient;
        private HighbrowOfflineQueue offlineQueue;

        private Coroutine sessionCoroutine;
        private Coroutine flushRetryCoroutine;

        private string currentSuid;
        private string currentAccountId;
        private AccountType currentAccountType = AccountType.None;
        private DateTime? userCreateTimeUtc;
        private DateTime lastActiveTimeUtc = DateTime.UtcNow;
        private bool isNewDuidCached;

        public string CurrentSuid => currentSuid;
        public string CurrentAccountId => currentAccountId;
        public AccountType CurrentAccountType => currentAccountType;

        public HighbrowLogManager()
        {
            instance = this;
        }

        #region IHighbrowModule Lifecycle

        public void Initialize(HighbrowConfig sdkConfig)
        {
            if (IsInitialized)
            {
                HighbrowLogger.LogWarning("HighbrowLogManager is already initialized.");
                return;
            }

            config = sdkConfig ?? new HighbrowConfig();
            httpClient = new HighbrowHttpClient();
            offlineQueue = new HighbrowOfflineQueue(config.MaxOfflineQueueSize);

            // Determine if this is a new DUID (First launch)
            if (!PlayerPrefs.HasKey(PrefsFirstLaunchKey))
            {
                isNewDuidCached = true;
                PlayerPrefs.SetInt(PrefsFirstLaunchKey, 1);
                PlayerPrefs.Save();
            }
            else
            {
                isNewDuidCached = false;
            }

            IsInitialized = true;
            HighbrowLogger.Log("HighbrowLogManager initialized successfully.");

            // Start periodic offline log flush retry coroutine
            if (config.FlushRetryIntervalSeconds > 0)
            {
                flushRetryCoroutine = HighbrowDispatcher.Instance.RunCoroutine(PeriodicFlushCoroutine());
            }

            // Start auto session tracking if configured
            if (config.AutoSessionTracking)
            {
                StartSessionTracking(config.SessionIntervalSeconds);
            }

            // Hook application pause/quit events
            HighbrowDispatcher.OnPauseStateChanged += HandlePauseStateChanged;
            HighbrowDispatcher.OnQuitTriggered += HandleApplicationQuit;

            // Trigger initial queue flush
            FlushOfflineQueue();
        }

        public void Shutdown()
        {
            if (!IsInitialized) return;

            StopSessionTracking();

            if (flushRetryCoroutine != null)
            {
                HighbrowDispatcher.Instance.TerminateCoroutine(flushRetryCoroutine);
                flushRetryCoroutine = null;
            }

            HighbrowDispatcher.OnPauseStateChanged -= HandlePauseStateChanged;
            HighbrowDispatcher.OnQuitTriggered -= HandleApplicationQuit;

            IsInitialized = false;
            HighbrowLogger.Log("HighbrowLogManager shutdown complete.");
        }

        #endregion

        #region User Context Configuration

        /// <summary>
        /// Registers or updates the active user context for subsequent log emissions.
        /// </summary>
        public void SetUserInfo(string suid, string accountId = null, AccountType accountType = AccountType.None, DateTime? userCreateTime = null)
        {
            currentSuid = suid;
            if (!string.IsNullOrEmpty(accountId)) currentAccountId = accountId;
            if (accountType != AccountType.None) currentAccountType = accountType;
            if (userCreateTime.HasValue) userCreateTimeUtc = userCreateTime.Value;

            HighbrowLogger.Log($"User context updated: SUID={suid}, AccountID={accountId}, AccountType={accountType}");
        }

        #endregion

        #region Public Tracking APIs

        /// <summary>
        /// Tracks user authentication / login completion.
        /// </summary>
        /// <param name="suid">User unique ID.</param>
        /// <param name="accountId">Platform account ID (e.g. Google sub, Apple user identifier).</param>
        /// <param name="accountType">Account type enum.</param>
        /// <param name="nickname">User nickname.</param>
        /// <param name="result">Authentication result (Default: "OK").</param>
        /// <param name="ipAddress">Optional user IP address.</param>
        public void TrackAuth(string suid, string accountId, AccountType accountType, string nickname, string result = "OK", string ipAddress = null)
        {
            SetUserInfo(suid, accountId, accountType);

            AuthLog log = new AuthLog
            {
                Time = HighbrowContext.GetUtcNowIsoString(),
                Suid = ResolveSuid(suid),
                Nickname = nickname ?? string.Empty,
                Duid = HighbrowContext.GetDuid(config?.CustomDuid),
                Market = HighbrowContext.GetMarketType(config?.CustomMarket),
                Os = HighbrowContext.GetOsType(),
                Country = HighbrowContext.GetCountry(config?.CustomCountry),
                IpAddress = ipAddress ?? string.Empty,
                AccountType = (int)accountType,
                AccountId = accountId ?? string.Empty,
                Result = result ?? "OK",
                DeviceInfo = HighbrowContext.GetDeviceInfo(),
                ClientVersion = HighbrowContext.GetClientVersion(config?.CustomClientVersion),
                LastActiveTime = HighbrowContext.FormatUtcIsoString(lastActiveTimeUtc),
                IsNewDuid = isNewDuidCached,
                Region = ResolveRegion()
            };

            lastActiveTimeUtc = DateTime.UtcNow;
            SendLog("LogAuth", JsonUtility.ToJson(log));
        }

        /// <summary>
        /// Tracks new user registration / character creation.
        /// </summary>
        /// <param name="suid">User unique ID (optional if previously set).</param>
        /// <param name="accountType">Optional account type override.</param>
        public void TrackNewUser(string suid = null, AccountType? accountType = null)
        {
            AccountType targetAccountType = accountType ?? currentAccountType;
            string targetSuid = ResolveSuid(suid);

            NewUserLog log = new NewUserLog
            {
                Time = HighbrowContext.GetUtcNowIsoString(),
                Suid = targetSuid,
                Duid = HighbrowContext.GetDuid(config?.CustomDuid),
                Market = HighbrowContext.GetMarketType(config?.CustomMarket),
                Os = HighbrowContext.GetOsType(),
                Country = HighbrowContext.GetCountry(config?.CustomCountry),
                AccountType = (int)targetAccountType,
                IsNewDuid = isNewDuidCached,
                Region = ResolveRegion()
            };

            SendLog("LogNewUser", JsonUtility.ToJson(log));
        }

        /// <summary>
        /// Tracks in-app purchase store receipt.
        /// Automatically fires TrackFirstPurchase if isFirstPurchase is true.
        /// </summary>
        /// <param name="receiptId">Store receipt transaction ID (Apple transactionId / Google orderId).</param>
        /// <param name="price">Product price in USD/local currency standard.</param>
        /// <param name="priceId">Store item identifier.</param>
        /// <param name="productId">Internal game product numeric ID.</param>
        /// <param name="productName">Product name string.</param>
        /// <param name="purchaseTime">Purchase timestamp (UTC). Defaults to UtcNow.</param>
        /// <param name="isFirstPurchase">Whether this transaction is the user's first purchase.</param>
        /// <param name="suid">Optional SUID override.</param>
        public void TrackPurchase(string receiptId, float price, string priceId, int productId, string productName, DateTime? purchaseTime = null, bool isFirstPurchase = false, string suid = null)
        {
            DateTime pTime = purchaseTime ?? DateTime.UtcNow;
            string targetSuid = ResolveSuid(suid);

            StoreReceiptLog log = new StoreReceiptLog
            {
                Time = HighbrowContext.GetUtcNowIsoString(),
                Suid = targetSuid,
                ReceiptId = receiptId ?? string.Empty,
                Market = HighbrowContext.GetMarketType(config?.CustomMarket),
                Os = HighbrowContext.GetOsType(),
                Country = HighbrowContext.GetCountry(config?.CustomCountry),
                Price = price,
                PriceId = priceId ?? string.Empty,
                ProductId = productId,
                ProductName = productName ?? string.Empty,
                PurchaseTime = HighbrowContext.FormatUtcIsoString(pTime),
                ClientVersion = HighbrowContext.GetClientVersion(config?.CustomClientVersion),
                DeviceInfo = HighbrowContext.GetDeviceInfo(),
                Region = ResolveRegion()
            };

            SendLog("LogStoreReceipt", JsonUtility.ToJson(log));

            if (isFirstPurchase)
            {
                TrackFirstPurchase(productId, pTime, targetSuid);
            }
        }

        /// <summary>
        /// Tracks first purchase log when an account makes their very first IAP purchase.
        /// </summary>
        public void TrackFirstPurchase(int productId, DateTime? purchaseTime = null, string suid = null)
        {
            DateTime pTime = purchaseTime ?? DateTime.UtcNow;

            NewPayingLog log = new NewPayingLog
            {
                Time = HighbrowContext.GetUtcNowIsoString(),
                Suid = ResolveSuid(suid),
                Market = HighbrowContext.GetMarketType(config?.CustomMarket),
                Os = HighbrowContext.GetOsType(),
                Country = HighbrowContext.GetCountry(config?.CustomCountry),
                PurchaseTime = HighbrowContext.FormatUtcIsoString(pTime),
                ProductId = productId,
                Region = ResolveRegion()
            };

            SendLog("LogNewPaying", JsonUtility.ToJson(log));
        }

        /// <summary>
        /// Tracks advertisement view lifecycle (impression start or completion).
        /// </summary>
        /// <param name="adType">Ad placement format type.</param>
        /// <param name="isComplete">True for completed view (reward eligible), False for view start.</param>
        /// <param name="userAdSkipPackage">Whether user active ad-skip subscription is used.</param>
        /// <param name="customAdTypeName">Optional string representation override of AdType.</param>
        /// <param name="suid">Optional SUID override.</param>
        public void TrackAd(AdType adType, bool isComplete, bool userAdSkipPackage = false, string customAdTypeName = null, string suid = null)
        {
            string adTypeName = !string.IsNullOrEmpty(customAdTypeName) ? customAdTypeName : adType.ToString();

            AdLog log = new AdLog
            {
                Time = HighbrowContext.GetUtcNowIsoString(),
                Suid = ResolveSuid(suid),
                Duid = HighbrowContext.GetDuid(config?.CustomDuid),
                Market = HighbrowContext.GetMarketType(config?.CustomMarket),
                Os = HighbrowContext.GetOsType(),
                Country = HighbrowContext.GetCountry(config?.CustomCountry),
                AdType = (int)adType,
                AdTypeName = adTypeName,
                IsComplete = isComplete,
                UserAdSkipPackage = userAdSkipPackage,
                Region = ResolveRegion()
            };

            SendLog("LogAd", JsonUtility.ToJson(log));
        }

        #endregion

        #region Session Heartbeat Tracking

        /// <summary>
        /// Starts periodic session heartbeat tracking (default: 5 minutes = 300 seconds).
        /// </summary>
        /// <param name="intervalSeconds">Interval between heartbeats in seconds.</param>
        public void StartSessionTracking(float intervalSeconds = 300f)
        {
            StopSessionTracking();

            HighbrowLogger.Log($"Starting session tracking coroutine (Interval: {intervalSeconds}s)");
            sessionCoroutine = HighbrowDispatcher.Instance.RunCoroutine(SessionTrackingRoutine(intervalSeconds));
        }

        /// <summary>
        /// Stops active session heartbeat tracking.
        /// </summary>
        public void StopSessionTracking()
        {
            if (sessionCoroutine != null)
            {
                HighbrowDispatcher.Instance.TerminateCoroutine(sessionCoroutine);
                sessionCoroutine = null;
                HighbrowLogger.Log("Session tracking stopped.");
            }
        }

        private IEnumerator SessionTrackingRoutine(float intervalSeconds)
        {
            // Initial session ping
            SendUserSessionLog();

            while (true)
            {
                yield return new WaitForSecondsRealtime(intervalSeconds);
                SendUserSessionLog();
            }
        }

        private void SendUserSessionLog()
        {
            DateTime createTime = userCreateTimeUtc ?? DateTime.UtcNow;

            UserSessionLog log = new UserSessionLog
            {
                Time = HighbrowContext.GetUtcNowIsoString(),
                AccountType = (int)currentAccountType,
                Suid = ResolveSuid(null),
                Duid = HighbrowContext.GetDuid(config?.CustomDuid),
                Market = HighbrowContext.GetMarketType(config?.CustomMarket),
                Os = HighbrowContext.GetOsType(),
                Country = HighbrowContext.GetCountry(config?.CustomCountry),
                UserCreateTime = HighbrowContext.FormatUtcIsoString(createTime),
                Region = ResolveRegion()
            };

            SendLog("LogAlive", JsonUtility.ToJson(log));
        }

        #endregion

        #region Network Transmission & Offline Retry

        private void SendLog(string logType, string jsonPayload)
        {
            if (!IsInitialized || httpClient == null)
            {
                HighbrowLogger.LogWarning($"SDK not initialized. Caching [{logType}] into offline queue.");
                offlineQueue?.Enqueue(logType, jsonPayload);
                return;
            }

            string endpoint = config?.GetResolvedLogEndpointUrl();
            string appKey = config?.AppKey;

            httpClient.PostJson(endpoint, appKey, logType, jsonPayload, (success, response) =>
            {
                if (!success)
                {
                    HighbrowLogger.LogWarning($"Failed to transmit [{logType}]. Enqueueing to PlayerPrefs offline cache.");
                    offlineQueue.Enqueue(logType, jsonPayload);
                }
                else
                {
                    HighbrowLogger.Log($"[{logType}] Delivered successfully.");
                }
            });
        }

        private IEnumerator PeriodicFlushCoroutine()
        {
            while (true)
            {
                yield return new WaitForSecondsRealtime(config.FlushRetryIntervalSeconds);
                FlushOfflineQueue();
            }
        }

        /// <summary>
        /// Attempts to flush cached logs from PlayerPrefs queue to the server.
        /// </summary>
        public void FlushOfflineQueue(Action<int> onCompleted = null)
        {
            if (offlineQueue == null || offlineQueue.Count == 0)
            {
                onCompleted?.Invoke(0);
                return;
            }

            HighbrowDispatcher.Instance.RunCoroutine(FlushQueueRoutine(onCompleted));
        }

        private IEnumerator FlushQueueRoutine(Action<int> onCompleted)
        {
            var batch = offlineQueue.PeekBatch(20);
            if (batch == null || batch.Count == 0)
            {
                onCompleted?.Invoke(0);
                yield break;
            }

            HighbrowLogger.Log($"Flushing offline queue: {batch.Count} logs pending.");
            int successCount = 0;

            for (int i = 0; i < batch.Count; i++)
            {
                var item = batch[i];
                bool isDone = false;
                bool isSuccess = false;

                httpClient.PostJson(config?.GetResolvedLogEndpointUrl(), config?.AppKey, item.LogType, item.JsonPayload, (success, res) =>
                {
                    isSuccess = success;
                    isDone = true;
                });

                while (!isDone)
                {
                    yield return null;
                }

                if (isSuccess)
                {
                    successCount++;
                }
                else
                {
                    // If network is still failing, abort current flush cycle
                    HighbrowLogger.LogWarning("Network error during queue flush. Pausing retry batch.");
                    break;
                }
            }

            if (successCount > 0)
            {
                offlineQueue.RemoveProcessed(successCount);
            }

            onCompleted?.Invoke(successCount);
        }

        #endregion

        #region Helpers & Lifecycle Callbacks

        private string ResolveSuid(string explicitSuid)
        {
            if (!string.IsNullOrEmpty(explicitSuid))
            {
                return explicitSuid;
            }
            return !string.IsNullOrEmpty(currentSuid) ? currentSuid : string.Empty;
        }

        private string ResolveRegion()
        {
            return !string.IsNullOrEmpty(config?.Region) ? config.Region : "dev";
        }

        private void HandlePauseStateChanged(bool isPaused)
        {
            if (isPaused)
            {
                HighbrowLogger.Log("App pausing. Triggering session heartbeat and queue flush.");
                SendUserSessionLog();
                FlushOfflineQueue();
            }
        }

        private void HandleApplicationQuit()
        {
            HighbrowLogger.Log("App quitting. Flushing pending offline logs.");
            SendUserSessionLog();
        }

        #endregion
    }
}
