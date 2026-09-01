using Highbrow.Core;
using UnityEditor;
using UnityEngine;

namespace Highbrow.Editor
{
    /// <summary>
    /// Editor utilities for Highbrow SDK.
    /// </summary>
    public static class HighbrowSdkEditor
    {
        [MenuItem("Highbrow/Clear Offline Log Cache (PlayerPrefs)", false, 10)]
        public static void ClearOfflineCache()
        {
            PlayerPrefs.DeleteKey("HIGHBROW_SDK_OFFLINE_LOG_QUEUE");
            PlayerPrefs.DeleteKey("HIGHBROW_SDK_SAVED_DUID");
            PlayerPrefs.DeleteKey("HIGHBROW_SDK_FIRST_LAUNCH_FLAG");
            PlayerPrefs.Save();
            Debug.Log("[HighbrowSDK Editor] Offline log cache and device identifiers cleared from PlayerPrefs.");
        }

        [MenuItem("Highbrow/Select HighbrowGamesInfo Asset", false, 20)]
        public static void SelectGamesInfoAsset()
        {
            var asset = Resources.Load<HighbrowGamesInfo>("Highbrow/HighbrowGamesInfo");
            if (asset != null)
            {
                Selection.activeObject = asset;
                EditorGUIUtility.PingObject(asset);
            }
            else
            {
                Debug.LogWarning("[HighbrowSDK Editor] HighbrowGamesInfo asset not found in Resources/Highbrow.");
            }
        }

        [MenuItem("Highbrow/Show Current SDK Status", false, 30)]
        public static void ShowSdkStatus()
        {
            string status = HighbrowSDK.IsInitialized ? "Initialized" : "Not Initialized";
            string mode = HighbrowSDK.Config?.ServerMode ?? "N/A";
            string region = HighbrowSDK.Config?.Region ?? "N/A";
            EditorUtility.DisplayDialog("Highbrow SDK Status", $"Status: {status}\nServer Mode: {mode}\nRegion: {region}", "OK");
        }
    }
}
