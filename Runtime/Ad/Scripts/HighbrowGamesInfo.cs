using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Video;

namespace Highbrow
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
        public Sprite AppIcon;
        public VideoClip Video;
        public string VideoUrl = string.Empty;
        public float Portion = 1.0f;

        public string GetStoreLink()
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                return $"https://play.google.com/store/apps/details?id={PlaystoreId}";
            }
            else
            {
                return $"https://apps.apple.com/app/id{AppstoreId}";
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

            float totalWeight = ActiveGameMarketInfos.Sum(item => item.Portion);
            if (totalWeight <= 0f)
                return ActiveGameMarketInfos[0];

            System.Random random = new System.Random();
            float randomValue = (float)(random.NextDouble() * totalWeight);

            float cumulativeWeight = 0f;
            foreach (var item in ActiveGameMarketInfos)
            {
                cumulativeWeight += item.Portion;
                if (randomValue <= cumulativeWeight)
                    return item;
            }

            return ActiveGameMarketInfos[0];
        }
    }
}
