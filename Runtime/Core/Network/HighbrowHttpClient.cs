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
        private readonly bool dumpHttpPayload;

        public HighbrowHttpClient(int timeoutSeconds = 10, bool dumpHttpPayload = false)
        {
            this.timeoutSeconds = timeoutSeconds;
            this.dumpHttpPayload = dumpHttpPayload;
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
            PostJson(endpointUrl, appKey, logType, jsonPayload, (success, responseCode, response) =>
            {
                onComplete?.Invoke(success, response);
            });
        }

        /// <summary>
        /// Sends an asynchronous HTTP POST request with JSON payload, returning HTTP status code.
        /// </summary>
        /// <param name="endpointUrl">Target server URL.</param>
        /// <param name="appKey">Application Key for authorization header.</param>
        /// <param name="logType">Type header identifier (e.g. LogAuth, LogAlive).</param>
        /// <param name="jsonPayload">JSON formatted string.</param>
        /// <param name="onComplete">Callback with (success, httpStatusCode, error/responseMessage).</param>
        public void PostJson(string endpointUrl, string appKey, string logType, string jsonPayload, Action<bool, long, string> onComplete)
        {
            if (string.IsNullOrEmpty(endpointUrl))
            {
                onComplete?.Invoke(false, 0, "Endpoint URL is empty.");
                return;
            }

            HighbrowDispatcher dispatcher = HighbrowDispatcher.Instance;
            if (dispatcher == null)
            {
                onComplete?.Invoke(false, 0, "Dispatcher is unavailable.");
                return;
            }

            dispatcher.RunCoroutine(PostJsonCoroutine(endpointUrl, appKey, logType, jsonPayload, onComplete));
        }

        private IEnumerator PostJsonCoroutine(string endpointUrl, string appKey, string logType, string jsonPayload, Action<bool, long, string> onComplete)
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

                request.SetRequestHeader("X-SDK-Version", HighbrowSDK.SdkVersion);

                HighbrowLogger.Log($"Sending [{logType}] request to {endpointUrl} (Bytes: {bodyRaw.Length})");
                if (ShouldDumpHttpPayload)
                {
                    LogRequestDump(endpointUrl, appKey, logType, jsonPayload);
                }

                yield return request.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
                bool isSuccess = request.result == UnityWebRequest.Result.Success;
#else
                bool isSuccess = !request.isNetworkError && !request.isHttpError;
#endif

                if (ShouldDumpHttpPayload)
                {
                    LogResponseDump(request);
                }

                long responseCode = request.responseCode;

                if (isSuccess)
                {
                    string responseText = request.downloadHandler?.text ?? string.Empty;
                    HighbrowLogger.Log($"[{logType}] Sent successfully. Response Code: {responseCode}");
                    onComplete?.Invoke(true, responseCode, responseText);
                }
                else
                {
                    string errorMsg = $"HTTP {responseCode} - {request.error}";
                    HighbrowLogger.LogWarning($"[{logType}] Request failed: {errorMsg}");
                    onComplete?.Invoke(false, responseCode, errorMsg);
                }
            }
        }

        private bool ShouldDumpHttpPayload => dumpHttpPayload && HighbrowLogger.DebugMode;

        private static void LogRequestDump(string endpointUrl, string appKey, string logType, string jsonPayload)
        {
            StringBuilder dump = new StringBuilder();
            dump.AppendLine($"\n[HighbrowSDK HTTP DUMP] ---> POST {endpointUrl}");
            dump.AppendLine("[Request Headers]");
            dump.AppendLine("  Content-Type: application/json; charset=utf-8");
            dump.AppendLine("  Accept: application/json");
            if (!string.IsNullOrEmpty(appKey)) dump.AppendLine($"  X-App-Key: {appKey}");
            if (!string.IsNullOrEmpty(logType)) dump.AppendLine($"  X-Log-Type: {logType}");
            dump.AppendLine($"  X-SDK-Version: {HighbrowSDK.SdkVersion}");
            dump.AppendLine("[Request Body (JSON)]");
            dump.AppendLine(jsonPayload);
            dump.AppendLine("--------------------------------------------------");
            HighbrowLogger.Log(dump.ToString());
        }

        private static void LogResponseDump(UnityWebRequest request)
        {
            StringBuilder dump = new StringBuilder();
            dump.AppendLine($"[HighbrowSDK HTTP DUMP] <--- HTTP {request.responseCode} {request.error}");
            dump.AppendLine("[Response Headers]");

            var responseHeaders = request.GetResponseHeaders();
            if (responseHeaders != null)
            {
                foreach (var header in responseHeaders)
                {
                    dump.AppendLine($"  {header.Key}: {header.Value}");
                }
            }

            dump.AppendLine("[Response Body]");
            dump.AppendLine(request.downloadHandler?.text ?? string.Empty);
            dump.AppendLine("--------------------------------------------------");
            HighbrowLogger.Log(dump.ToString());
        }
    }
}
