using System;
using UnityEngine.Scripting;

namespace Highbrow.Log
{
    /// <summary>
    /// User session heartbeat log recorded periodically (every 2 minutes) while active.
    /// Emits client runtime facts. Server DB injects UserCreateTime upon Snowflake ingestion.
    /// </summary>
    [Preserve]
    [Serializable]
    public class UserSessionLog
    {
        public string Time;
        public int AccountType;
        public string Suid;
        public string Duid;
        public int Market;
        public int Os;
        public string Country;
    }
}
