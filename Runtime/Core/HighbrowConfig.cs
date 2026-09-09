using System;
using Highbrow.Core.Utils;
using UnityEngine;

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
        /// Whether to automatically start 2-minute alive/session tracking when Log module is initialized.
        /// </summary>
        public bool AutoSessionTracking = true;

        /// <summary>
        /// Interval in seconds for session tracking logs (Default: 120s = 2 minutes).
        /// </summary>
        public float SessionIntervalSeconds = 120f;

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
        /// When DebugMode is enabled, dumps HTTP request and response headers and payloads to the Unity console.
        /// Contains sensitive values and must remain disabled outside local debugging.
        /// </summary>
        public bool DumpHttpPayload = false;

        /// <summary>
        /// HTTP request timeout in seconds for log transmissions.
        /// Automatically clamped between 3 and 30 seconds for network safety (Default: 10s).
        /// </summary>
        public int HttpTimeoutSeconds = 10;

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
                return CustomLogBaseUrl.Trim().TrimEnd('/');
            }

            if (!string.IsNullOrEmpty(CustomLogEndpointUrl))
            {
                return CustomLogEndpointUrl.Trim().TrimEnd('/');
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

        /// <summary>
        /// Validates and normalizes configuration values.
        /// Clamps network timeouts and sanitizes custom country codes.
        /// </summary>
        /// <param name="errorMessage">Output error message if validation fails.</param>
        /// <returns>True if configuration is valid to proceed; otherwise false.</returns>
        public bool Validate(out string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(AppKey))
            {
                errorMessage = "AppKey is missing or empty. A valid AppKey issued by Highbrow is required.";
                return false;
            }

            // Normalize and validate CustomCountry (2-letter ISO)
            if (!string.IsNullOrEmpty(CustomCountry))
            {
                string trimmed = CustomCountry.Trim().ToUpperInvariant();
                if (trimmed.Length == 2 && char.IsLetter(trimmed[0]) && char.IsLetter(trimmed[1]))
                {
                    CustomCountry = trimmed;
                }
                else
                {
                    HighbrowLogger.LogWarning($"[HighbrowConfig] Invalid CustomCountry '{CustomCountry}'. Must be a 2-letter ISO code (e.g. 'KR', 'US'). Resetting to auto-detect.");
                    CustomCountry = null;
                }
            }

            // Safe clamp for HTTP timeout (3s ~ 30s)
            HttpTimeoutSeconds = Mathf.Clamp(HttpTimeoutSeconds, 3, 30);

            // Safe bounds for session and queue configurations
            if (SessionIntervalSeconds <= 0f) SessionIntervalSeconds = 120f;
            if (MaxOfflineQueueSize <= 0) MaxOfflineQueueSize = 300;
            if (FlushRetryIntervalSeconds <= 0f) FlushRetryIntervalSeconds = 30f;

            errorMessage = null;
            return true;
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
