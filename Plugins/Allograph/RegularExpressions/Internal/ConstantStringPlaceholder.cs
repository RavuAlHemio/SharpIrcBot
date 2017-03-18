namespace SharpIrcBot.Plugins.Allograph.RegularExpressions.Internal
{
    public class ConstantStringPlaceholder : IPlaceholder
    {
        public string ConstantString { get; }

        public ConstantStringPlaceholder(string constantString)
        {
            ConstantString = constantString;
        }

        public string Replace(ReplacementState state)
        {
            return ConstantString;
        }
    }
}
