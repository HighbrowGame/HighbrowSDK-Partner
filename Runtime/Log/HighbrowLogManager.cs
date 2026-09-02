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
    /// Handles the 4 core fact logs (Auth, Alive, Purchase, Advertise), environment auto-injection,
    /// 2-minute session heartbeat coroutines, UnityWebRequest transmission, and offline PlayerPrefs retry caching.
    /// Derived metrics (New User, First Purchase, DAU) are processed automatically by the Highbrow Collector backend.
    /// </summary>
    public class HighbrowLogManager : IHighbrowModule
    {
        public const string PathLogAuth = "/v1/log/auth";
        public const string PathLogAlive = "/v1/log/alive";
        public const string PathLogPurchase = "/v1/log/purchase";
        public const string PathLogAd = "/v1/log/ad";

        // Legacy alias compatibility
        public const string PathLogStoreReceipt = PathLogPurchase;

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
        private string currentDuid;

        public string CurrentSuid => currentSuid;
        public string CurrentAccountId => currentAccountId;
        public AccountType CurrentAccountType => currentAccountType;
        public string CurrentDuid => currentDuid;

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
            httpClient = new HighbrowHttpClient(dumpHttpPayload: config.DumpHttpPayload);
            offlineQueue = new HighbrowOfflineQueue(config.MaxOfflineQueueSize);

            IsInitialized = true;
            HighbrowLogger.Log("HighbrowLogManager initialized successfully.");

            // Start periodic offline log flush retry coroutine
            if (config.FlushRetryIntervalSeconds > 0)
            {
                flushRetryCoroutine = HighbrowDispatcher.Instance.RunCoroutine(PeriodicFlushCoroutine());
            }

            // Note: Per Haegin requirements, session heartbeat (Alive) is NOT started at Initialize().
            // It automatically starts shortly after successful TrackAuth() when AutoSessionTracking is enabled.

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
        /// Registers or updates the active user context (SUID, AccountID, AccountType, DUID) for subsequent log emissions.
        /// </summary>
        public void SetUserInfo(string suid, string accountId = null, AccountType accountType = AccountType.None, string duid = null)
        {
            currentSuid = suid;
            if (!string.IsNullOrEmpty(accountId)) currentAccountId = accountId;
            if (accountType != AccountType.None) currentAccountType = accountType;
            if (!string.IsNullOrEmpty(duid)) currentDuid = duid;
            else if (string.IsNullOrEmpty(currentDuid)) currentDuid = HighbrowContext.GetDuid();

            HighbrowLogger.Log($"User context updated: SUID={MaskIdentifier(suid)}, AccountID={MaskIdentifier(accountId)}, AccountType={accountType}, DUID={MaskIdentifier(currentDuid)}");
        }

        /// <summary>
        /// Clears active user context and stops alive session tracking. Call on logout / account switch.
        /// </summary>
        public void ClearUser()
        {
            StopSessionTracking();
            currentSuid = null;
            currentAccountId = null;
            currentAccountType = AccountType.None;
            HighbrowLogger.Log("User context cleared.");
        }

        #endregion

        #region 1. Auth (Authentication Log)

        /// <summary>
        /// Tracks user authentication / login completion.
        /// Caches SUID, AccountID, AccountType, and DUID for subsequent purchase, ad, and alive logs.
        /// (Haegin requirement: Automatically triggers session alive heartbeat shortly after Auth).
        /// </summary>
        /// <param name="suid">User unique ID.</param>
        /// <param name="accountId">Platform account ID (e.g. Google sub, Apple user identifier).</param>
        /// <param name="accountType">Account type enum.</param>
        /// <param name="nickname">User nickname.</param>
        /// <param name="duid">Optional custom device unique ID (null defaults to device unique ID).</param>
        /// <param name="result">Authentication result (Default: "OK").</param>
        /// <param name="ipAddress">Optional user IP address.</param>
        public void TrackAuth(string suid, string accountId, AccountType accountType, string nickname, string duid = null, string result = "OK", string ipAddress = null)
        {
            SetUserInfo(suid, accountId, accountType, duid);

            AuthLog log = new AuthLog
            {
                Time = HighbrowContext.GetUtcNowIsoString(),
                AccountType = (int)accountType,
                AccountId = accountId ?? string.Empty,
                Suid = ResolveSuid(suid),
                Duid = ResolveDuid(),
                Market = HighbrowContext.GetMarketType(config != null ? config.Market : MarketType.None, config?.CustomMarket),
                Os = HighbrowContext.GetOsType(),
                Country = HighbrowContext.GetCountry(config?.CustomCountry),
                IpAddress = ipAddress ?? string.Empty,
                Nickname = nickname ?? string.Empty,
                DeviceInfo = HighbrowContext.GetDeviceInfo(),
                Result = result ?? "OK"
            };

            SendLog(PathLogAuth, "LogAuth", JsonUtility.ToJson(log));

            // Haegin requirement: Start Alive session tracking shortly after successful Auth
            if (config != null && config.AutoSessionTracking)
            {
                StartSessionTrackingWithDelay(1f, config.SessionIntervalSeconds > 0 ? config.SessionIntervalSeconds : 120f);
            }
        }

        #endregion

        #region 2. Purchase (In-App Purchase Log)

        /// <summary>
        /// Tracks in-app purchase store receipt.
        /// Backend automatically derives First Purchase (New Paying) metrics by querying user purchase history in DB.
        /// </summary>
        /// <param name="receiptId">Store receipt transaction ID (Apple transactionId / Google orderId).</param>
        /// <param name="price">Product price in USD/local currency standard.</param>
        /// <param name="priceId">Store item identifier.</param>
        /// <param name="productId">Internal game product numeric ID.</param>
        /// <param name="productName">Product name string.</param>
        /// <param name="purchaseTime">Purchase timestamp (UTC). Defaults to UtcNow.</param>
        /// <param name="suid">Optional SUID override.</param>
        public void TrackPurchase(string receiptId, float price, string priceId, int productId, string productName, DateTime? purchaseTime = null, string suid = null)
        {
            DateTime pTime = purchaseTime ?? DateTime.UtcNow;
            string targetSuid = ResolveSuid(suid);

            StoreReceiptLog log = new StoreReceiptLog
            {
                Time = HighbrowContext.GetUtcNowIsoString(),
                Suid = targetSuid,
                ReceiptId = receiptId ?? string.Empty,
                Market = HighbrowContext.GetMarketType(config != null ? config.Market : MarketType.None, config?.CustomMarket),
                Os = HighbrowContext.GetOsType(),
                Country = HighbrowContext.GetCountry(config?.CustomCountry),
                Price = price,
                PriceId = priceId ?? string.Empty,
                ProductId = productId,
                ProductName = productName ?? string.Empty,
                PurchaseTime = HighbrowContext.FormatUtcIsoString(pTime),
                ClientVersion = HighbrowContext.GetClientVersion(config?.ClientVersion),
                DeviceInfo = HighbrowContext.GetDeviceInfo()
            };

            SendLog(PathLogPurchase, "LogStoreReceipt", JsonUtility.ToJson(log));
        }

        #endregion

        #region 3. Advertise (Ad Impression Log)

        /// <summary>
        /// Tracks advertisement view event.
        /// </summary>
        /// <param name="adType">Ad placement format type.</param>
        /// <param name="customAdTypeName">Optional string representation override of AdType.</param>
        /// <param name="suid">Optional SUID override.</param>
        public void TrackAd(AdType adType, string customAdTypeName = null, string suid = null)
        {
            string adTypeName = !string.IsNullOrEmpty(customAdTypeName) ? customAdTypeName : adType.ToString();

            AdLog log = new AdLog
            {
                Time = HighbrowContext.GetUtcNowIsoString(),
                Suid = ResolveSuid(suid),
                Market = HighbrowContext.GetMarketType(config != null ? config.Market : MarketType.None, config?.CustomMarket),
                Os = HighbrowContext.GetOsType(),
                Country = HighbrowContext.GetCountry(config?.CustomCountry),
                AdType = (int)adType,
                AdTypeName = adTypeName
            };

            SendLog(PathLogAd, "LogAd", JsonUtility.ToJson(log));
        }

        #endregion

        #region 4. Alive (Session Heartbeat Tracking)

        /// <summary>
        /// Starts periodic session heartbeat tracking with optional initial delay.
        /// (Haegin requirement: Starts shortly after Auth completion).
        /// </summary>
        /// <param name="initialDelaySeconds">Delay in seconds before the first heartbeat ping.</param>
        /// <param name="intervalSeconds">Interval between subsequent heartbeats in seconds (Default: 120s).</param>
        public void StartSessionTrackingWithDelay(float initialDelaySeconds, float intervalSeconds = 120f)
        {
            StopSessionTracking();

            HighbrowLogger.Log($"Starting session tracking coroutine (Delay: {initialDelaySeconds}s, Interval: {intervalSeconds}s)");
            sessionCoroutine = HighbrowDispatcher.Instance.RunCoroutine(SessionTrackingDelayedRoutine(initialDelaySeconds, intervalSeconds));
        }

        /// <summary>
        /// Starts periodic session heartbeat tracking (default: 2 minutes = 120 seconds).
        /// </summary>
        /// <param name="intervalSeconds">Interval between heartbeats in seconds.</param>
        public void StartSessionTracking(float intervalSeconds = 120f)
        {
            StartSessionTrackingWithDelay(0f, intervalSeconds);
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

        private IEnumerator SessionTrackingDelayedRoutine(float initialDelaySeconds, float intervalSeconds)
        {
            if (initialDelaySeconds > 0)
            {
                yield return new WaitForSecondsRealtime(initialDelaySeconds);
            }

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
            UserSessionLog log = new UserSessionLog
            {
                Time = HighbrowContext.GetUtcNowIsoString(),
                AccountType = (int)currentAccountType,
                Suid = ResolveSuid(null),
                Duid = ResolveDuid(),
                Market = HighbrowContext.GetMarketType(config != null ? config.Market : MarketType.None, config?.CustomMarket),
                Os = HighbrowContext.GetOsType(),
                Country = HighbrowContext.GetCountry(config?.CustomCountry)
            };

            SendLog(PathLogAlive, "LogAlive", JsonUtility.ToJson(log));
        }

        #endregion

        #region Network Transmission & Offline Retry

        private void SendLog(string logPath, string logType, string jsonPayload)
        {
            if (!IsInitialized || httpClient == null || HighbrowDispatcher.IsQuitting)
            {
                HighbrowLogger.Log($"SDK unavailable for transmission. Caching [{logPath}] into offline queue.");
                offlineQueue?.Enqueue(logPath, jsonPayload);
                return;
            }

            string endpoint = config?.GetEndpointUrl(logPath);
            string appKey = config?.AppKey;

            httpClient.PostJson(endpoint, appKey, logType, jsonPayload, (success, response) =>
            {
                if (!success)
                {
                    HighbrowLogger.LogWarning($"Failed to transmit [{logPath}]. Enqueueing to PlayerPrefs offline cache.");
                    offlineQueue.Enqueue(logPath, jsonPayload);
                }
                else
                {
                    HighbrowLogger.Log($"[{logPath}] Delivered successfully.");
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

                string resolvedEndpoint = ResolveEndpointFromQueuedLog(item.LogType);
                string logTypeHeader = ExtractLogTypeName(item.LogType);

                httpClient.PostJson(resolvedEndpoint, config?.AppKey, logTypeHeader, item.JsonPayload, (success, res) =>
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

        private string ResolveEndpointFromQueuedLog(string logTypeOrPath)
        {
            if (string.IsNullOrEmpty(logTypeOrPath))
            {
                return config?.GetResolvedLogEndpointUrl();
            }

            if (logTypeOrPath.StartsWith("/") ||
                logTypeOrPath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                logTypeOrPath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return config?.GetEndpointUrl(logTypeOrPath);
            }

            // Backwards compatibility for legacy logType strings
            switch (logTypeOrPath)
            {
                case "LogAuth":
                    return config?.GetEndpointUrl(PathLogAuth);
                case "LogAlive":
                    return config?.GetEndpointUrl(PathLogAlive);
                case "LogStoreReceipt":
                case "LogPurchase":
                    return config?.GetEndpointUrl(PathLogPurchase);
                case "LogAd":
                case "LogAdvertisement":
                    return config?.GetEndpointUrl(PathLogAd);
                default:
                    return config?.GetResolvedLogEndpointUrl();
            }
        }

        private string ExtractLogTypeName(string logTypeOrPath)
        {
            if (string.IsNullOrEmpty(logTypeOrPath)) return string.Empty;

            switch (logTypeOrPath)
            {
                case PathLogAuth:
                    return "LogAuth";
                case PathLogAlive:
                    return "LogAlive";
                case PathLogPurchase:
                    return "LogStoreReceipt";
                case PathLogAd:
                    return "LogAd";
                default:
                    return logTypeOrPath;
            }
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

        private string ResolveDuid()
        {
            if (!string.IsNullOrEmpty(currentDuid))
            {
                return currentDuid;
            }

            currentDuid = HighbrowContext.GetDuid();
            return currentDuid;
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
            HighbrowLogger.Log("App quitting. Caching final session heartbeat.");
            SendUserSessionLog();
        }

        private static string MaskIdentifier(string val)
        {
            if (string.IsNullOrEmpty(val)) return "[EMPTY]";
            if (val.Length <= 4) return "***";
            return val.Substring(0, 2) + "***" + val.Substring(val.Length - 2);
        }

        #endregion
    }
}
