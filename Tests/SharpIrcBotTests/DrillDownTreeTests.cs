using System;
using System.Collections.Immutable;
using SharpIrcBot.Collections;
using Xunit;

namespace SharpIrcBotTests
{
    public class DrillDownTreeTests
    {
        protected void BestMatchesFullMatch(DrillDownTree<string, int> tree, ImmutableList<string> keyList, int expectedValue)
        {
            ImmutableList<int> matches;
            int depth = tree.GetBestMatches(keyList, out matches);

            // make sure depth is the same as the key length
            Assert.Equal(keyList.Count, depth);
            Assert.Equal(1, matches.Count);
            Assert.Equal(expectedValue, matches[0]);
        }

        protected void BestMatchesPartial(DrillDownTree<string, int> tree, ImmutableList<string> keyList, int expectedDepth, params int[] expectedValues)
        {
            ImmutableList<int> matches;
            int depth = tree.GetBestMatches(keyList, out matches);

            // make sure depth is the same as the key length
            Assert.Equal(expectedDepth, depth);
            Assert.Equal(expectedValues.Length, matches.Count);
            foreach (int expectedValue in expectedValues)
            {
                Assert.Contains(expectedValue, matches);
            }
        }

        [Fact]
        public void TestTree()
        {
            var oneTwoThree = ImmutableList.Create("one", "two", "three");
            var oneTwoFour = ImmutableList.Create("one", "two", "four");
            var oneTwoFive = ImmutableList.Create("one", "two", "five");
            var einsZwei = ImmutableList.Create("eins", "zwei");
            var einsElf = ImmutableList.Create("eins", "elf");
            var einsDroelf = ImmutableList.Create("eins", "droelf");
            var einsEmpty = ImmutableList.Create("eins", "");

            var tree = new DrillDownTree<string, int>();
            tree[oneTwoThree] = 4;
            tree[oneTwoFour] = 5;
            tree[einsZwei] = 12;
            tree[einsElf] = 22;

            Assert.Equal(4, tree[oneTwoThree]);
            Assert.Equal(5, tree[oneTwoFour]);
            Assert.Throws<IndexOutOfRangeException>(() => tree[oneTwoFive]);
            Assert.Equal(12, tree[einsZwei]);
            Assert.Equal(22, tree[einsElf]);
            Assert.Throws<IndexOutOfRangeException>(() => tree[einsDroelf]);
            Assert.Throws<IndexOutOfRangeException>(() => tree[einsEmpty]);

            BestMatchesFullMatch(tree, oneTwoThree, 4);
            BestMatchesFullMatch(tree, oneTwoFour, 5);
            BestMatchesFullMatch(tree, einsZwei, 12);
            BestMatchesFullMatch(tree, einsElf, 22);
            BestMatchesPartial(tree, oneTwoFive, 2, 4, 5);
            BestMatchesPartial(tree, einsDroelf, 1, 12, 22);
            BestMatchesPartial(tree, einsDroelf, 1, 22, 12);
            BestMatchesPartial(tree, einsEmpty, 1, 12, 22);
        }
    }
}
