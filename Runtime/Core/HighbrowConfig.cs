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
        public const string DefaultProductionBaseUrl = "https://log-api.highbrow-inc.com";
        public const string DefaultSandboxBaseUrl = "https://sandbox-log-api.highbrow-inc.com";

        // Legacy compatibility constants
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
        /// Optional custom collector base URL override (e.g. "https://custom-log.domain.com").
        /// If not set, automatically resolves to Sandbox or Production base URL based on UseSandbox.
        /// </summary>
        public string CustomLogBaseUrl = null;

        /// <summary>
        /// Optional custom collector URL override (Legacy).
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
        public bool EnableAd = true;

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
        /// Target distribution market / store (e.g. GooglePlay, OneStore, AppleStore, Steam).
        /// Recommended to set dynamically via preprocessor symbols.
        /// If MarketType.None, auto-resolves based on runtime platform.
        /// </summary>
        public MarketType Market = MarketType.None;

        /// <summary>
        /// Custom Market type override (integer). If null, config.Market or auto-detected platform is used.
        /// </summary>
        public int? CustomMarket = null;

        /// <summary>
        /// Game client application version string (e.g. "1.0.0", "1.2.34").
        /// If null or empty, Application.version is automatically used.
        /// </summary>
        public string ClientVersion = null;

        /// <summary>
        /// Legacy alias for ClientVersion.
        /// </summary>
        public string CustomClientVersion
        {
            get => ClientVersion;
            set => ClientVersion = value;
        }

        /// <summary>
        /// Resolves active log collector base URL based on UseSandbox, CustomLogBaseUrl, or CustomLogEndpointUrl.
        /// </summary>
        public string GetResolvedBaseUrl()
        {
            if (!string.IsNullOrEmpty(CustomLogBaseUrl))
            {
                return CustomLogBaseUrl.TrimEnd('/');
            }

            if (!string.IsNullOrEmpty(CustomLogEndpointUrl))
            {
                return CustomLogEndpointUrl.TrimEnd('/');
            }

            return UseSandbox ? DefaultSandboxBaseUrl : DefaultProductionBaseUrl;
        }

        /// <summary>
        /// Resolves full endpoint URL for a given relative path (e.g. "/v1/log/auth").
        /// </summary>
        public string GetEndpointUrl(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
            {
                return GetResolvedLogEndpointUrl();
            }

            if (relativePath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                relativePath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return relativePath;
            }

            string baseUrl = GetResolvedBaseUrl();
            return $"{baseUrl}/{relativePath.TrimStart('/')}";
        }

        /// <summary>
        /// Resolves active log collector endpoint URL (Legacy default).
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
