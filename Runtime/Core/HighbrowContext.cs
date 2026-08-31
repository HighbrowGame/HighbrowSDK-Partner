using System;
using System.Globalization;
using UnityEngine;

namespace Highbrow.Core
{
    /// <summary>
    /// Auto-injection context providing device, OS, time, and environment information
    /// for Highbrow log payloads.
    /// </summary>
    public static class HighbrowContext
    {
        private const string PrefsDuidKey = "HIGHBROW_SDK_SAVED_DUID";
        private static string cachedDuid;

        /// <summary>
        /// Returns current UTC timestamp formatted as ISO 8601 string (e.g. 2026-08-31T11:29:44.123456Z).
        /// </summary>
        public static string GetUtcNowIsoString()
        {
            return DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.ffffffZ", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Formats a given DateTime to ISO 8601 UTC string.
        /// </summary>
        public static string FormatUtcIsoString(DateTime dateTime)
        {
            DateTime utc = dateTime.Kind == DateTimeKind.Utc ? dateTime : dateTime.ToUniversalTime();
            return utc.ToString("yyyy-MM-ddTHH:mm:ss.ffffffZ", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Gets device unique identifier with fallback and caching.
        /// </summary>
        public static string GetDuid(string customDuid = null)
        {
            if (!string.IsNullOrEmpty(customDuid))
            {
                return customDuid;
            }

            if (!string.IsNullOrEmpty(cachedDuid))
            {
                return cachedDuid;
            }

            string duid = SystemInfo.deviceUniqueIdentifier;
            if (string.IsNullOrEmpty(duid) || duid == SystemInfo.unsupportedIdentifier)
            {
                duid = PlayerPrefs.GetString(PrefsDuidKey, string.Empty);
                if (string.IsNullOrEmpty(duid))
                {
                    duid = Guid.NewGuid().ToString("N");
                    PlayerPrefs.SetString(PrefsDuidKey, duid);
                    PlayerPrefs.Save();
                }
            }

            cachedDuid = duid;
            return cachedDuid;
        }

        /// <summary>
        /// Auto-detects OS type integer code based on Unity RuntimePlatform.
        /// 1: iOS, 2: Android, 3: OSX, 4: Windows, 5: Linux, 6: PC, 0: None
        /// </summary>
        public static int GetOsType()
        {
            switch (Application.platform)
            {
                case RuntimePlatform.IPhonePlayer:
                    return 1; // iOS
                case RuntimePlatform.Android:
                    return 2; // Android
                case RuntimePlatform.OSXPlayer:
                case RuntimePlatform.OSXEditor:
                    return 3; // OSX
                case RuntimePlatform.WindowsPlayer:
                case RuntimePlatform.WindowsEditor:
                    return 4; // Windows
                case RuntimePlatform.LinuxPlayer:
                case RuntimePlatform.LinuxEditor:
                    return 5; // Linux
                default:
                    return 6; // PC / Other
            }
        }

        /// <summary>
        /// Auto-detects Market type integer code.
        /// 1: AppleStore, 2: GooglePlay, 3: Steam, 4: OneStore, 5: SamsungStore, 6: VngWeb, 0: None
        /// </summary>
        public static int GetMarketType(int? customMarket = null)
        {
            if (customMarket.HasValue && customMarket.Value > 0)
            {
                return customMarket.Value;
            }

            switch (Application.platform)
            {
                case RuntimePlatform.IPhonePlayer:
                case RuntimePlatform.OSXPlayer:
                    return 1; // AppleStore
                case RuntimePlatform.Android:
                    return 2; // GooglePlay default
                case RuntimePlatform.WindowsPlayer:
                case RuntimePlatform.WindowsEditor:
                    return 3; // Steam default for Windows
                default:
                    return 0; // None
            }
        }

        /// <summary>
        /// Gets 2-letter ISO Country code (e.g. "KR", "US").
        /// </summary>
        public static string GetCountry(string customCountry = null)
        {
            if (!string.IsNullOrEmpty(customCountry))
            {
                return customCountry.ToUpperInvariant();
            }

            try
            {
                RegionInfo currentRegion = RegionInfo.CurrentRegion;
                if (currentRegion != null && !string.IsNullOrEmpty(currentRegion.TwoLetterISORegionName))
                {
                    return currentRegion.TwoLetterISORegionName.ToUpperInvariant();
                }
            }
            catch
            {
                // Fallback to language-based detection
            }

            switch (Application.systemLanguage)
            {
                case SystemLanguage.Korean:
                    return "KR";
                case SystemLanguage.Japanese:
                    return "JP";
                case SystemLanguage.Chinese:
                case SystemLanguage.ChineseSimplified:
                case SystemLanguage.ChineseTraditional:
                    return "CN";
                case SystemLanguage.Vietnamese:
                    return "VN";
                case SystemLanguage.German:
                    return "DE";
                case SystemLanguage.French:
                    return "FR";
                case SystemLanguage.Spanish:
                    return "ES";
                case SystemLanguage.Russian:
                    return "RU";
                case SystemLanguage.English:
                default:
                    return "US";
            }
        }

        /// <summary>
        /// Gets client version string.
        /// </summary>
        public static string GetClientVersion(string customVersion = null)
        {
            if (!string.IsNullOrEmpty(customVersion))
            {
                return customVersion;
            }

            string version = Application.version;
            return string.IsNullOrEmpty(version) ? "1.0.0" : version;
        }

        /// <summary>
        /// Formats hardware and OS device info string (e.g. "Samsung SM-G996N | Android OS 14").
        /// </summary>
        public static string GetDeviceInfo()
        {
            string deviceModel = SystemInfo.deviceModel;
            string os = SystemInfo.operatingSystem;
            return $"{deviceModel} | {os}";
        }
    }
}
