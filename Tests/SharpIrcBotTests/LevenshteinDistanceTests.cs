using System;
using SharpIrcBot.Util;
using Xunit;

namespace SharpIrcBot.Tests.SharpIrcBotTests
{
    public class LevenshteinDistanceTests
    {
        [Theory]
        [InlineData(3, "kitten", "sitting")]
        [InlineData(3, "Saturday", "Sunday")]
        [InlineData(4, "goose", "gander")]
        [InlineData(36, "the quick brown fox jumps over the lazy dog", "jackdaws love my big sphinx of quartz")]
        public void TestLevenshteinDistance(int distance, string one, string other)
        {
            Assert.Equal(distance, StringUtil.LevenshteinDistance(one, other));
            Assert.Equal(distance, StringUtil.LevenshteinDistance(other, one));
        }
    }
}
