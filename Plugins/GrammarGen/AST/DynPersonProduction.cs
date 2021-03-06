using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace SharpIrcBot.Plugins.GrammarGen.AST
{
    public class DynPersonProduction : Production
    {
        public DynPersonProduction()
        {
        }

        public override string Produce(Random rng, Rulebook rulebook, ImmutableDictionary<string, object> parameters)
        {
            var nicks = (IReadOnlyList<string>)parameters["nicknames"];
            return nicks[rng.Next(nicks.Count)];
        }

        public override void CollectSoundnessErrors(Rulebook rulebook, List<string> errors)
        {
        }

        public override string ToString()
        {
            return "<DynPerson>";
        }
    }
}
