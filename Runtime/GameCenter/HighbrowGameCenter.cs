using System;
using System.Collections.Generic;
using Highbrow.Core;
using Highbrow.Core.Utils;
using UnityEngine;

namespace Highbrow.GameCenter
{
    /// <summary>
    /// Future extension module skeleton: Highbrow Game Center.
    /// Provides cross-game download rewards, cross-promotion mission verifications,
    /// and partner publishing ecosystem integration.
    /// Fully decoupled from Highbrow.Log and Highbrow.Ad modules.
    /// </summary>
    public class HighbrowGameCenter : IHighbrowModule
    {
        private static HighbrowGameCenter instance;

        public static HighbrowGameCenter Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new HighbrowGameCenter();
                    HighbrowSDK.RegisterModule(instance);
                }
                return instance;
            }
        }

        public string ModuleName => "HighbrowGameCenter";
        public bool IsInitialized { get; private set; }

        private HighbrowConfig config;

        [Serializable]
        public class PromotionItem
        {
            public string GameId;
            public string Title;
            public string Description;
            public string IconUrl;
            public string StoreUrl;
            public int RewardAmount;
            public string RewardItemKey;
            public bool IsInstalled;
            public bool IsRewardClaimed;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoRegister()
        {
            HighbrowSDK.RegisterModule(Instance);
        }

        public void Initialize(HighbrowConfig sdkConfig)
        {
            config = sdkConfig;
            if (config != null && !config.EnableGameCenter)
            {
                HighbrowLogger.Log("HighbrowGameCenter is disabled in HighbrowConfig.");
                return;
            }

            IsInitialized = true;
            HighbrowLogger.Log("HighbrowGameCenter skeleton initialized.");
        }

        public void Shutdown()
        {
            IsInitialized = false;
            HighbrowLogger.Log("HighbrowGameCenter shutdown.");
        }

        #region Public GameCenter APIs (Future Skeleton)

        /// <summary>
        /// Fetches available cross-promotion game download mission list from Highbrow GameCenter backend.
        /// </summary>
        public void FetchPromotionList(Action<bool, List<PromotionItem>> callback)
        {
            HighbrowLogger.Log("[GameCenter Skeleton] FetchPromotionList requested.");
            // Future implementation: HTTP GET from Highbrow GameCenter API
            callback?.Invoke(true, new List<PromotionItem>());
        }

        /// <summary>
        /// Verifies whether the target cross-promotion game has been installed and mission completed.
        /// </summary>
        public void CheckMissionStatus(string targetGameId, Action<bool, bool> callback)
        {
            HighbrowLogger.Log($"[GameCenter Skeleton] CheckMissionStatus requested for {targetGameId}");
            // Future implementation: Check package manager or backend verification
            callback?.Invoke(true, false);
        }

        /// <summary>
        /// Claims reward for completed cross-game download or promotion mission.
        /// </summary>
        public void ClaimCrossReward(string targetGameId, string missionId, Action<bool, string> callback)
        {
            HighbrowLogger.Log($"[GameCenter Skeleton] ClaimCrossReward requested for game {targetGameId}, mission {missionId}");
            // Future implementation: Send verification receipt to Highbrow GameCenter Server
            callback?.Invoke(true, "RewardClaimSuccess_Mock");
        }

        /// <summary>
        /// Opens Highbrow Game Center unified hub UI overlay.
        /// </summary>
        public void OpenGameCenterHub()
        {
            HighbrowLogger.Log("[GameCenter Skeleton] OpenGameCenterHub UI requested.");
            // Future implementation: Open GameCenter canvas/overlay
        }

        #endregion
    }
}
