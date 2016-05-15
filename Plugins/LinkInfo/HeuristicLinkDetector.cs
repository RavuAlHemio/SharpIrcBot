using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using JetBrains.Annotations;

namespace LinkInfo
{
    public class HeuristicLinkDetector
    {
        [NotNull]
        protected HashSet<string> TopLevelDomains { get; set; }
        [NotNull]
        protected IdnMapping IDNMapping { get; set; }

        public HeuristicLinkDetector(IEnumerable<string> topLevelDomains)
        {
            TopLevelDomains = new HashSet<string>(topLevelDomains);
            IDNMapping = new IdnMapping();
        }

        public bool TryCreateUri(string word, out Uri uri)
        {
            uri = null;

            // fail fast for obvious non-URLs
            if (word.All(c => c != '.' && c != '/'))
            {
                return false;
            }

            // would this word make sense with http:// in front of it?
            if (!Uri.TryCreate("http://" + word, UriKind.Absolute, out uri))
            {
                // nope
                return false;
            }

            // does the host have at least one dot?
            if (uri.Host.All(c => c != '.'))
            {
                return false;
            }

            // check host against list of TLDs
            var tld = IDNMapping.GetAscii(uri.Host.Split('.').Last()).ToLowerInvariant();
            if (!TopLevelDomains.Contains(tld))
            {
                // invalid TLD; probably not a URI
                uri = null;
                return false;
            }

            // it probably is a URI
            return true;
        }
    }
}
