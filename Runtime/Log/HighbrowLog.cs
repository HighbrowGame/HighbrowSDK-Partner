using System;
using Highbrow.Core;
using UnityEngine;

namespace Highbrow.Log
{
    /// <summary>
    /// Static convenience facade for Highbrow.Log module.
    /// Provides simple, direct access to tracking APIs across game scripts.
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
        /// Registers or updates current user context.
        /// </summary>
        public static void SetUserInfo(string suid, string accountId = null, AccountType accountType = AccountType.None, DateTime? userCreateTime = null)
        {
            Manager.SetUserInfo(suid, accountId, accountType, userCreateTime);
        }

        /// <summary>
        /// Tracks user authentication / login completion.
        /// </summary>
        public static void TrackAuth(string suid, string accountId, AccountType accountType, string nickname, string result = "OK", string ipAddress = null)
        {
            Manager.TrackAuth(suid, accountId, accountType, nickname, result, ipAddress);
        }

        /// <summary>
        /// Tracks new user / character creation.
        /// </summary>
        public static void TrackNewUser(string suid = null, AccountType? accountType = null)
        {
            Manager.TrackNewUser(suid, accountType);
        }

        /// <summary>
        /// Starts periodic session heartbeat tracking (default: 5 minutes / 300 seconds).
        /// </summary>
        public static void StartSessionTracking(float intervalSeconds = 300f)
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
        /// Tracks in-app purchase store receipt.
        /// </summary>
        public static void TrackPurchase(string receiptId, float price, string priceId, int productId, string productName, DateTime? purchaseTime = null, bool isFirstPurchase = false, string suid = null)
        {
            Manager.TrackPurchase(receiptId, price, priceId, productId, productName, purchaseTime, isFirstPurchase, suid);
        }

        /// <summary>
        /// Tracks first purchase log when an account makes their very first IAP purchase.
        /// </summary>
        public static void TrackFirstPurchase(int productId, DateTime? purchaseTime = null, string suid = null)
        {
            Manager.TrackFirstPurchase(productId, purchaseTime, suid);
        }

        /// <summary>
        /// Tracks advertisement view lifecycle.
        /// </summary>
        public static void TrackAd(AdType adType, bool isComplete = true, bool userAdSkipPackage = false, string customAdTypeName = null, string suid = null)
        {
            Manager.TrackAd(adType, isComplete, userAdSkipPackage, customAdTypeName, suid);
        }

        /// <summary>
        /// Tracks daily active unique user (DAU SUID) log recorded on daily date transition or market change.
        /// </summary>
        public static void TrackDailyActiveUserSuid(DateTime lastActiveTime, DateTime? userCreateTime = null, string suid = null)
        {
            Manager.TrackDailyActiveUserSuid(lastActiveTime, userCreateTime, suid);
        }

        /// <summary>
        /// Tracks daily active unique device (DAU DUID) log recorded on daily date transition.
        /// </summary>
        public static void TrackDailyActiveUserDuid(DateTime? userCreateTime = null, bool? isNewDuid = null, string duid = null)
        {
            Manager.TrackDailyActiveUserDuid(userCreateTime, isNewDuid, duid);
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
