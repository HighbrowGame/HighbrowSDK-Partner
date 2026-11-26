using System;
using UnityEngine.Scripting;

namespace Highbrow.Log
{
    /// <summary>
    /// In-App Purchase store receipt log recorded upon successful store purchase.
    /// JSON Keys match Snowflake STORE_RECEIPT log schema.
    /// </summary>
    [Preserve]
    [Serializable]
    public class StoreReceiptLog
    {
        public string Time;
        public long UnixTimestampMs;
        public string Suid;
        public string ReceiptId;
        public int Market;
        public int Os;
        public string Country;
        // Store price in Currency, retained under the existing collector field name.
        public float OriginalPrice;
        public string Currency;
        public string PriceId;
        public int ProductId;
        public string ProductName;
        public string PurchaseTime;
        public string ClientVersion;
        public string SdkVersion;
        public string DeviceInfo;
    }

    /// <summary>
    /// Sandbox-only variant of <see cref="StoreReceiptLog"/> that also carries the receipt string exactly as the game passed it.
    /// Used so the receipt shape of each store can be inspected from the collector. Never sent to production
    /// (JsonUtility cannot omit a field, so production uses the base class, which has no RawData key).
    /// </summary>
    [Preserve]
    [Serializable]
    public class StoreReceiptSandboxLog : StoreReceiptLog
    {
        public string RawData;
    }
}
