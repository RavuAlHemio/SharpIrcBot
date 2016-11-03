using Microsoft.EntityFrameworkCore;

namespace Thanks.ORM
{
    public class ThanksContext : DbContext
    {
        public DbSet<ThanksEntry> ThanksEntries { get; set; }

        public ThanksContext(DbContextOptions<ThanksContext> options)
            : base(options)
        {
        }
    }
}
