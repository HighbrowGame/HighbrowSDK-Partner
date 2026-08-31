using System;
using System.Collections;
using System.Text;
using Highbrow.Core.Utils;
using UnityEngine;
using UnityEngine.Networking;

namespace Highbrow.Core.Network
{
    /// <summary>
    /// HTTP network transport layer using UnityWebRequest.
    /// Handles sending JSON payloads with headers, timeout, and response callbacks.
    /// </summary>
    public class HighbrowHttpClient
    {
        private readonly int timeoutSeconds;

        public HighbrowHttpClient(int timeoutSeconds = 10)
        {
            this.timeoutSeconds = timeoutSeconds;
        }

        /// <summary>
        /// Sends an asynchronous HTTP POST request with JSON payload.
        /// </summary>
        /// <param name="endpointUrl">Target server URL.</param>
        /// <param name="appKey">Application Key for authorization header.</param>
        /// <param name="logType">Type header identifier (e.g. LogAuth, LogAlive).</param>
        /// <param name="jsonPayload">JSON formatted string.</param>
        /// <param name="onComplete">Callback with (success, error/responseMessage).</param>
        public void PostJson(string endpointUrl, string appKey, string logType, string jsonPayload, Action<bool, string> onComplete)
        {
            if (string.IsNullOrEmpty(endpointUrl))
            {
                onComplete?.Invoke(false, "Endpoint URL is empty.");
                return;
            }

            HighbrowDispatcher.Instance.RunCoroutine(PostJsonCoroutine(endpointUrl, appKey, logType, jsonPayload, onComplete));
        }

        private IEnumerator PostJsonCoroutine(string endpointUrl, string appKey, string logType, string jsonPayload, Action<bool, string> onComplete)
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);

            using (UnityWebRequest request = new UnityWebRequest(endpointUrl, UnityWebRequest.kHttpVerbPOST))
            {
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.timeout = timeoutSeconds;

                request.SetRequestHeader("Content-Type", "application/json; charset=utf-8");
                request.SetRequestHeader("Accept", "application/json");

                if (!string.IsNullOrEmpty(appKey))
                {
                    request.SetRequestHeader("X-App-Key", appKey);
                }

                if (!string.IsNullOrEmpty(logType))
                {
                    request.SetRequestHeader("X-Log-Type", logType);
                }

                request.SetRequestHeader("X-SDK-Version", "1.0.0");

                HighbrowLogger.Log($"Sending [{logType}] request to {endpointUrl} (Bytes: {bodyRaw.Length})");

                yield return request.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
                bool isSuccess = request.result == UnityWebRequest.Result.Success;
#else
                bool isSuccess = !request.isNetworkError && !request.isHttpError;
#endif

                if (isSuccess)
                {
                    string responseText = request.downloadHandler?.text ?? string.Empty;
                    HighbrowLogger.Log($"[{logType}] Sent successfully. Response Code: {request.responseCode}");
                    onComplete?.Invoke(true, responseText);
                }
                else
                {
                    string errorMsg = $"HTTP {request.responseCode} - {request.error}";
                    HighbrowLogger.LogWarning($"[{logType}] Request failed: {errorMsg}");
                    onComplete?.Invoke(false, errorMsg);
                }
            }
        }
    }
}
