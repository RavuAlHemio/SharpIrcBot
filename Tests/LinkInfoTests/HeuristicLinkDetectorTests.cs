using System;
using System.Collections.Generic;
using SharpIrcBot.Plugins.LinkInfo;
using Xunit;

namespace SharpIrcBot.Tests.LinkInfoTests
{
    public class HeuristicLinkDetectorTests
    {
        [Fact]
        public void TestDomainDetection()
        {
            // short list for testing purposes
            var tlds = new HashSet<string>
            {
                "com",
                "org",
                "edu",
                "mil",
                "gov",
                "net"
            };
            var detector = new HeuristicLinkDetector(tlds);

            AssertValidUri(detector, "omg.org", "http://omg.org/");
            AssertInvalidUri(detector, "omg.aero");
            AssertValidUri(detector, "upload.wikimedia.org/wikipedia/commons/c/c5/Logo_FC_Bayern_M%C3%BCnchen.svg", "http://upload.wikimedia.org/wikipedia/commons/c/c5/Logo_FC_Bayern_M%C3%BCnchen.svg");
            AssertInvalidUri(detector, "test@omg.org");
        }

        private void AssertValidUri(HeuristicLinkDetector detector, string input, string absoluteUri)
        {
            Uri uri;
            Assert.True(detector.TryCreateUri(input, out uri));
            Assert.Equal(absoluteUri, uri.AbsoluteUri);
        }

        private void AssertInvalidUri(HeuristicLinkDetector detector, string input)
        {
            Uri uri;
            Assert.False(detector.TryCreateUri(input, out uri));
        }
    }
}
