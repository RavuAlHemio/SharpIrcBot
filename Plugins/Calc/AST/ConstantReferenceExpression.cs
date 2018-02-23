using System.Collections.Immutable;
using System.Text;

namespace SharpIrcBot.Plugins.Calc.AST
{
    public class ConstantReferenceExpression : Expression
    {
        public string Name { get; }

        public ConstantReferenceExpression(int index, int length, string name)
            : base(index, length)
        {
            Name = name;
        }

        public override string ToString()
        {
            return Name;
        }

        public override PrimitiveExpression Simplified(Grimoire grimoire, CalcTimer timer)
        {
            timer.ThrowIfTimedOut();

            PrimitiveExpression ret;
            if (!grimoire.Constants.TryGetValue(Name, out ret))
            {
                throw new SimplificationException(
                    $"Unknown constant '{Name}'.",
                    this
                );
            }

            return ret.Repositioned(Index, Length);
        }
    }
}
