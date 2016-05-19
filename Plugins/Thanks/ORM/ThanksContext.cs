using System.Data.Common;
using System.Data.Entity;

namespace Thanks.ORM
{
    public class ThanksContext : DbContext
    {
        public DbSet<ThanksEntry> ThanksEntries { get; set; }

        static ThanksContext()
        {
            Database.SetInitializer<ThanksContext>(null);
        }

        public ThanksContext(DbConnection connectionToOwn) : base(connectionToOwn, true)
        {
        }
    }
}
