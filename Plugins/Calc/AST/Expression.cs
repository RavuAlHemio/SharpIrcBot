namespace SharpIrcBot.Plugins.Calc.AST
{
    public abstract class Expression
    {
        public abstract PrimitiveExpression Simplified(Grimoire grimoire);
    }
}
