using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Highbrow.Core.Utils
{
    /// <summary>
    /// Global MonoBehaviour dispatcher for running coroutines, main-thread actions,
    /// and monitoring application lifecycle events across scenes.
    /// </summary>
    public class HighbrowDispatcher : MonoBehaviour
    {
        private static HighbrowDispatcher instance;
        private static readonly object lockObject = new object();
        private static bool isQuitting = false;
        private static int mainThreadId;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CaptureMainThreadId()
        {
            mainThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
        }

        /// <summary>
        /// Gets whether the current executing thread is the Unity main thread.
        /// </summary>
        public static bool IsMainThread
        {
            get
            {
                if (mainThreadId == 0) return true;
                return System.Threading.Thread.CurrentThread.ManagedThreadId == mainThreadId;
            }
        }

        private readonly Queue<Action> executionQueue = new Queue<Action>();

        public static event Action<bool> OnPauseStateChanged;
        public static event Action OnQuitTriggered;

        /// <summary>
        /// Gets whether Unity application shutdown has started.
        /// </summary>
        public static bool IsQuitting => isQuitting;

        public static HighbrowDispatcher Instance
        {
            get
            {
                if (isQuitting)
                {
                    return null;
                }

                lock (lockObject)
                {
                    if (instance == null)
                    {
                        GameObject go = new GameObject("[HighbrowSDK_Dispatcher]");
                        DontDestroyOnLoad(go);
                        instance = go.AddComponent<HighbrowDispatcher>();
                    }
                    return instance;
                }
            }
        }

        public static void EnsureCreated()
        {
            _ = Instance;
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            mainThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            lock (executionQueue)
            {
                while (executionQueue.Count > 0)
                {
                    try
                    {
                        executionQueue.Dequeue()?.Invoke();
                    }
                    catch (Exception ex)
                    {
                        HighbrowLogger.LogError($"Dispatcher action error: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Enqueues an action to be executed on the Unity main thread in Update.
        /// </summary>
        public void Enqueue(Action action)
        {
            if (action == null) return;

            lock (executionQueue)
            {
                executionQueue.Enqueue(action);
            }
        }

        /// <summary>
        /// Starts a coroutine managed by the HighbrowDispatcher lifecycle.
        /// </summary>
        public Coroutine RunCoroutine(IEnumerator routine)
        {
            if (routine == null) return null;
            return StartCoroutine(routine);
        }

        /// <summary>
        /// Stops a coroutine managed by the dispatcher.
        /// </summary>
        public void TerminateCoroutine(Coroutine coroutine)
        {
            if (coroutine != null)
            {
                StopCoroutine(coroutine);
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            HighbrowLogger.Log($"Application pause status changed: {pauseStatus}");
            OnPauseStateChanged?.Invoke(pauseStatus);
        }

        private void OnApplicationQuit()
        {
            HighbrowLogger.Log("Application quit triggered.");
            isQuitting = true;
            OnQuitTriggered?.Invoke();
        }
    }
}
