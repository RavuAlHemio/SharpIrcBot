using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace SharpIrcBot.Plugins.GrammarGen.AST
{
    public class DynPersonProduction : Production
    {
        protected bool ChosenPerson { get; }

        public DynPersonProduction(bool chosenPerson = false)
        {
            ChosenPerson = chosenPerson;
        }

        protected virtual ImmutableArray<string> GetNicknames(ImmutableDictionary<string, object> parameters)
        {
            var nicks = (IReadOnlyList<string>)parameters["nicknames"];

            if (ChosenPerson)
            {
                var chosenNick = (string)parameters["message"];
                string knownNick = nicks
                    .FirstOrDefault(n => string.Equals(n, chosenNick, StringComparison.OrdinalIgnoreCase));
                if (knownNick != null)
                {
                    return ImmutableArray.Create(knownNick);
                }
            }

            return nicks.ToImmutableArray();
        }

        public override string Produce(Random rng, Rulebook rulebook, ImmutableDictionary<string, object> parameters)
        {
            var nicks = GetNicknames(parameters);
            if (nicks.Length == 0)
            {
                throw new NoProductionsRemainException(this);
            }
            return nicks[rng.Next(nicks.Length)];
        }

        public override IEnumerable<string> ProduceAll(Rulebook rulebook, ImmutableDictionary<string, object> parameters)
        {
            var nicks = GetNicknames(parameters);
            if (nicks.Length == 0)
            {
                throw new NoProductionsRemainException(this);
            }

            foreach (string nick in nicks)
            {
                yield return nick;
            }
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
