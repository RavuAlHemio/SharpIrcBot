using Microsoft.EntityFrameworkCore;

namespace Quotes.ORM
{
    public class QuotesContext : DbContext
    {
        public DbSet<Quote> Quotes { get; set; }
        public DbSet<QuoteVote> QuoteVotes { get; set; }

        public QuotesContext(DbContextOptions<QuotesContext> options)
            : base(options)
        {
        }
    }
}
