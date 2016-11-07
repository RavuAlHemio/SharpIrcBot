using System;
using System.Collections.Generic;

namespace Quotes.ORM
{
    public class Quote
    {
        public long ID { get; set; }

        public DateTime Timestamp { get; set; }

        public string Channel { get; set; }

        public string Author { get; set; }

        public string MessageType { get; set; }

        public string Body { get; set; }

        public virtual ICollection<QuoteVote> Votes { get; set; }
    }
}
