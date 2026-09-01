using System;
using Highbrow.Core;
using Highbrow.Log;
using UnityEngine;

namespace Highbrow.Samples
{
    /// <summary>
    /// Quick-start sample demonstrating SDK initialization, user authentication,
    /// session tracking, in-app purchase logging, and ad tracking.
    /// </summary>
    public class HighbrowSdkDemo : MonoBehaviour
    {
        [Header("SDK Settings")]
        [SerializeField] private string appKey = "SAMPLE_HIGHBROW_APP_KEY";
        [SerializeField] private bool useSandbox = true; // true: Sandbox/DEV, false: Production
        [SerializeField] private string region = "kr";

        private void Start()
        {
            // 1. Initialize Highbrow SDK (2-tier automatic routing: Sandbox vs Production)
            HighbrowConfig config = new HighbrowConfig
            {
                AppKey = appKey,
                UseSandbox = useSandbox, // Set false for live release
                Region = region,
                EnableLog = true,
                AutoSessionTracking = true,
                SessionIntervalSeconds = 300f, // 5 minutes
                DebugMode = true
            };

            HighbrowSDK.Initialize(config);

            Debug.Log($"[HighbrowSdkDemo] Highbrow SDK Initialized (Endpoint: {config.GetResolvedLogEndpointUrl()})");
        }

        // Example: Called when user logs in via Google/Apple/Guest
        public void OnUserLoginSuccess(string suid, string accountId, AccountType accountType, string nickname)
        {
            // 2. Track Authentication Log
            HighbrowLog.TrackAuth(
                suid: suid,
                accountId: accountId,
                accountType: accountType,
                nickname: nickname,
                result: "OK"
            );

            // If user is brand new (character created)
            bool isBrandNewUser = false; // Replace with your game's check
            if (isBrandNewUser)
            {
                HighbrowLog.TrackNewUser(suid, accountType);
            }
        }

        // Example: Called when IAP purchase succeeds
        public void OnPurchaseSuccess(string receiptId, float price, string priceId, int productId, string productName, bool isFirstPurchase)
        {
            // 3. Track Purchase Log
            HighbrowLog.TrackPurchase(
                receiptId: receiptId,
                price: price,
                priceId: priceId,
                productId: productId,
                productName: productName,
                purchaseTime: DateTime.UtcNow,
                isFirstPurchase: isFirstPurchase
            );
        }

        // Example: Called when Ad is viewed
        public void OnAdViewed(AdType adType, bool isCompleted, bool hasSkipPackage)
        {
            // 4. Track Ad View Log
            HighbrowLog.TrackAd(
                adType: adType,
                isComplete: isCompleted,
                userAdSkipPackage: hasSkipPackage
            );
        }

        // Example: Called to show Highbrow In-house Cross Promotion Ad
        public void ShowCrossPromotionAd()
        {
            // 5. Show In-house House Ad
            Highbrow.Ad.HighbrowAd.Show(
                onCompleted: () =>
                {
                    Debug.Log("[HighbrowSdkDemo] Cross-promotion ad finished. Grant reward here.");
                },
                onFailed: () =>
                {
                    Debug.LogWarning("[HighbrowSdkDemo] Failed to load or display cross-promotion ad.");
                }
            );
        }
    }
}
