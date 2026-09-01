using System;

namespace Highbrow.Log
{
    /// <summary>
    /// User authentication / login log recorded on successful login.
    /// JSON Keys match Snowflake AUTH log schema (PascalCase).
    /// </summary>
    [Serializable]
    public class AuthLog
    {
        public string Time;
        public int AccountType;
        public string AccountId;
        public string Suid;
        public string Duid;
        public int Market;
        public int Os;
        public string Country;
        public string IpAddress;
        public string Nickname;
        public string DeviceInfo;
        public string Result;
    }
}
