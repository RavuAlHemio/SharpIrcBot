using System.Data.Common;
using System.Data.Entity;

namespace LinkInfoOptIn.ORM
{
    public class LinkInfoOptInContext : DbContext
    {
        public DbSet<OptedInUser> OptedInUsers { get; set; }

        static LinkInfoOptInContext()
        {
            Database.SetInitializer<LinkInfoOptInContext>(null);
        }

        public LinkInfoOptInContext(DbConnection connectionToOwn) : base(connectionToOwn, true)
        {
        }
    }
}
