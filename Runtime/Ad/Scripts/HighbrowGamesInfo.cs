using System;
using System.Collections.Generic;
using Highbrow.Core;
using UnityEngine;
using UnityEngine.Video;

namespace Highbrow.Ad
{
    [Serializable]
    public class SimpleMultiLangString
    {
        public string ko = string.Empty;
        public string en = string.Empty;
        public string ja = string.Empty;
        public string zhcn = string.Empty;
        public string zhtw = string.Empty;

        public string GetText(SystemLanguage langCode)
        {
            return langCode switch
            {
                SystemLanguage.Korean => !string.IsNullOrEmpty(ko) ? ko : en,
                SystemLanguage.English => !string.IsNullOrEmpty(en) ? en : ko,
                SystemLanguage.Japanese => !string.IsNullOrEmpty(ja) ? ja : en,
                SystemLanguage.ChineseSimplified => !string.IsNullOrEmpty(zhcn) ? zhcn : en,
                SystemLanguage.ChineseTraditional => !string.IsNullOrEmpty(zhtw) ? zhtw : en,
                _ => !string.IsNullOrEmpty(en) ? en : ko,
            };
        }
    }

    [Serializable]
    public class GameMarketInfo
    {
        public string GameCode = string.Empty;
        public bool Enable = true;
        public SimpleMultiLangString Name = new SimpleMultiLangString();
        public SimpleMultiLangString Desc = new SimpleMultiLangString();
        public string PlaystoreId = string.Empty;
        public string AppstoreId = string.Empty;
        public string OnestoreId = string.Empty;
        public string CustomStoreUrl = string.Empty;
        public Sprite AppIcon;
        public VideoClip Video;
        public string VideoUrl = string.Empty;
        public float Portion = 1.0f;

        public string GetStoreLink()
        {
            if (!string.IsNullOrEmpty(CustomStoreUrl))
            {
                return CustomStoreUrl;
            }

            var market = (MarketType)HighbrowContext.GetMarketType(HighbrowSDK.Config != null ? HighbrowSDK.Config.Market : MarketType.None, HighbrowSDK.Config?.CustomMarket);
            if (market == MarketType.OneStore)
            {
                if (!string.IsNullOrEmpty(OnestoreId))
                {
                    return $"https://m.onestore.co.kr/v2/ko-kr/app/{OnestoreId}?scYn=Y";
                }
                return !string.IsNullOrEmpty(PlaystoreId) ? $"https://play.google.com/store/apps/details?id={PlaystoreId}" : string.Empty;
            }

            if (Application.platform == RuntimePlatform.Android)
            {
                return !string.IsNullOrEmpty(PlaystoreId) ? $"https://play.google.com/store/apps/details?id={PlaystoreId}" : string.Empty;
            }
            else if (Application.platform == RuntimePlatform.IPhonePlayer)
            {
                return !string.IsNullOrEmpty(AppstoreId) ? $"https://apps.apple.com/app/id{AppstoreId}" : string.Empty;
            }
            else
            {
                // PC Standalone / Editor / Other platforms do not redirect to mobile app stores
                return string.Empty;
            }
        }
    }

    [CreateAssetMenu(fileName = "HighbrowGamesInfo", menuName = "HighbrowSDK/HighbrowGamesInfo")]
    public class HighbrowGamesInfo : ScriptableObject
    {
        public GameMarketInfo[] Classes;
        public string Version = "1.0.0";

        public List<GameMarketInfo> ActiveGameMarketInfos { get; private set; }

        public void Initialize()
        {
            ActiveGameMarketInfos = new List<GameMarketInfo>();
            if (Classes == null) return;

            for (int i = 0; i < Classes.Length; i++)
            {
                if (Classes[i] != null && Classes[i].Enable)
                {
                    ActiveGameMarketInfos.Add(Classes[i]);
                }
            }
        }

        public GameMarketInfo GetRandomByWeight()
        {
            if (ActiveGameMarketInfos == null || ActiveGameMarketInfos.Count == 0)
            {
                Initialize();
            }

            if (ActiveGameMarketInfos == null || ActiveGameMarketInfos.Count == 0)
                return null;

            float totalWeight = 0f;
            for (int i = 0; i < ActiveGameMarketInfos.Count; i++)
            {
                totalWeight += ActiveGameMarketInfos[i].Portion;
            }

            if (totalWeight <= 0f)
                return ActiveGameMarketInfos[0];

            float randomValue = UnityEngine.Random.Range(0f, totalWeight);

            float cumulativeWeight = 0f;
            for (int i = 0; i < ActiveGameMarketInfos.Count; i++)
            {
                cumulativeWeight += ActiveGameMarketInfos[i].Portion;
                if (randomValue <= cumulativeWeight)
                    return ActiveGameMarketInfos[i];
            }

            return ActiveGameMarketInfos[0];
        }
    }
}
