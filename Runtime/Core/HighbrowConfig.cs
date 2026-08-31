using System;

namespace Highbrow.Core
{
    /// <summary>
    /// Global configuration container for Highbrow SDK.
    /// </summary>
    [Serializable]
    public class HighbrowConfig
    {
        /// <summary>
        /// Issued Application Key / Project Key for authentication.
        /// </summary>
        public string AppKey = string.Empty;

        /// <summary>
        /// Server execution environment mode (DEV, QA, PROD).
        /// Default is "DEV".
        /// </summary>
        public string ServerMode = "DEV";

        /// <summary>
        /// Server region code (e.g. "kr", "us", "eu", "dev", "qa", "liveqa").
        /// Default is "dev".
        /// </summary>
        public string Region = "dev";

        /// <summary>
        /// Base URL / endpoint for Highbrow log collector backend.
        /// </summary>
        public string LogEndpointUrl = "https://log-api.highbrow-inc.com/v1/collect";

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

        public HighbrowConfig() { }

        public HighbrowConfig(string appKey, string serverMode = "DEV", string region = "dev")
        {
            AppKey = appKey;
            ServerMode = serverMode;
            Region = region;
        }
    }
}
