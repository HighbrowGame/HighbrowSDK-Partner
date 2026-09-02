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
        /// Resolves Market type integer code.
        /// Prioritizes developer-specified config.Market or customMarket, fallback to runtime platform.
        /// 1: AppleStore, 2: GooglePlay, 3: Steam, 4: OneStore, 5: SamsungStore, 6: VngWeb, 0: None
        /// </summary>
        public static int GetMarketType(MarketType configMarket = MarketType.None, int? customMarket = null)
        {
            if (configMarket != MarketType.None)
            {
                return (int)configMarket;
            }

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
                    return 2; // GooglePlay default fallback
                case RuntimePlatform.WindowsPlayer:
                case RuntimePlatform.WindowsEditor:
                    return 3; // Steam default fallback
                default:
                    return 0; // None
            }
        }

        /// <summary>
        /// Resolves 2-letter ISO Country code (e.g. "KR", "US", "JP", "GB", "TW").
        /// Extracts accurate country code from OS RegionInfo or CultureInfo locale without language guessing.
        /// If undetectable, returns empty string so the collector server Geo-IP can inject the true IP country.
        /// </summary>
        public static string GetCountry(string customCountry = null)
        {
            if (!string.IsNullOrEmpty(customCountry))
            {
                return customCountry.ToUpperInvariant();
            }

            // 1. Try .NET RegionInfo (OS Device Region Setting)
            try
            {
                RegionInfo currentRegion = RegionInfo.CurrentRegion;
                if (currentRegion != null && !string.IsNullOrEmpty(currentRegion.TwoLetterISORegionName) && currentRegion.TwoLetterISORegionName.Length == 2)
                {
                    return currentRegion.TwoLetterISORegionName.ToUpperInvariant();
                }
            }
            catch
            {
                // Fallback to CultureInfo
            }

            // 2. Try CultureInfo.CurrentCulture ("ko-KR", "en-US", "zh-TW", "en-GB", etc.)
            try
            {
                CultureInfo currentCulture = CultureInfo.CurrentCulture;
                if (currentCulture != null && !string.IsNullOrEmpty(currentCulture.Name))
                {
                    string[] parts = currentCulture.Name.Split('-');
                    if (parts.Length > 1 && parts[parts.Length - 1].Length == 2)
                    {
                        return parts[parts.Length - 1].ToUpperInvariant();
                    }
                }
            }
            catch
            {
                // Unresolvable locale
            }

            // 3. Fallback: Return empty string to let collector server inject accurate Country from IP header (Cloudflare / CloudFront Geo-IP)
            return string.Empty;
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
