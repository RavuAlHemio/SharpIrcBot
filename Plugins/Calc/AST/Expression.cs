namespace SharpIrcBot.Plugins.Calc.AST
{
    public abstract class Expression
    {
        public int Index { get; }
        public int Length { get; }

        protected Expression(int index, int length)
        {
            Index = index;
            Length = length;
        }

        public abstract PrimitiveExpression Simplified(Grimoire grimoire, CalcTimer timer);
    }
}
