using System;
using System.Linq;
using Xunit;
using SharpIrcBot.Plugins.Fact.Uncyclopedia;

namespace SharpIrcBot.Tests.FactTests
{
    public class WikitextLinkTests
    {
        static void CheckLinks(string input, string expectedStripped, params string[] expectedUrlStrings)
        {
            Uri[] expectedUrls = expectedUrlStrings
                .Select(us => new Uri(us))
                .ToArray();
            var baseUri = new Uri("https://en.wikipedia.org/wiki/");
            var (stripped, links) = UncyclopediaSourcePlugin.DefaultExtractWikitextLinks(input, baseUri);
            Assert.Equal(expectedStripped, stripped);
            Assert.Equal(expectedUrls, links);
        }

        [Fact]
        public void TestNoLinks()
        {
            CheckLinks(
                "Did you know that a lack of links is not a link of lacks?",
                "Did you know that a lack of links is not a link of lacks?"
            );
        }

        [Fact]
        public void TestUnterminatedSquareBrackets()
        {
            CheckLinks(
                "Did you know that a [[syntax error is always fun?",
                "Did you know that a [[syntax error is always fun?"
            );
        }

        [Fact]
        public void TestLinksWithoutPipe()
        {
            CheckLinks(
                "Did you know that [[Siemens]] is in [[Püssi]]?",
                "Did you know that Siemens is in Püssi?",
                "https://en.wikipedia.org/wiki/Siemens",
                "https://en.wikipedia.org/wiki/Püssi"
            );

            CheckLinks(
                "Did you know that [[Jackson County, Florida]] elected a [[Wankard]] to represent it?",
                "Did you know that Jackson County, Florida elected a Wankard to represent it?",
                "https://en.wikipedia.org/wiki/Jackson_County,_Florida",
                "https://en.wikipedia.org/wiki/Wankard"
            );

            CheckLinks(
                "[[Some links]] appear at the beginning or [[the end]]",
                "Some links appear at the beginning or the end",
                "https://en.wikipedia.org/wiki/Some_links",
                "https://en.wikipedia.org/wiki/The_end"
            );
        }

        [Fact]
        public void TestLinksWithPipe()
        {
            CheckLinks(
                "Did you know that [[Siemens AG|Siemens]] is in [[Püssi, Estonia|Püssi]]?",
                "Did you know that Siemens is in Püssi?",
                "https://en.wikipedia.org/wiki/Siemens_AG",
                "https://en.wikipedia.org/wiki/Püssi,_Estonia"
            );

            CheckLinks(
                "Did you know that [[Jackson County, Florida]] elected a [[Wankard Pooser|Wankard]] to represent it?",
                "Did you know that Jackson County, Florida elected a Wankard to represent it?",
                "https://en.wikipedia.org/wiki/Jackson_County,_Florida",
                "https://en.wikipedia.org/wiki/Wankard_Pooser"
            );

            CheckLinks(
                "Did you know that [[:File:Beautiful picture.jpeg|]] does not abide by Wikipedia's [[Wikipedia:Guideline for Beautiful Pictures|]]?",
                "Did you know that Beautiful picture.jpeg does not abide by Wikipedia's Guideline for Beautiful Pictures?",
                "https://en.wikipedia.org/wiki/File%3ABeautiful_picture.jpeg",
                "https://en.wikipedia.org/wiki/Wikipedia%3AGuideline_for_Beautiful_Pictures"
            );
        }
    }
}
