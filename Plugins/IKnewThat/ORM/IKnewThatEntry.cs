using System;

namespace IKnewThat.ORM
{
    public class IKnewThatEntry
    {
        public string AuthorLowercase { get; set; }

        public string KeywordLowercase { get; set; }

        public DateTimeOffset Timestamp { get; set; }

        public string Message { get; set; }
    }
}
