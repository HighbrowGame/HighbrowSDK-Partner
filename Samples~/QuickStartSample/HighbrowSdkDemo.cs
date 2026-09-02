using System;
using Highbrow.Core;
using Highbrow.Log;
using UnityEngine;

namespace Highbrow.Samples
{
    /// <summary>
    /// Quick-start sample demonstrating Highbrow SDK integration.
    /// The client SDK only needs to emit 4 core fact logs:
    /// 1. Auth (Login)
    /// 2. Alive (Session Heartbeat - Automated)
    /// 3. Purchase (IAP)
    /// 4. Advertise (Ad Impression)
    /// Derived metrics (New User, First Purchase, DAU) are calculated automatically on the Highbrow Collector backend.
    /// </summary>
    public class HighbrowSdkDemo : MonoBehaviour
    {
        [Header("SDK Settings")]
        [SerializeField] private string appKey = "SAMPLE_HIGHBROW_APP_KEY";
        [SerializeField] private bool useSandbox = true; // true: Sandbox/DEV, false: Production

        private void Start()
        {
            // Resolve target market dynamically (OneStore, GooglePlay, AppleStore, Steam)
            MarketType targetMarket = MarketType.None;
#if UNITY_IOS
            targetMarket = MarketType.AppleStore;
#elif UNITY_ANDROID
            #if ONESTORE
            targetMarket = MarketType.OneStore;
            #else
            targetMarket = MarketType.GooglePlay;
            #endif
#elif UNITY_STANDALONE_WIN
            targetMarket = MarketType.Steam;
#endif

            // 1. Initialize Highbrow SDK (2-tier automatic routing: Sandbox vs Production)
            HighbrowConfig config = new HighbrowConfig
            {
                AppKey = appKey,
                Market = targetMarket, // Dynamically resolved store
                UseSandbox = useSandbox, // Set false for live release
                EnableLog = true,
                AutoSessionTracking = true, // 5-min session heartbeat (Alive) starts automatically
                SessionIntervalSeconds = 300f,
                DebugMode = true
            };

            HighbrowSDK.Initialize(config);

            Debug.Log($"[HighbrowSdkDemo] Highbrow SDK Initialized (BaseUrl: {config.GetResolvedBaseUrl()})");
        }

        // 1. Called on user login success (Google, Apple, Guest, etc.)
        public void OnUserLoginSuccess(string suid, string accountId, AccountType accountType, string nickname)
        {
            HighbrowLog.TrackAuth(
                suid: suid,
                accountId: accountId,
                accountType: accountType,
                nickname: nickname,
                result: "OK"
            );
        }

        // 2. Called on in-app purchase success (Unity IAP ProcessPurchase, etc.)
        public void OnPurchaseSuccess(string receiptId, float price, string priceId, int productId, string productName)
        {
            HighbrowLog.TrackPurchase(
                receiptId: receiptId,
                price: price,
                priceId: priceId,
                productId: productId,
                productName: productName,
                purchaseTime: DateTime.UtcNow
            );
        }

        // 3. Called on ad view impression
        public void OnAdViewed(AdType adType)
        {
            HighbrowLog.TrackAd(adType);
        }
    }
}
