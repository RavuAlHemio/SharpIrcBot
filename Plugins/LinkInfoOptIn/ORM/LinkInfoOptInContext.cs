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

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<OptedInUser>(entBuilder =>
            {
                entBuilder.ToTable("opted_in_users", schema: "link_info_opt_in");
                entBuilder.HasKey(e => e.UserName);

                entBuilder.Property(e => e.UserName)
                    .IsRequired()
                    .HasColumnName("user_name");
            });
        }
    }
}
