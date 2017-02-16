using System;

namespace Allograph.RegularExpressions.Internal
{
    public class MatchGroupPlaceholder : IPlaceholder
    {
        public string GroupName { get; }

        public int GroupIndex { get; }

        public MatchGroupPlaceholder(string groupName)
        {
            if (groupName == null)
            {
                throw new ArgumentNullException(nameof(groupName));
            }

            GroupName = groupName;
            GroupIndex = -1;
        }

        public MatchGroupPlaceholder(int groupIndex)
        {
            if (groupIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(groupIndex));
            }

            GroupName = null;
            GroupIndex = groupIndex;
        }

        public string Replace(ReplacementState state)
        {
            return (GroupName == null)
                ? state.Match.Groups[GroupIndex].Value
                : state.Match.Groups[GroupName].Value;
        }
    }
}
