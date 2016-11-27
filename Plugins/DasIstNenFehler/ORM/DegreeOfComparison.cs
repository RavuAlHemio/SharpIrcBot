namespace DasIstNenFehler.ORM
{
    public enum DegreeOfComparison : byte
    {
        Positive = 1,
        Comparative = (byte)'>',
        Superlative = 0xF0,
        Hyperlative = 0xFF
    }
}
