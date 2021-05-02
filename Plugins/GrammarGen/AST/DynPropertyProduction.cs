using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace SharpIrcBot.Plugins.GrammarGen.AST
{
    public class DynPropertyProduction : Production
    {
        public string PropertyName { get; }

        public DynPropertyProduction(string propertyName)
        {
            PropertyName = propertyName;
        }

        public override string Produce(Random rng, Rulebook rulebook, ImmutableDictionary<string, object> parameters)
        {
            return (string)parameters[PropertyName];
        }

        public override IEnumerable<string> ProduceAll(Rulebook rulebook, ImmutableDictionary<string, object> parameters)
        {
            yield return (string)parameters[PropertyName];
        }

        public override CountBounds CountVariantBounds(Rulebook rulebook, ImmutableDictionary<string, object> parameters)
            => new CountBounds(1, 1);

        public override void CollectSoundnessErrors(Rulebook rulebook, List<string> errors)
        {
        }

        public override string ToString()
        {
            return $"<DynProperty({PropertyName})>";
        }
    }
}
