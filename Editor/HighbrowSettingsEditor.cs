using Highbrow.Core;
using UnityEditor;
using UnityEngine;

namespace Highbrow.Editor
{
    /// <summary>
    /// Custom Inspector for HighbrowSettings ScriptableObject.
    /// Provides real-time validation warnings for AppKey, Sandbox mode, and Country format.
    /// </summary>
    [CustomEditor(typeof(HighbrowSettings))]
    public class HighbrowSettingsEditor : UnityEditor.Editor
    {
        private SerializedProperty appKeyProp;
        private SerializedProperty useSandboxProp;
        private SerializedProperty customCountryProp;
        private SerializedProperty httpTimeoutSecondsProp;

        private void OnEnable()
        {
            appKeyProp = serializedObject.FindProperty("appKey");
            useSandboxProp = serializedObject.FindProperty("useSandbox");
            customCountryProp = serializedObject.FindProperty("customCountry");
            httpTimeoutSecondsProp = serializedObject.FindProperty("httpTimeoutSeconds");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Highbrow SDK Configuration", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Configure Highbrow SDK parameters. Changes are serialized to this asset and automatically loaded when calling HighbrowSDK.Initialize().", MessageType.None);
            EditorGUILayout.Space(6);

            // Validation Warning Banners
            if (appKeyProp != null && string.IsNullOrWhiteSpace(appKeyProp.stringValue))
            {
                EditorGUILayout.HelpBox("AppKey is missing! Highbrow SDK requires a valid AppKey issued by Highbrow to initialize.", MessageType.Error);
                EditorGUILayout.Space(2);
            }

            if (useSandboxProp != null && useSandboxProp.boolValue)
            {
                EditorGUILayout.HelpBox("SANDBOX MODE IS ACTIVE. Logs will route to the development/testing collector. Ensure this is disabled for production release builds.", MessageType.Warning);
                EditorGUILayout.Space(2);
            }

            if (customCountryProp != null && !string.IsNullOrWhiteSpace(customCountryProp.stringValue))
            {
                string country = customCountryProp.stringValue.Trim().ToUpperInvariant();
                if (country.Length != 2 || !char.IsLetter(country[0]) || !char.IsLetter(country[1]))
                {
                    EditorGUILayout.HelpBox($"CustomCountry '{customCountryProp.stringValue}' is invalid. Must be a 2-letter ISO code (e.g. 'KR', 'US'). Invalid codes will be ignored at runtime.", MessageType.Warning);
                    EditorGUILayout.Space(2);
                }
            }

            if (httpTimeoutSecondsProp != null && (httpTimeoutSecondsProp.intValue < 3 || httpTimeoutSecondsProp.intValue > 30))
            {
                EditorGUILayout.HelpBox($"HttpTimeoutSeconds is set to {httpTimeoutSecondsProp.intValue}s. It will be automatically clamped between 3s and 30s at runtime for safety.", MessageType.Info);
                EditorGUILayout.Space(2);
            }

            DrawDefaultInspector();

            serializedObject.ApplyModifiedProperties();
        }
    }
}
