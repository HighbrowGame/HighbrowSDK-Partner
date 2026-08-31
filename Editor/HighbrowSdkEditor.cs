using Highbrow.Core;
using Highbrow.Log;
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

        [MenuItem("Highbrow/Show Current SDK Status", false, 11)]
        public static void ShowSdkStatus()
        {
            string status = HighbrowSDK.IsInitialized ? "Initialized" : "Not Initialized";
            string mode = HighbrowSDK.Config?.ServerMode ?? "N/A";
            string region = HighbrowSDK.Config?.Region ?? "N/A";
            EditorUtility.DisplayDialog("Highbrow SDK Status", $"Status: {status}\nServer Mode: {mode}\nRegion: {region}", "OK");
        }
    }
}
