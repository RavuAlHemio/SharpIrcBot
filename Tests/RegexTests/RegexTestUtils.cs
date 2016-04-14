using System;
using System.Text;
using System.Collections.Generic;

namespace RegexTests
{
    public static class RegexTestUtils
    {
        public const int DefaultSpaceOutCount = 5;

        public static IEnumerable<string> SpaceOut(int spaceOutCount, params string[] pieces)
        {
            if (pieces.Length == 1)
            {
                // start at zero for the trailing spaces
                for (int i = 0; i <= spaceOutCount; ++i)
                {
                    yield return new StringBuilder()
                        .Append(pieces[0])
                        .Append(' ', i)
                        .ToString();
                }
                yield break;
            }

            // strip off the first piece
            var subPieces = new string[pieces.Length - 1];
            Array.Copy(pieces, 1, subPieces, 0, subPieces.Length);

            // run the subtree
            var spaceOutSubtree = new List<string>(SpaceOut(spaceOutCount, subPieces));

            // Cartesian product!
            for (int i = 1; i <= spaceOutCount; ++i)
            foreach (var sub in spaceOutSubtree)
            {
                yield return new StringBuilder()
                    .Append(pieces[0])
                    .Append(' ', i)
                    .Append(sub)
                    .ToString();
            }
        }

        public static IEnumerable<string> SpaceOut(params string[] pieces)
        {
            return SpaceOut(DefaultSpaceOutCount, pieces);
        }
    }
}
