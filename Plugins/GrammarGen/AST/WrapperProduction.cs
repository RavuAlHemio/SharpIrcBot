using System.Collections.Generic;

namespace SharpIrcBot.Plugins.GrammarGen.AST
{
    public abstract class WrapperProduction : Production
    {
        public Production Inner { get; }

        public WrapperProduction(Production inner)
        {
            Inner = inner;
        }

        public override void CollectSoundnessErrors(Rulebook rulebook, List<string> errors)
        {
            Inner.CollectSoundnessErrors(rulebook, errors);
        }
    }
}
