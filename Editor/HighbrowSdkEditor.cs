using Highbrow.Ad;
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

        [MenuItem("Highbrow/Diagnose House Ad Prefab", false, 40)]
        public static void DiagnoseHouseAdPrefab()
        {
            // 1. AssetDatabase Load check (Package path)
            const string packagePrefabPath = "Packages/com.highbrow.sdk/Runtime/Ad/Resources/Highbrow/UI_HighbrowAd.prefab";
            var assetDbPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(packagePrefabPath);
            Debug.Log($"[HighbrowSDK Editor Diagnosis] AssetDatabase.LoadAssetAtPath: {(assetDbPrefab != null ? "SUCCESS (non-null)" : "FAILED (null)")} at '{packagePrefabPath}'");

            // 2. Resources.Load check
            const string resourcePath = "Highbrow/UI_HighbrowAd";
            var resourcesPrefab = Resources.Load<GameObject>(resourcePath);
            Debug.Log($"[HighbrowSDK Editor Diagnosis] Resources.Load<GameObject>: {(resourcesPrefab != null ? "SUCCESS (non-null)" : "FAILED (null)")} at '{resourcePath}'");

            if (assetDbPrefab != null && resourcesPrefab == null)
            {
                Debug.LogError("[HighbrowSDK Editor Diagnosis] Result: AssetDatabase succeeded but Resources.Load failed. The asset exists in PackageCache but is not registered in Unity's Resources index. Reimporting the folder or restarting Unity will resolve this.");
            }
            else if (assetDbPrefab == null && resourcesPrefab == null)
            {
                Debug.LogError("[HighbrowSDK Editor Diagnosis] Result: Both failed. The package cache does not contain the prefab or the package is corrupted. Please reinstall/re-resolve the package from Package Manager.");
            }
            else if (resourcesPrefab != null)
            {
                Debug.Log("[HighbrowSDK Editor Diagnosis] Result: Resources.Load SUCCESS! Prefab is ready and HighbrowAd.Show will work.");
            }
        }
    }
}
