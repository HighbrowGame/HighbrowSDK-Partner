using System;

namespace Highbrow.Log
{
    /// <summary>
    /// User session heartbeat log recorded periodically (every 5 minutes) while active.
    /// JSON Keys match Snowflake USER_SESSION log schema.
    /// </summary>
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
        public string UserCreateTime;
        public string Region;
    }
}
