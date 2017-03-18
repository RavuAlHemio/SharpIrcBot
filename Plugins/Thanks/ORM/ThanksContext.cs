using Microsoft.EntityFrameworkCore;

namespace SharpIrcBot.Plugins.Thanks.ORM
{
    public class ThanksContext : DbContext
    {
        public DbSet<ThanksEntry> ThanksEntries { get; set; }

        public ThanksContext(DbContextOptions<ThanksContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<ThanksEntry>(entBuilder =>
            {
                entBuilder.ToTable("thanks", schema: "thanks");
                entBuilder.HasKey(t => t.ID);

                entBuilder.Property(t => t.ID)
                    .IsRequired()
                    .HasColumnName("thanks_id")
                    .ValueGeneratedOnAdd();
                entBuilder.Property(t => t.Timestamp)
                    .IsRequired()
                    .HasColumnName("timestamp");
                entBuilder.Property(t => t.ThankerLowercase)
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasColumnName("thanker_lowercase");
                entBuilder.Property(t => t.ThankeeLowercase)
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasColumnName("thankee_lowercase");
                entBuilder.Property(t => t.Channel)
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasColumnName("channel");
                entBuilder.Property(t => t.Reason)
                    .HasMaxLength(255)
                    .HasColumnName("reason")
                    .HasDefaultValue(null);
                entBuilder.Property(t => t.Deleted)
                    .IsRequired()
                    .HasColumnName("deleted");
            });
        }
    }
}
