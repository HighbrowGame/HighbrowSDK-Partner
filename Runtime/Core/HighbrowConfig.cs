using System;

namespace Highbrow.Core
{
    /// <summary>
    /// Global configuration container for Highbrow SDK.
    /// Provides 2-tier automatic endpoint routing (Sandbox vs Production).
    /// </summary>
    [Serializable]
    public class HighbrowConfig
    {
        public const string DefaultProductionEndpoint = "https://log-api.highbrow-inc.com/v1/collect";
        public const string DefaultSandboxEndpoint = "https://sandbox-log-api.highbrow-inc.com/v1/collect";

        /// <summary>
        /// Issued Application Key / Project Key for authentication.
        /// </summary>
        public string AppKey = string.Empty;

        /// <summary>
        /// When true, routes logs to the Sandbox/Testing collector (DEV mode).
        /// When false (default), routes logs to the live Production collector (PROD mode).
        /// </summary>
        public bool UseSandbox = false;

        /// <summary>
        /// Server region code (e.g. "kr", "us", "eu", "dev", "qa", "liveqa").
        /// Default is "kr".
        /// </summary>
        public string Region = "kr";

        /// <summary>
        /// Optional custom collector URL override.
        /// If not set, automatically resolves to Sandbox or Production URL based on UseSandbox.
        /// </summary>
        public string CustomLogEndpointUrl = null;

        /// <summary>
        /// Server mode string representation ("DEV" when UseSandbox is true, "PROD" otherwise).
        /// </summary>
        public string ServerMode
        {
            get => UseSandbox ? "DEV" : "PROD";
            set => UseSandbox = string.Equals(value, "DEV", StringComparison.OrdinalIgnoreCase) ||
                               string.Equals(value, "SANDBOX", StringComparison.OrdinalIgnoreCase) ||
                               string.Equals(value, "QA", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Enable or disable the Log module upon SDK initialization.
        /// </summary>
        public bool EnableLog = true;

        /// <summary>
        /// Enable or disable the Ad module upon SDK initialization.
        /// </summary>
        public bool EnableAd = false;

        /// <summary>
        /// Enable or disable the GameCenter module upon SDK initialization.
        /// </summary>
        public bool EnableGameCenter = false;

        /// <summary>
        /// Whether to automatically start 5-minute alive/session tracking when Log module is initialized.
        /// </summary>
        public bool AutoSessionTracking = true;

        /// <summary>
        /// Interval in seconds for session tracking logs (Default: 300s = 5 minutes).
        /// </summary>
        public float SessionIntervalSeconds = 300f;

        /// <summary>
        /// Maximum number of logs cached in PlayerPrefs when offline or on network failure.
        /// </summary>
        public int MaxOfflineQueueSize = 300;

        /// <summary>
        /// Interval in seconds to automatically retry flushing offline cached logs.
        /// </summary>
        public float FlushRetryIntervalSeconds = 30f;

        /// <summary>
        /// Enable detailed SDK internal logging to Unity console.
        /// </summary>
        public bool DebugMode = false;

        /// <summary>
        /// Custom DUID override. If null or empty, SystemInfo.deviceUniqueIdentifier is used.
        /// </summary>
        public string CustomDuid = null;

        /// <summary>
        /// Custom Country code override (2-letter ISO, e.g., "KR", "US").
        /// If null or empty, detected from system/region.
        /// </summary>
        public string CustomCountry = null;

        /// <summary>
        /// Custom Market type override. If null, auto-detected from runtime platform.
        /// </summary>
        public int? CustomMarket = null;

        /// <summary>
        /// Custom client version override. If null or empty, Application.version is used.
        /// </summary>
        public string CustomClientVersion = null;

        /// <summary>
        /// Resolves active log collector endpoint URL based on UseSandbox and CustomLogEndpointUrl.
        /// </summary>
        public string GetResolvedLogEndpointUrl()
        {
            if (!string.IsNullOrEmpty(CustomLogEndpointUrl))
            {
                return CustomLogEndpointUrl;
            }

            return UseSandbox ? DefaultSandboxEndpoint : DefaultProductionEndpoint;
        }

        public HighbrowConfig() { }

        public HighbrowConfig(string appKey, bool useSandbox = false, string region = "kr")
        {
            AppKey = appKey;
            UseSandbox = useSandbox;
            Region = region;
        }
    }
}
