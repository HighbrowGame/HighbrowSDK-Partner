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
        public string Suid;
        public string ReceiptId;
        public int Market;
        public int Os;
        public string Country;
        public float Price;
        public string Currency;
        public string PriceId;
        public int ProductId;
        public string ProductName;
        public string PurchaseTime;
        public string ClientVersion;
        public string DeviceInfo;
    }
}
