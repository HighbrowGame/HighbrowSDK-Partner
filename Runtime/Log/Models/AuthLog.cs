using System;

namespace Highbrow.Log
{
    /// <summary>
    /// User authentication / login log recorded on successful or attempted login.
    /// JSON Keys match Snowflake AUTH log schema.
    /// </summary>
    [Serializable]
    public class AuthLog
    {
        public string Time;
        public string Suid;
        public string Nickname;
        public string Duid;
        public int Market;
        public int Os;
        public string Country;
        public string IpAddress;
        public int AccountType;
        public string AccountId;
        public string Result;
        public string DeviceInfo;
        public string ClientVersion;
        public string LastActiveTime;
        public bool IsNewDuid;
        public string Region;
    }
}
