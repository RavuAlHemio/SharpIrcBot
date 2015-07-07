using System.Data.Common;
using System.Data.Entity;

namespace Quotes.ORM
{
    public class QuotesContext : DbContext
    {
        public DbSet<Quote> Quotes { get; set; }
        public DbSet<QuoteVote> QuoteVotes { get; set; }

        static QuotesContext()
        {
            Database.SetInitializer<QuotesContext>(null);
        }

        public QuotesContext(DbConnection connectionToOwn) : base(connectionToOwn, true)
        {
        }
    }
}
