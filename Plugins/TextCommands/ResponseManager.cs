using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using SharpIrcBot;

namespace TextCommands
{
    public class ResponseManager
    {
        public List<string> Responses { get; set; }

        public List<int> CurrentIndexes { get; set; }

        public int NextIndexIndex { get; set; }

        public ResponseManager(IEnumerable<string> responses, Random rng)
        {
            Responses = new List<string>(responses);
            CurrentIndexes = new List<int>(Enumerable.Range(0, Responses.Count));
            CurrentIndexes.Shuffle(rng);
            NextIndexIndex = 0;
        }

        public string NextResponse(Random rng)
        {
            switch (Responses.Count)
            {
                case 0:
                    return null;
                case 1:
                    return Responses[0];
                default:
                    string response = Responses[CurrentIndexes[NextIndexIndex]];
                    ++NextIndexIndex;
                    if (NextIndexIndex >= CurrentIndexes.Count)
                    {
                        NextIndexIndex = 0;
                        CurrentIndexes.Shuffle(rng);
                    }
                    return response;
            }
        }

        [ContractInvariantMethod]
        private void ObjectInvariant()
        {
            // number of responses equals number of indexes
            Contract.Invariant(Responses.Count == CurrentIndexes.Count);

            // indexes are a permutation of the range 0..Count-1
            Contract.Invariant(CurrentIndexes.OrderBy(i => i).SequenceEqual(Enumerable.Range(0, CurrentIndexes.Count)));

            // NextIndexIndex is within 0..Count-1
            Contract.Invariant(NextIndexIndex >= 0 && NextIndexIndex < CurrentIndexes.Count);
        }
    }
}
