using Microsoft.EntityFrameworkCore;

namespace LinkInfoOptIn.ORM
{
    public class LinkInfoOptInContext : DbContext
    {
        public DbSet<OptedInUser> OptedInUsers { get; set; }

        public LinkInfoOptInContext(DbContextOptions<LinkInfoOptInContext> options)
            : base(options)
        {
        }
    }
}
