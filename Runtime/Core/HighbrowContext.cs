using System;
using System.Globalization;
using Highbrow.Core.Utils;
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
        private static string cachedCountry;
        private static int? cachedOsType;
        private static string cachedDeviceInfo;
        private static string cachedClientVersion;
        private static int? cachedMarketType;
        private static bool isPreWarmed = false;

        /// <summary>
        /// Pre-warms and caches immutable device, OS, and platform metadata on the Unity main thread.
        /// Prevents Unity main-thread exceptions when log tracking methods are invoked from background/worker threads.
        /// </summary>
        public static void PreWarm(HighbrowConfig config = null)
        {
            try
            {
                if (!isPreWarmed)
                {
                    cachedDuid = GetDuid();
                    cachedOsType = ResolvePlatformOsType();
                    cachedDeviceInfo = ResolvePlatformDeviceInfo();
                    cachedClientVersion = ResolvePlatformClientVersion(config?.ClientVersion);
                    cachedMarketType = ResolvePlatformMarket(config != null ? config.Market : MarketType.None, config?.CustomMarket);
                    string country = GetCountry(config?.CustomCountry);
                    if (!string.IsNullOrEmpty(country))
                    {
                        cachedCountry = country;
                    }
                    isPreWarmed = true;
                    HighbrowLogger.Log("[HighbrowContext] Pre-warmed metadata successfully for cross-thread safety.");
                }
            }
            catch (Exception ex)
            {
                HighbrowLogger.LogWarning($"[HighbrowContext] PreWarm warning: {ex.Message}");
            }
        }

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
        /// Safe for background threads if pre-warmed.
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

            try
            {
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
            catch
            {
                // Fallback for background thread invocation if not pre-warmed
                return Guid.NewGuid().ToString("N");
            }
        }

        /// <summary>
        /// Auto-detects OS type integer code based on Unity RuntimePlatform.
        /// 1: iOS, 2: Android, 3: OSX, 4: Windows, 5: Linux, 6: PC, 0: None
        /// </summary>
        public static int GetOsType()
        {
            if (cachedOsType.HasValue)
            {
                return cachedOsType.Value;
            }

            int os = ResolvePlatformOsType();
            cachedOsType = os;
            return os;
        }

        private static int ResolvePlatformOsType()
        {
            try
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
            catch
            {
                return 6;
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

            if (cachedMarketType.HasValue)
            {
                return cachedMarketType.Value;
            }

            int market = ResolvePlatformMarket(configMarket, customMarket);
            cachedMarketType = market;
            return market;
        }

        private static int ResolvePlatformMarket(MarketType configMarket = MarketType.None, int? customMarket = null)
        {
            if (configMarket != MarketType.None)
            {
                return (int)configMarket;
            }

            if (customMarket.HasValue && customMarket.Value > 0)
            {
                return customMarket.Value;
            }

#if ONESTORE || ONE_STORE
            return 4; // OneStore
#elif SAMSUNG || SAMSUNG_STORE || GALAXY_STORE
            return 5; // SamsungStore
#elif STEAM
            return 3; // Steam
#endif

            try
            {
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
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Manually sets and caches the user country code (e.g. from game server authentication).
        /// Must be a 2-letter ISO country code (e.g. "KR", "US", "JP").
        /// </summary>
        public static void SetCachedCountry(string countryCode)
        {
            if (!string.IsNullOrEmpty(countryCode))
            {
                string trimmed = countryCode.Trim().ToUpperInvariant();
                if (trimmed.Length == 2 && char.IsLetter(trimmed[0]) && char.IsLetter(trimmed[1]))
                {
                    cachedCountry = trimmed;
                    return;
                }
            }

            cachedCountry = null;
        }

        /// <summary>
        /// Clears the cached country code, allowing re-detection.
        /// </summary>
        public static void ClearCachedCountry()
        {
            cachedCountry = null;
        }

        /// <summary>
        /// Resolves 2-letter ISO Country code (e.g. "KR", "US", "JP", "GB", "TW").
        /// Prioritizes developer override, cached code, Android OS native Locale JNI, .NET RegionInfo,
        /// CultureInfo (CurrentCulture/CurrentUICulture), and safe Unity SystemLanguage mapping.
        /// If undetectable, returns empty string so the collector server Geo-IP can inject the true IP country.
        /// </summary>
        public static string GetCountry(string customCountry = null)
        {
            if (!string.IsNullOrEmpty(customCountry))
            {
                string trimmed = customCountry.Trim().ToUpperInvariant();
                if (trimmed.Length == 2 && char.IsLetter(trimmed[0]) && char.IsLetter(trimmed[1]))
                {
                    return trimmed;
                }
            }

            if (!string.IsNullOrEmpty(cachedCountry))
            {
                return cachedCountry;
            }

            string resolved = ResolveSystemCountry();
            if (!string.IsNullOrEmpty(resolved))
            {
                cachedCountry = resolved;
                return cachedCountry;
            }

            return string.Empty;
        }

        private static string ResolveSystemCountry()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            // 1. Android OS Native Locale JNI (100% reliable on Android IL2CPP)
            try
            {
                using (var localeClass = new AndroidJavaClass("java.util.Locale"))
                using (var defaultLocale = localeClass.CallStatic<AndroidJavaObject>("getDefault"))
                {
                    string country = defaultLocale.Call<string>("getCountry");
                    if (!string.IsNullOrEmpty(country) && country.Length == 2 && char.IsLetter(country[0]) && char.IsLetter(country[1]))
                    {
                        return country.ToUpperInvariant();
                    }
                }
            }
            catch (Exception ex)
            {
                HighbrowLogger.LogWarning($"[HighbrowContext] Android native Locale check failed: {ex.Message}");
            }
#endif

            // 2. .NET RegionInfo (OS Device Region Setting)
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
                // Managed stripping fallback
            }

            // 3. CultureInfo (CurrentCulture, CurrentUICulture, InstalledUICulture) BCP-47 tags
            CultureInfo[] candidateCultures = { CultureInfo.CurrentCulture, CultureInfo.CurrentUICulture, CultureInfo.InstalledUICulture };
            foreach (var culture in candidateCultures)
            {
                if (culture == null || string.IsNullOrEmpty(culture.Name)) continue;
                try
                {
                    string[] parts = culture.Name.Split(new[] { '-', '_' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 1 && parts[parts.Length - 1].Length == 2)
                    {
                        string candidate = parts[parts.Length - 1].ToUpperInvariant();
                        if (char.IsLetter(candidate[0]) && char.IsLetter(candidate[1]))
                        {
                            return candidate;
                        }
                    }
                }
                catch { }
            }

            // 4. Safe Unity SystemLanguage mapping (only for unambiguous 1:1 language-to-country mappings)
            switch (Application.systemLanguage)
            {
                case SystemLanguage.Korean: return "KR";
                case SystemLanguage.Japanese: return "JP";
                case SystemLanguage.ChineseSimplified: return "CN";
                case SystemLanguage.ChineseTraditional: return "TW";
                case SystemLanguage.Vietnamese: return "VN";
                case SystemLanguage.Thai: return "TH";
                case SystemLanguage.Indonesian: return "ID";
                case SystemLanguage.Russian: return "RU";
                case SystemLanguage.Turkish: return "TR";
                default:
                    // Multi-country languages (English, Spanish, etc.) defer to server Geo-IP to avoid skewing metrics
                    return string.Empty;
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

            if (!string.IsNullOrEmpty(cachedClientVersion))
            {
                return cachedClientVersion;
            }

            string version = ResolvePlatformClientVersion(customVersion);
            cachedClientVersion = version;
            return version;
        }

        private static string ResolvePlatformClientVersion(string customVersion = null)
        {
            if (!string.IsNullOrEmpty(customVersion))
            {
                return customVersion;
            }

            try
            {
                string version = Application.version;
                return string.IsNullOrEmpty(version) ? "1.0.0" : version;
            }
            catch
            {
                return "1.0.0";
            }
        }

        /// <summary>
        /// Formats hardware and OS device info string (e.g. "Samsung SM-G996N | Android OS 14").
        /// </summary>
        public static string GetDeviceInfo()
        {
            if (!string.IsNullOrEmpty(cachedDeviceInfo))
            {
                return cachedDeviceInfo;
            }

            string info = ResolvePlatformDeviceInfo();
            cachedDeviceInfo = info;
            return info;
        }

        private static string ResolvePlatformDeviceInfo()
        {
            try
            {
                string deviceModel = SystemInfo.deviceModel;
                string os = SystemInfo.operatingSystem;
                return $"{deviceModel} | {os}";
            }
            catch
            {
                return "Unknown | Unknown";
            }
        }
    }
}
