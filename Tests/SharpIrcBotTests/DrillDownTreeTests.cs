using System.Collections.Immutable;
using AlsoKnownAs;
using Xunit;

namespace SharpIrcBotTests
{
    public class DrillDownTreeTests
    {
        [Fact]
        public void TestTree()
        {
            var tree = new DrillDownTree<string, int>();
            tree[ImmutableList.Create("one", "two", "three")] = 4;
            tree[ImmutableList.Create("one", "two", "four")] = 5;
        }
    }
}
