using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace SharpIrcBot.Plugins.GrammarGen.AST
{
    public class PlaceholderProduction : Production
    {
        public PlaceholderProduction()
        {
        }

        public override string Produce(Random rng, Rulebook rulebook, ImmutableDictionary<string, object> parameters)
        {
            throw new GrammarException("attempted to make a placeholder production produce a value");
        }

        public override void CollectSoundnessErrors(Rulebook rulebook, List<string> errors)
        {
            // don't worry about me, I'll be okay
        }

        public override string ToString()
        {
            return "<PLACEHOLDER>";
        }
    }
}
