using System;

namespace Highbrow.Log
{
    /// <summary>
    /// New user log recorded upon first account creation / character creation / lobby entry.
    /// JSON Keys match Snowflake NEW_USER log schema.
    /// </summary>
    [Serializable]
    public class NewUserLog
    {
        public string Time;
        public string Suid;
        public string Duid;
        public int Market;
        public int Os;
        public string Country;
        public int AccountType;
        public bool IsNewDuid;
        public string Region;
    }
}
