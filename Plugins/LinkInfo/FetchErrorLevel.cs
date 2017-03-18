namespace SharpIrcBot.Plugins.LinkInfo
{
    public enum FetchErrorLevel
    {
        Unfetched = -1,
        Success = 0,
        TransientError = 1,
        LastingError = 2
    }
}
