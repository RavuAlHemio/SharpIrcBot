using System.Collections.Immutable;
using System.Text;

namespace SharpIrcBot.Plugins.Calc.AST
{
    public class ConstantReferenceExpression : Expression
    {
        public string Name { get; }

        public ConstantReferenceExpression(string name)
        {
            Name = name;
        }

        public override string ToString()
        {
            return Name;
        }

        public override PrimitiveExpression Simplified(Grimoire grimoire)
        {
            PrimitiveExpression ret;
            if (!grimoire.Constants.TryGetValue(Name, out ret))
            {
                throw new SimplificationException(
                    $"Unknown constant '{Name}'."
                );
            }
            return ret;
        }
    }
}
