using System;
using UnityEngine;

namespace Highbrow.Core
{
    /// <summary>
    /// ScriptableObject asset for configuring Highbrow SDK via the Unity Inspector.
    /// Asset is loaded from Resources: "Highbrow/HighbrowSettings".
    /// </summary>
    [CreateAssetMenu(fileName = "HighbrowSettings", menuName = "Highbrow/Highbrow Settings Asset", order = 100)]
    public class HighbrowSettings : ScriptableObject
    {
        public const string ResourcePath = "Highbrow/HighbrowSettings";

        [Header("Authentication & Environment")]
        [Tooltip("Issued Application Key / Project Key for authentication.")]
        [SerializeField] private string appKey = string.Empty;

        [Tooltip("When true, routes logs to the Sandbox/Testing collector (DEV mode). When false, routes to Production.")]
        [SerializeField] private bool useSandbox = false;

        [Tooltip("Server region code (Default: 'kr').")]
        [SerializeField] private string region = "kr";

        [Header("Platform & Store")]
        [Tooltip("Target distribution market. If None, auto-resolves based on runtime platform.")]
        [SerializeField] private MarketType market = MarketType.None;

        [Tooltip("Optional 2-letter ISO Country code override (e.g. 'KR', 'US'). Leave empty to auto-detect.")]
        [SerializeField] private string customCountry = string.Empty;

        [Header("Module Controls")]
        [Tooltip("Enable or disable the Log module upon SDK initialization.")]
        [SerializeField] private bool enableLog = true;

        [Tooltip("Enable or disable the Ad module upon SDK initialization.")]
        [SerializeField] private bool enableAd = true;

        [Header("Session Tracking & Network")]
        [Tooltip("Automatically send periodic Alive/Session heartbeat logs.")]
        [SerializeField] private bool autoSessionTracking = true;

        [Tooltip("Session heartbeat interval in seconds (Default: 120s = 2 minutes).")]
        [SerializeField] private float sessionIntervalSeconds = 120f;

        [Tooltip("HTTP request timeout in seconds (Default: 10s, safe range 3s - 30s).")]
        [SerializeField] private int httpTimeoutSeconds = 10;

        [Header("Debugging")]
        [Tooltip("Enable detailed SDK internal logging in the Unity console.")]
        [SerializeField] private bool debugMode = false;

        [Tooltip("Dump HTTP request/response headers and JSON payloads to the console (DebugMode only).")]
        [SerializeField] private bool dumpHttpPayload = false;

        // Public accessors
        public string AppKey { get => appKey; set => appKey = value; }
        public bool UseSandbox { get => useSandbox; set => useSandbox = value; }
        public string Region { get => region; set => region = value; }
        public MarketType Market { get => market; set => market = value; }
        public string CustomCountry { get => customCountry; set => customCountry = value; }
        public bool EnableLog { get => enableLog; set => enableLog = value; }
        public bool EnableAd { get => enableAd; set => enableAd = value; }
        public bool AutoSessionTracking { get => autoSessionTracking; set => autoSessionTracking = value; }
        public float SessionIntervalSeconds { get => sessionIntervalSeconds; set => sessionIntervalSeconds = value; }
        public int HttpTimeoutSeconds { get => httpTimeoutSeconds; set => httpTimeoutSeconds = value; }
        public bool DebugMode { get => debugMode; set => debugMode = value; }
        public bool DumpHttpPayload { get => dumpHttpPayload; set => dumpHttpPayload = value; }

        /// <summary>
        /// Converts the ScriptableObject settings to a runtime HighbrowConfig instance.
        /// </summary>
        public HighbrowConfig ToConfig()
        {
            return new HighbrowConfig
            {
                AppKey = appKey,
                UseSandbox = useSandbox,
                Region = string.IsNullOrEmpty(region) ? "kr" : region,
                Market = market,
                CustomCountry = string.IsNullOrWhiteSpace(customCountry) ? null : customCountry.Trim().ToUpperInvariant(),
                EnableLog = enableLog,
                EnableAd = enableAd,
                AutoSessionTracking = autoSessionTracking,
                SessionIntervalSeconds = sessionIntervalSeconds,
                HttpTimeoutSeconds = httpTimeoutSeconds,
                DebugMode = debugMode,
                DumpHttpPayload = dumpHttpPayload
            };
        }

        /// <summary>
        /// Loads the HighbrowSettings asset from Resources/Highbrow/HighbrowSettings.
        /// Returns null if not found.
        /// </summary>
        public static HighbrowSettings LoadSettings()
        {
            return Resources.Load<HighbrowSettings>(ResourcePath);
        }
    }
}
