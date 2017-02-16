using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Allograph.RegularExpressions.Internal
{
    public class ReplacementState
    {
        public string InputString { get; set; }
        public Regex Regex { get; set; }
        public Match Match { get; set; }
        public Dictionary<string, string> Lookups { get; set; }
    }
}
