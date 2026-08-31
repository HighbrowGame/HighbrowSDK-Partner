using System;
using Highbrow.Core;
using Highbrow.Core.Utils;
using UnityEngine;

namespace Highbrow.Ad
{
    /// <summary>
    /// Future extension module skeleton: Highbrow Ad Player.
    /// Provides cross-promotion banner, interstitial, and rewarded video ad display functionalities.
    /// Fully decoupled from Highbrow.Log and Highbrow.GameCenter modules.
    /// </summary>
    public class HighbrowAdPlayer : IHighbrowModule
    {
        private static HighbrowAdPlayer instance;

        public static HighbrowAdPlayer Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new HighbrowAdPlayer();
                    HighbrowSDK.RegisterModule(instance);
                }
                return instance;
            }
        }

        public string ModuleName => "HighbrowAd";
        public bool IsInitialized { get; private set; }

        private HighbrowConfig config;

        // Ad lifecycle events
        public event Action<string> OnAdLoaded;
        public event Action<string> OnAdOpened;
        public event Action<string> OnAdClosed;
        public event Action<string, string, double> OnUserRewarded;
        public event Action<string, string> OnAdFailedToLoad;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoRegister()
        {
            HighbrowSDK.RegisterModule(Instance);
        }

        public void Initialize(HighbrowConfig sdkConfig)
        {
            config = sdkConfig;
            if (config != null && !config.EnableAd)
            {
                HighbrowLogger.Log("HighbrowAdPlayer is disabled in HighbrowConfig.");
                return;
            }

            IsInitialized = true;
            HighbrowLogger.Log("HighbrowAdPlayer skeleton initialized.");
        }

        public void Shutdown()
        {
            IsInitialized = false;
            HighbrowLogger.Log("HighbrowAdPlayer shutdown.");
        }

        #region Public Ad APIs (Future Skeleton)

        /// <summary>
        /// Loads a rewarded video ad for the given placement tag.
        /// </summary>
        public void LoadRewardedVideo(string placementId)
        {
            HighbrowLogger.Log($"[Ad Skeleton] LoadRewardedVideo requested for placement: {placementId}");
            // Future implementation: Mediation network / Highbrow Ad Server integration
        }

        /// <summary>
        /// Checks if a rewarded video ad is ready to be displayed.
        /// </summary>
        public bool IsRewardedVideoAvailable(string placementId)
        {
            HighbrowLogger.Log($"[Ad Skeleton] IsRewardedVideoAvailable checked for placement: {placementId}");
            return false;
        }

        /// <summary>
        /// Shows a rewarded video ad and fires reward callback upon completion.
        /// </summary>
        public void ShowRewardedVideo(string placementId, Action<bool, string> onComplete)
        {
            HighbrowLogger.Log($"[Ad Skeleton] ShowRewardedVideo requested for placement: {placementId}");
            // Future implementation: Trigger video playback and reward validation
            onComplete?.Invoke(true, "RewardGranted_Mock");
        }

        /// <summary>
        /// Shows a cross-promotion or interstitial banner.
        /// </summary>
        public void ShowInterstitial(string placementId, Action onClosed = null)
        {
            HighbrowLogger.Log($"[Ad Skeleton] ShowInterstitial requested for placement: {placementId}");
            onClosed?.Invoke();
        }

        /// <summary>
        /// Displays bottom/top cross-promotion banner ad.
        /// </summary>
        public void ShowBanner(string placementId, int position)
        {
            HighbrowLogger.Log($"[Ad Skeleton] ShowBanner requested for placement: {placementId}");
        }

        /// <summary>
        /// Hides active banner ad.
        /// </summary>
        public void HideBanner()
        {
            HighbrowLogger.Log("[Ad Skeleton] HideBanner requested.");
        }

        #endregion
    }
}
