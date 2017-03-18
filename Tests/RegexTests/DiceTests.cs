using System;
using System.Linq;
using System.Text.RegularExpressions;
using SharpIrcBot.Plugins.Dice;
using Xunit;

namespace SharpIrcBot.Tests.RegexTests
{
    public class DiceTests
    {
        private static void TestDiceThrowRegexValid(string testString, string firstRoll, params string[] otherRolls)
        {
            var match = DicePlugin.DiceThrowRegex.Match(testString);
            Assert.True(match.Success);
            Assert.True(match.Groups["firstRoll"].Success);
            Assert.Equal(firstRoll, match.Groups["firstRoll"].Value);
            Assert.Equal(otherRolls.Length, match.Groups["nextRoll"].Captures.Count);
            foreach (var thingy in Enumerable.Zip(otherRolls, match.Groups["nextRoll"].Captures.OfType<Capture>(), Tuple.Create))
            {
                string expected = thingy.Item1;
                string actual = thingy.Item2.Value;
                Assert.Equal(expected, actual);
            }
        }

        private static void TestDiceThrowRegexInvalid(string testString)
        {
            Assert.False(DicePlugin.DiceThrowRegex.IsMatch(testString));
        }

        [Fact]
        public void TestDiceThrowRegex()
        {
            // "!roll d6", "!roll 1d6", "!roll 10d128"
            foreach (var testString in RegexTestUtils.SpaceOut("!roll", "d6"))
            {
                TestDiceThrowRegexValid(testString, "d6");
            }
            foreach (var testString in RegexTestUtils.SpaceOut("!roll", "1d6"))
            {
                TestDiceThrowRegexValid(testString, "1d6");
            }
            foreach (var testString in RegexTestUtils.SpaceOut("!roll", "10d128"))
            {
                TestDiceThrowRegexValid(testString, "10d128");
            }

            // "!roll 6d6 1d20"
            foreach (var testString in RegexTestUtils.SpaceOut("!roll", "6d6", "1d20"))
            {
                TestDiceThrowRegexValid(testString, "6d6", "1d20");
            }

            TestDiceThrowRegexValid("!roll 6d6, 1d20", "6d6", "1d20");

            // "!roll 6d6 , 1d20"
            foreach (var testString in RegexTestUtils.SpaceOut("!roll", "6d6", "1d20"))
            {
                TestDiceThrowRegexValid(testString, "6d6", "1d20");
            }

            // "!roll 6d6 ,, 1d20"
            foreach (var testString in RegexTestUtils.SpaceOut("!roll", "6d6", ",,", "1d20"))
            {
                TestDiceThrowRegexValid(testString, "6d6", "1d20");
            }

            // "!roll 6d6 , , 1d20"
            foreach (var testString in RegexTestUtils.SpaceOut("!roll", "6d6", ",", ",", "1d20"))
            {
                TestDiceThrowRegexValid(testString, "6d6", "1d20");
            }

            // "!roll 6d6 , , 1d20 ,, ,,,,, d654" [reduce to max 3 spaces]
            foreach (var testString in RegexTestUtils.SpaceOut(3, "!roll", "6d6", ",", ",", "1d20", ",,", ",,,,,", "d654"))
            {
                TestDiceThrowRegexValid(testString, "6d6", "1d20", "d654");
            }

            // zero dice or zero sides: "!roll 0d6", "!roll 6d0"
            foreach (var testString in RegexTestUtils.SpaceOut("!roll", "0d6"))
            {
                TestDiceThrowRegexInvalid(testString);
            }
            foreach (var testString in RegexTestUtils.SpaceOut("!roll", "6d0"))
            {
                TestDiceThrowRegexInvalid(testString);
            }

            // trailing comma: "!roll 1d6,"
            foreach (var testString in RegexTestUtils.SpaceOut("!roll", "1d6,"))
            {
                TestDiceThrowRegexInvalid(testString);
            }

            // trailing comma: "!roll 1d6 ,"
            foreach (var testString in RegexTestUtils.SpaceOut("!roll", "1d6", ","))
            {
                TestDiceThrowRegexInvalid(testString);
            }

            // leading comma: "!roll ,1d6"
            foreach (var testString in RegexTestUtils.SpaceOut("!roll", ",1d6"))
            {
                TestDiceThrowRegexInvalid(testString);
            }

            // leading comma: "!roll , 1d6"
            foreach (var testString in RegexTestUtils.SpaceOut("!roll", ",", "1d6"))
            {
                TestDiceThrowRegexInvalid(testString);
            }

            // missing space: "!roll1d6"
            foreach (var testString in RegexTestUtils.SpaceOut("!roll1d6"))
            {
                TestDiceThrowRegexInvalid(testString);
            }
        }
    }
}
