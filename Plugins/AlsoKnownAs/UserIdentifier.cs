using System;
using JetBrains.Annotations;

namespace AlsoKnownAs
{
    public abstract class UserIdentifier : IEquatable<UserIdentifier>
    {
        public abstract bool Equals([CanBeNull] UserIdentifier other);

        public override bool Equals([CanBeNull] object obj)
        {
            var other = obj as UserIdentifier;
            if (other != null)
            {
                return this.Equals(other);
            }
            return false;
        }

        public abstract override int GetHashCode();
    }
}
