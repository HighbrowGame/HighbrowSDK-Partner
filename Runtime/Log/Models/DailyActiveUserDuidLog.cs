using System;

namespace Highbrow.Log
{
    /// <summary>
    /// Daily active unique device log recorded on daily date transition.
    /// JSON Keys match Snowflake DAILY_ACTIVE_USER_DUID log schema.
    /// </summary>
    [Serializable]
    public class DailyActiveUserDuidLog
    {
        public string Time;
        public string Duid;
        public int Market;
        public int Os;
        public string Country;
        public string UserCreateTime;
        public bool IsNewDuid;
    }
}
