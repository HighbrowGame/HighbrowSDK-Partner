using System;

namespace Highbrow.Log
{
    /// <summary>
    /// Advertisement impression log recorded upon ad view.
    /// JSON Keys match Snowflake ADVERTISEMENT log schema.
    /// </summary>
    [Serializable]
    public class AdLog
    {
        public string Time;
        public string Suid;
        public int Market;
        public int Os;
        public string Country;
        public int AdType;
        public string AdTypeName;
    }
}
