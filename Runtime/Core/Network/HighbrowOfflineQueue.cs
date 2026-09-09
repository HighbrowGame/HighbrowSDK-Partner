using System;
using System.Collections.Generic;
using Highbrow.Core.Utils;
using UnityEngine;
using UnityEngine.Scripting;

namespace Highbrow.Core.Network
{
    /// <summary>
    /// Persistent offline log cache using PlayerPrefs.
    /// Provides FIFO buffering and automatic rotation to prevent memory/storage overflow.
    /// </summary>
    public class HighbrowOfflineQueue
    {
        private const string PrefsQueueKey = "HIGHBROW_SDK_OFFLINE_LOG_QUEUE";
        private readonly int maxQueueSize;
        private readonly object queueLock = new object();
        private QueuedLogWrapper cachedData;

        [Preserve]
        [Serializable]
        public class QueuedLogItem
        {
            public string LogType;
            public string JsonPayload;
            public long TimestampTicks;

            public QueuedLogItem() { }

            public QueuedLogItem(string logType, string jsonPayload)
            {
                LogType = logType;
                JsonPayload = jsonPayload;
                TimestampTicks = DateTime.UtcNow.Ticks;
            }
        }

        [Preserve]
        [Serializable]
        public class QueuedLogWrapper
        {
            public List<QueuedLogItem> Items = new List<QueuedLogItem>();
        }

        public HighbrowOfflineQueue(int maxQueueSize = 300)
        {
            this.maxQueueSize = maxQueueSize;
            LoadQueueFromPrefs();
        }

        public int Count
        {
            get
            {
                lock (queueLock)
                {
                    return cachedData.Items.Count;
                }
            }
        }

        /// <summary>
        /// Enqueues a failed log payload to PlayerPrefs cache.
        /// </summary>
        public void Enqueue(string logType, string jsonPayload)
        {
            if (string.IsNullOrEmpty(jsonPayload)) return;

            lock (queueLock)
            {
                // FIFO eviction if capacity reached
                while (cachedData.Items.Count >= maxQueueSize && cachedData.Items.Count > 0)
                {
                    cachedData.Items.RemoveAt(0);
                    HighbrowLogger.LogWarning("Offline log queue reached capacity. Evicting oldest item.");
                }

                cachedData.Items.Add(new QueuedLogItem(logType, jsonPayload));
                SaveQueueToPrefs(true);
                HighbrowLogger.Log($"Enqueued log to offline cache. Current queue count: {cachedData.Items.Count}");
            }
        }

        /// <summary>
        /// Retrieves a batch of pending logs without immediately removing them.
        /// </summary>
        public List<QueuedLogItem> PeekBatch(int maxBatchCount = 20)
        {
            lock (queueLock)
            {
                int count = Math.Min(maxBatchCount, cachedData.Items.Count);
                List<QueuedLogItem> batch = new List<QueuedLogItem>(count);
                for (int i = 0; i < count; i++)
                {
                    if (cachedData.Items[i] != null)
                    {
                        batch.Add(cachedData.Items[i]);
                    }
                }
                return batch;
            }
        }

        /// <summary>
        /// Removes the specified count of processed items from head of the queue.
        /// </summary>
        public void RemoveProcessed(int count)
        {
            lock (queueLock)
            {
                int removeCount = Math.Min(count, cachedData.Items.Count);
                if (removeCount > 0)
                {
                    cachedData.Items.RemoveRange(0, removeCount);
                    SaveQueueToPrefs(true);
                    HighbrowLogger.Log($"Removed {removeCount} processed logs from offline cache. Remaining: {cachedData.Items.Count}");
                }
            }
        }

        /// <summary>
        /// Clears all offline logs from cache.
        /// </summary>
        public void Clear()
        {
            lock (queueLock)
            {
                cachedData.Items.Clear();
                try
                {
                    if (HighbrowDispatcher.IsMainThread)
                    {
                        PlayerPrefs.DeleteKey(PrefsQueueKey);
                        PlayerPrefs.Save();
                    }
                    else
                    {
                        HighbrowDispatcher.Instance?.Enqueue(() =>
                        {
                            try
                            {
                                PlayerPrefs.DeleteKey(PrefsQueueKey);
                                PlayerPrefs.Save();
                            }
                            catch (Exception ex)
                            {
                                HighbrowLogger.LogError($"Failed to delete offline queue from PlayerPrefs: {ex.Message}");
                            }
                        });
                    }
                }
                catch (Exception ex)
                {
                    HighbrowLogger.LogError($"Failed to clear offline queue: {ex.Message}");
                }
            }
        }

        private void LoadQueueFromPrefs()
        {
            lock (queueLock)
            {
                string json = string.Empty;
                try
                {
                    json = PlayerPrefs.GetString(PrefsQueueKey, string.Empty);
                }
                catch (Exception ex)
                {
                    HighbrowLogger.LogError($"Failed to read offline queue from PlayerPrefs: {ex.Message}");
                }

                if (!string.IsNullOrEmpty(json))
                {
                    try
                    {
                        cachedData = JsonUtility.FromJson<QueuedLogWrapper>(json);
                    }
                    catch (Exception ex)
                    {
                        HighbrowLogger.LogError($"Failed to deserialize offline queue: {ex.Message}");
                        cachedData = new QueuedLogWrapper();
                    }
                }

                if (cachedData == null)
                {
                    cachedData = new QueuedLogWrapper();
                }

                if (cachedData.Items == null)
                {
                    cachedData.Items = new List<QueuedLogItem>();
                }
                else
                {
                    cachedData.Items.RemoveAll(item => item == null);
                }
            }
        }

        /// <summary>
        /// Flushes in-memory queue changes to persistent storage immediately.
        /// Recommended to call during application pause or quit.
        /// </summary>
        public void PersistToDisk()
        {
            lock (queueLock)
            {
                SaveQueueToPrefs(true);
            }
        }

        private void SaveQueueToPrefs(bool flushToDisk = false)
        {
            try
            {
                string json = JsonUtility.ToJson(cachedData);
                if (HighbrowDispatcher.IsMainThread)
                {
                    PlayerPrefs.SetString(PrefsQueueKey, json);
                    if (flushToDisk)
                    {
                        PlayerPrefs.Save();
                    }
                }
                else
                {
                    HighbrowDispatcher.Instance?.Enqueue(() =>
                    {
                        try
                        {
                            PlayerPrefs.SetString(PrefsQueueKey, json);
                            if (flushToDisk)
                            {
                                PlayerPrefs.Save();
                            }
                        }
                        catch (Exception ex)
                        {
                            HighbrowLogger.LogError($"Failed to persist offline queue on main thread: {ex.Message}");
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                HighbrowLogger.LogError($"Failed to persist offline queue to PlayerPrefs: {ex.Message}");
            }
        }
    }
}
