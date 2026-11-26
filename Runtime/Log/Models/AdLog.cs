using System;
using UnityEngine.Scripting;

namespace Highbrow.Log
{
    /// <summary>
    /// Advertisement impression log recorded upon ad view.
    /// JSON Keys match Snowflake ADVERTISEMENT log schema.
    /// </summary>
    [Preserve]
    [Serializable]
    public class AdLog
    {
        public string Time;
        public long UnixTimestampMs;
        public string SdkVersion;
        public string Suid;
        public int Market;
        public int Os;
        public string Country;
        public int AdType;
        public string AdTypeName;
    }
}
