using System;

namespace SharpIrcBot
{
    [Flags]
    public enum MessageFlags : long
    {
        None = 0,

        /// <summary>
        /// The user is banned.
        /// </summary>
        UserBanned = 1
    }
}

