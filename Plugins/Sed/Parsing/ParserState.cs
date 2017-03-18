namespace SharpIrcBot.Plugins.Sed.Parsing
{
    public enum ParserState
    {
        AwaitingCommand = 10,
        AwaitingSeparatorAfterCommand = 20,
        AwaitingPattern = 30,
        AwaitingReplacement = 40,
        AwaitingFlags = 50,
        Finished = 60
    }
}
