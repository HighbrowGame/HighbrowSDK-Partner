using UnityEngine;

namespace Highbrow.Core.Utils
{
    /// <summary>
    /// Internal logger for Highbrow SDK.
    /// Prefixes all messages with [HighbrowSDK] and honors DebugMode flag.
    /// </summary>
    public static class HighbrowLogger
    {
        public static bool DebugMode = false;
        private const string Tag = "[HighbrowSDK]";

        public static void Log(string message)
        {
            if (DebugMode)
            {
                Debug.Log($"{Tag} {message}");
            }
        }

        public static void LogWarning(string message)
        {
            Debug.LogWarning($"{Tag} [WARN] {message}");
        }

        public static void LogError(string message)
        {
            Debug.LogError($"{Tag} [ERROR] {message}");
        }
    }
}
