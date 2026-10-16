namespace Highbrow.Log
{
    /// <summary>
    /// User account authentication provider type.
    /// Matches Highbrow authentication protocol specifications.
    /// </summary>
    public enum AccountType : sbyte
    {
        All = -1,
        None = 0,
        Guest = 0, // Same as None: the collector treats None as a guest account.
        AppleGameCenter = 1,
        GooglePlay = 2,
        Facebook = 3,
        Steam = 4,
        AppleId = 5,
        GameCenterTeam = 6,
        Vng = 7,
        HighbrowId = 8
    }
}
