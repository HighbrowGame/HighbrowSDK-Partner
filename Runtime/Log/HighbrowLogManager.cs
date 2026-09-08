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
            httpClient = new HighbrowHttpClient(config.HttpTimeoutSeconds, config.DumpHttpPayload);
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
        /// Explicitly sets and caches the active country code (e.g. from server authentication response).
        /// Must be a 2-letter ISO code (e.g. "KR", "US", "JP").
        /// </summary>
        public void SetCountry(string countryCode)
        {
            HighbrowContext.SetCachedCountry(countryCode);
            string current = HighbrowContext.GetCountry();
            HighbrowLogger.Log($"Country code explicitly updated to: '{(string.IsNullOrEmpty(current) ? "Auto-Detect / Server Geo-IP" : current)}'");
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
            try
            {
                if (!EnsureInitialized()) return;

                if (string.IsNullOrWhiteSpace(suid))
                {
                    Debug.LogError("[HighbrowLog] TrackAuth failed: 'suid' must not be null or empty. A valid unique user ID is required.");
                    return;
                }

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
            catch (Exception ex)
            {
                HighbrowLogger.LogError($"[HighbrowLog] Unexpected error in TrackAuth: {ex.Message}");
            }
        }

        #endregion

        #region 2. Purchase (In-App Purchase Log)

        /// <summary>
        /// Tracks in-app purchase store receipt.
        /// Backend automatically derives First Purchase (New Paying) metrics by querying user purchase history in DB.
        /// Requires user to be authenticated via TrackAuth first.
        /// </summary>
        /// <param name="receiptId">Store receipt transaction ID (Apple transactionId / Google orderId).</param>
        /// <param name="price">Product price.</param>
        /// <param name="priceId">Store item identifier.</param>
        /// <param name="currency">ISO 4217 Currency code (e.g. "KRW", "USD", "JPY"). Defaults to "KRW".</param>
        /// <param name="productId">Internal game product numeric ID (optional, default: 0).</param>
        /// <param name="productName">Product name string (optional, default: empty).</param>
        /// <param name="purchaseTime">Purchase timestamp (UTC). Defaults to UtcNow.</param>
        /// <param name="suid">Optional SUID override.</param>
        public void TrackPurchase(string receiptId, float price, string priceId, string currency = "KRW", int productId = 0, string productName = "", DateTime? purchaseTime = null, string suid = null)
        {
            try
            {
                if (!EnsureInitialized()) return;

                string targetSuid = ResolveSuid(suid);
                if (string.IsNullOrEmpty(targetSuid))
                {
                    Debug.LogError("[HighbrowLog] TrackPurchase rejected: User is not authenticated. HighbrowLog.TrackAuth must be called upon login before sending purchase logs.");
                    return;
                }

                string sanitizedReceipt = SanitizeReceiptId(receiptId);
                if (string.IsNullOrEmpty(sanitizedReceipt))
                {
                    return;
                }

                DateTime pTime = purchaseTime ?? DateTime.UtcNow;

                StoreReceiptLog log = new StoreReceiptLog
                {
                    Time = HighbrowContext.GetUtcNowIsoString(),
                    Suid = targetSuid,
                    ReceiptId = sanitizedReceipt,
                    Market = HighbrowContext.GetMarketType(config != null ? config.Market : MarketType.None, config?.CustomMarket),
                    Os = HighbrowContext.GetOsType(),
                    Country = HighbrowContext.GetCountry(config?.CustomCountry),
                    Price = price,
                    Currency = string.IsNullOrWhiteSpace(currency) ? "KRW" : currency.Trim().ToUpperInvariant(),
                    PriceId = priceId ?? string.Empty,
                    ProductId = productId,
                    ProductName = productName ?? string.Empty,
                    PurchaseTime = HighbrowContext.FormatUtcIsoString(pTime),
                    ClientVersion = HighbrowContext.GetClientVersion(config?.ClientVersion),
                    DeviceInfo = HighbrowContext.GetDeviceInfo()
                };

                SendLog(PathLogPurchase, "LogStoreReceipt", JsonUtility.ToJson(log));
            }
            catch (Exception ex)
            {
                HighbrowLogger.LogError($"[HighbrowLog] Unexpected error in TrackPurchase: {ex.Message}");
            }
        }

        /// <summary>
        /// Backwards compatible overload for TrackPurchase with productId as 4th parameter.
        /// </summary>
        public void TrackPurchase(string receiptId, float price, string priceId, int productId, string productName = "", DateTime? purchaseTime = null, string suid = null)
        {
            TrackPurchase(receiptId, price, priceId, "KRW", productId, productName, purchaseTime, suid);
        }

        #endregion

        #region 3. Advertise (Ad Impression Log)

        /// <summary>
        /// Tracks advertisement view event.
        /// Requires user to be authenticated via TrackAuth first.
        /// </summary>
        /// <param name="adType">Ad placement format type.</param>
        /// <param name="customAdTypeName">Optional string representation override of AdType.</param>
        /// <param name="suid">Optional SUID override.</param>
        public void TrackAd(AdType adType, string customAdTypeName = null, string suid = null)
        {
            try
            {
                if (!EnsureInitialized()) return;

                string targetSuid = ResolveSuid(suid);
                if (string.IsNullOrEmpty(targetSuid))
                {
                    Debug.LogError("[HighbrowLog] TrackAd rejected: User is not authenticated. HighbrowLog.TrackAuth must be called upon login before sending ad logs.");
                    return;
                }

                string adTypeName = !string.IsNullOrEmpty(customAdTypeName) ? customAdTypeName : adType.ToString();

                AdLog log = new AdLog
                {
                    Time = HighbrowContext.GetUtcNowIsoString(),
                    Suid = targetSuid,
                    Market = HighbrowContext.GetMarketType(config != null ? config.Market : MarketType.None, config?.CustomMarket),
                    Os = HighbrowContext.GetOsType(),
                    Country = HighbrowContext.GetCountry(config?.CustomCountry),
                    AdType = (int)adType,
                    AdTypeName = adTypeName
                };

                SendLog(PathLogAd, "LogAd", JsonUtility.ToJson(log));
            }
            catch (Exception ex)
            {
                HighbrowLogger.LogError($"[HighbrowLog] Unexpected error in TrackAd: {ex.Message}");
            }
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
            // Do not emit session alive logs if user has not completed authentication (no active SUID)
            if (string.IsNullOrEmpty(currentSuid))
            {
                return;
            }

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
            if (!EnsureInitialized())
            {
                return;
            }

            if (httpClient == null || HighbrowDispatcher.IsQuitting)
            {
                HighbrowLogger.Log($"SDK unavailable for transmission. Caching [{logPath}] into offline queue.");
                offlineQueue?.Enqueue(logPath, jsonPayload);
                return;
            }

            string endpoint = config?.GetEndpointUrl(logPath);
            string appKey = config?.AppKey;

            httpClient.PostJson(endpoint, appKey, logType, jsonPayload, (success, responseCode, response) =>
            {
                if (!success)
                {
                    // 4xx Client Error (e.g. 400 Bad Request, 401 Unauthorized, 403 Forbidden, 404 Not Found)
                    // Permanent client configuration error: Retrying will never succeed and floods server.
                    if (responseCode >= 400 && responseCode < 500)
                    {
                        HighbrowLogger.LogError($"[{logType}] Permanent HTTP {responseCode} error. Dropping log from retry queue. Please verify your AppKey and payload schema.");
                        return;
                    }

                    // Transient error (Network failure, timeout, or 5xx server error): Enqueue for offline retry
                    HighbrowLogger.LogWarning($"Failed to transmit [{logPath}] (HTTP {responseCode}). Enqueueing to PlayerPrefs offline cache.");
                    offlineQueue?.Enqueue(logPath, jsonPayload);
                }
                else
                {
                    HighbrowLogger.Log($"[{logType}] Delivered successfully.");
                }
            });
        }

        private bool EnsureInitialized()
        {
            if (IsInitialized && config != null)
            {
                return true;
            }

            Debug.LogError("[HighbrowSDK] ERROR: HighbrowSDK is not initialized! You must call HighbrowSDK.Initialize() before tracking any logs.");
            return false;
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

                httpClient.PostJson(resolvedEndpoint, config?.AppKey, logTypeHeader, item.JsonPayload, (success, responseCode, res) =>
                {
                    isSuccess = success;
                    // Permanent 4xx error: Drop item so it does not block the FIFO queue
                    if (!success && responseCode >= 400 && responseCode < 500)
                    {
                        HighbrowLogger.LogError($"[HighbrowLog] Permanent HTTP {responseCode} error during queue flush. Dropping invalid item from offline cache.");
                        isSuccess = true; // Mark as processed to remove from queue
                    }
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

            if (!string.IsNullOrEmpty(currentSuid))
            {
                return currentSuid;
            }

            return string.Empty;
        }

        private static string SanitizeReceiptId(string rawReceiptId)
        {
            if (string.IsNullOrWhiteSpace(rawReceiptId))
            {
                Debug.LogError("[HighbrowLog] TrackPurchase failed: 'receiptId' must not be null or empty. Please pass args.purchasedProduct.transactionID.");
                return string.Empty;
            }

            string trimmed = rawReceiptId.Trim();

            // Unwrap outer quotes if present (e.g. "\"{\\\"Store\\\":...}\"")
            if (trimmed.StartsWith("\"") && trimmed.EndsWith("\"") && trimmed.Length >= 2)
            {
                trimmed = trimmed.Substring(1, trimmed.Length - 2).Trim();
            }

            // Unity IAP defensive parsing: If developer passed raw receipt JSON instead of transactionID
            if (trimmed.StartsWith("{") && trimmed.EndsWith("}"))
            {
                try
                {
                    var match = System.Text.RegularExpressions.Regex.Match(
                        trimmed,
                        @"(?:\\*""|\b)(?:TransactionID|transactionId|orderId|order_id|txid|paymentId)(?:\\*"")\s*:\s*\\*""([^""\\]+)",
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase
                    );
                    if (match.Success && !string.IsNullOrEmpty(match.Groups[1].Value))
                    {
                        HighbrowLogger.Log($"[HighbrowLog] Auto-extracted TransactionID '{match.Groups[1].Value}' from raw JSON receipt.");
                        return match.Groups[1].Value;
                    }
                }
                catch { }

                // Do NOT send raw JSON fragments to avoid corrupting Snowflake schema
                Debug.LogError("[HighbrowLog] Invalid receiptId format: The receipt was passed as raw JSON, but failed to extract a transaction ID (orderId/transactionId/txid). Please pass args.purchasedProduct.transactionID directly.");
                return string.Empty;
            }

            // Safe length limit (max 128 chars) to protect Snowflake varchar schema from overflow
            if (trimmed.Length > 128)
            {
                HighbrowLogger.LogWarning($"[HighbrowLog] ReceiptId length ({trimmed.Length}) exceeds 128 characters. Clamping to 128 chars.");
                return trimmed.Substring(0, 128);
            }

            return trimmed;
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
                if (!string.IsNullOrEmpty(currentSuid))
                {
                    HighbrowLogger.Log("App pausing. Triggering session heartbeat and queue flush.");
                    SendUserSessionLog();
                }
                FlushOfflineQueue();
                offlineQueue?.PersistToDisk();
            }
            else
            {
                HighbrowLogger.Log("App resumed. Flushing offline log queue.");
                FlushOfflineQueue();
            }
        }

        private void HandleApplicationQuit()
        {
            if (!string.IsNullOrEmpty(currentSuid))
            {
                HighbrowLogger.Log("App quitting. Caching final session heartbeat.");
                SendUserSessionLog();
            }
            offlineQueue?.PersistToDisk();
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
