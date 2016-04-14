using System.Linq;
using Xunit;

namespace RegexTests
{
    public class RegexTestUtilTests
    {
        [Fact]
        public void TestSpaceOut()
        {
            var oneTwoThree1 = RegexTestUtils.SpaceOut(1, "one", "two", "three").ToList();
            Assert.Equal(2, oneTwoThree1.Count);
            Assert.Contains("one two three", oneTwoThree1);
            Assert.Contains("one two three ", oneTwoThree1);

            var one3 = RegexTestUtils.SpaceOut(3, "one").ToList();
            Assert.Equal(4, one3.Count);
            Assert.Contains("one", one3);
            Assert.Contains("one ", one3);
            Assert.Contains("one  ", one3);
            Assert.Contains("one   ", one3);

            var oneTwoThree3 = RegexTestUtils.SpaceOut(3, "one", "two", "three").ToList();
            Assert.Equal(36, oneTwoThree3.Count);
            Assert.Contains("one two three", oneTwoThree3);
            Assert.Contains("one two three ", oneTwoThree3);
            Assert.Contains("one two three  ", oneTwoThree3);
            Assert.Contains("one two three   ", oneTwoThree3);
            Assert.Contains("one two  three", oneTwoThree3);
            Assert.Contains("one two  three ", oneTwoThree3);
            Assert.Contains("one two  three  ", oneTwoThree3);
            Assert.Contains("one two  three   ", oneTwoThree3);
            Assert.Contains("one two   three", oneTwoThree3);
            Assert.Contains("one two   three ", oneTwoThree3);
            Assert.Contains("one two   three  ", oneTwoThree3);
            Assert.Contains("one two   three   ", oneTwoThree3);
            Assert.Contains("one  two three", oneTwoThree3);
            Assert.Contains("one  two three ", oneTwoThree3);
            Assert.Contains("one  two three  ", oneTwoThree3);
            Assert.Contains("one  two three   ", oneTwoThree3);
            Assert.Contains("one  two  three", oneTwoThree3);
            Assert.Contains("one  two  three ", oneTwoThree3);
            Assert.Contains("one  two  three  ", oneTwoThree3);
            Assert.Contains("one  two  three   ", oneTwoThree3);
            Assert.Contains("one  two   three", oneTwoThree3);
            Assert.Contains("one  two   three ", oneTwoThree3);
            Assert.Contains("one  two   three  ", oneTwoThree3);
            Assert.Contains("one  two   three   ", oneTwoThree3);
            Assert.Contains("one   two three", oneTwoThree3);
            Assert.Contains("one   two three ", oneTwoThree3);
            Assert.Contains("one   two three  ", oneTwoThree3);
            Assert.Contains("one   two three   ", oneTwoThree3);
            Assert.Contains("one   two  three", oneTwoThree3);
            Assert.Contains("one   two  three ", oneTwoThree3);
            Assert.Contains("one   two  three  ", oneTwoThree3);
            Assert.Contains("one   two  three   ", oneTwoThree3);
            Assert.Contains("one   two   three", oneTwoThree3);
            Assert.Contains("one   two   three ", oneTwoThree3);
            Assert.Contains("one   two   three  ", oneTwoThree3);
            Assert.Contains("one   two   three   ", oneTwoThree3);
        }
    }
}
