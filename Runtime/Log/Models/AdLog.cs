using System;

namespace Highbrow.Log
{
    /// <summary>
    /// Ad view log recorded during ad impression lifecycle (start / complete).
    /// JSON Keys match Snowflake AD log schema.
    /// </summary>
    [Serializable]
    public class AdLog
    {
        public string Time;
        public string Suid;
        public string Duid;
        public int Market;
        public int Os;
        public string Country;
        public int AdType;
        public string AdTypeName;
        public bool IsComplete;
        public bool UserAdSkipPackage;
        public string Region;
    }
}
