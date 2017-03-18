using Microsoft.EntityFrameworkCore;

namespace SharpIrcBot.Plugins.IKnewThat.ORM
{
    public class IKnewThatContext : DbContext
    {
        public DbSet<IKnewThatEntry> Entries { get; set; }

        public IKnewThatContext(DbContextOptions<IKnewThatContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<IKnewThatEntry>(entBuilder =>
            {
                entBuilder.ToTable("entries", schema: "i_knew_that");
                entBuilder.HasKey(e => new { e.AuthorLowercase, e.KeywordLowercase });

                entBuilder.Property(e => e.AuthorLowercase)
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasColumnName("author_lowercase");
                entBuilder.Property(e => e.KeywordLowercase)
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasColumnName("keyword");
                entBuilder.Property(e => e.Timestamp)
                    .IsRequired()
                    .HasColumnName("timestamp");
                entBuilder.Property(e => e.Message)
                    .IsRequired()
                    .HasColumnName("message");
            });
        }
    }
}
