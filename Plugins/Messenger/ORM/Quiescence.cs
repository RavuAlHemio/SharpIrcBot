﻿using System;

namespace SharpIrcBot.Plugins.Messenger.ORM
{
    public class Quiescence
    {
        public string UserLowercase { get; set; }

        public DateTimeOffset EndTimestamp { get; set; }
    }
}
