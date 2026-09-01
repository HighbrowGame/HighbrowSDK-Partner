using System;

namespace Highbrow.Log
{
    /// <summary>
    /// Daily active unique user log recorded on daily date transition or market change.
    /// JSON Keys match Snowflake DAILY_ACTIVE_USER_SUID log schema.
    /// </summary>
    [Serializable]
    public class DailyActiveUserSuidLog
    {
        public string Time;
        public string Suid;
        public int Market;
        public int Os;
        public string Country;
        public string LastActiveTime;
        public string UserCreateTime;
    }
}
