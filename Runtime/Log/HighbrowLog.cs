using System;
using Highbrow.Core;
using UnityEngine;

namespace Highbrow.Log
{
    /// <summary>
    /// Static convenience facade for Highbrow.Log module.
    /// Provides simple, direct access to the 4 core tracking APIs (Auth, Alive, Purchase, Advertise).
    /// All derived metrics (New User, First Purchase, DAU/DADU) are processed automatically by the Highbrow Collector backend.
    /// </summary>
    public static class HighbrowLog
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoRegister()
        {
            // Pre-registers module into HighbrowSDK registry before scenes load
            HighbrowSDK.RegisterModule(HighbrowLogManager.Instance);
        }

        public static HighbrowLogManager Manager => HighbrowLogManager.Instance;

        /// <summary>
        /// Registers or updates current user context (SUID, AccountID, AccountType, DUID).
        /// </summary>
        public static void SetUserInfo(string suid, string accountId = null, AccountType accountType = AccountType.None, string duid = null)
        {
            Manager.SetUserInfo(suid, accountId, accountType, duid);
        }

        /// <summary>
        /// Clears active user context and stops session heartbeat tracking on user logout.
        /// </summary>
        public static void ClearUser()
        {
            Manager.ClearUser();
        }

        /// <summary>
        /// Explicitly sets and caches the active country code (e.g. from server authentication response).
        /// Must be a 2-letter ISO code (e.g. "KR", "US", "JP").
        /// </summary>
        public static void SetCountry(string countryCode)
        {
            Manager.SetCountry(countryCode);
        }

        /// <summary>
        /// Tracks user authentication / login completion.
        /// Caches SUID, AccountID, AccountType, and DUID for subsequent purchase, ad, and alive logs.
        /// Automatically triggers session alive heartbeat shortly after Auth when AutoSessionTracking is enabled.
        /// </summary>
        public static void TrackAuth(string suid, string accountId, AccountType accountType, string nickname, string duid = null, string result = "OK", string ipAddress = null)
        {
            Manager.TrackAuth(suid, accountId, accountType, nickname, duid, result, ipAddress);
        }

        /// <summary>
        /// Tracks in-app purchase store receipt.
        /// Backend automatically derives First Purchase (New Paying) metrics.
        /// </summary>
        public static void TrackPurchase(string receiptId, float price, string priceId, int productId, string productName, DateTime? purchaseTime = null, string suid = null)
        {
            Manager.TrackPurchase(receiptId, price, priceId, productId, productName, purchaseTime, suid);
        }

        /// <summary>
        /// Tracks advertisement view event.
        /// </summary>
        public static void TrackAd(AdType adType, string customAdTypeName = null, string suid = null)
        {
            Manager.TrackAd(adType, customAdTypeName, suid);
        }

        /// <summary>
        /// Starts periodic session heartbeat tracking (default: 2 minutes / 120 seconds).
        /// Automatically enabled if AutoSessionTracking = true in HighbrowConfig.
        /// </summary>
        public static void StartSessionTracking(float intervalSeconds = 120f)
        {
            Manager.StartSessionTracking(intervalSeconds);
        }

        /// <summary>
        /// Stops session tracking.
        /// </summary>
        public static void StopSessionTracking()
        {
            Manager.StopSessionTracking();
        }

        /// <summary>
        /// Manually triggers offline queue flushing.
        /// </summary>
        public static void FlushOfflineQueue(Action<int> onCompleted = null)
        {
            Manager.FlushOfflineQueue(onCompleted);
        }
    }
}
