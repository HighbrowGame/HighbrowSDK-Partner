using System;

namespace Highbrow.Log
{
    /// <summary>
    /// First purchase log recorded when a user makes their very first IAP purchase.
    /// JSON Keys match Snowflake NEW_PAYING log schema.
    /// </summary>
    [Serializable]
    public class NewPayingLog
    {
        public string Time;
        public string Suid;
        public int Market;
        public int Os;
        public string Country;
        public string PurchaseTime;
        public int ProductId;
        public string Region;
    }
}
