using Microsoft.EntityFrameworkCore;

namespace SharpIrcBot.Plugins.Quotes.ORM
{
    public class QuotesContext : DbContext
    {
        public DbSet<Quote> Quotes { get; set; }
        public DbSet<QuoteVote> QuoteVotes { get; set; }

        public QuotesContext(DbContextOptions<QuotesContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<Quote>(entBuilder =>
            {
                entBuilder.ToTable("quotes", schema: "quotes");
                entBuilder.HasKey(q => q.ID);

                entBuilder.Property(q => q.ID)
                    .IsRequired()
                    .HasColumnName("quote_id")
                    .ValueGeneratedOnAdd();
                entBuilder.Property(q => q.Timestamp)
                    .IsRequired()
                    .HasColumnName("timestamp");
                entBuilder.Property(q => q.Channel)
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasColumnName("channel");
                entBuilder.Property(q => q.Author)
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasColumnName("author");
                entBuilder.Property(q => q.MessageType)
                    .IsRequired()
                    .HasMaxLength(1)
                    .HasColumnName("message_type");
                entBuilder.Property(q => q.Body)
                    .IsRequired()
                    .HasColumnName("body");
            });

            builder.Entity<QuoteVote>(entBuilder =>
            {
                entBuilder.ToTable("quote_votes", schema: "quotes");
                entBuilder.HasKey(qv => qv.ID);

                entBuilder.Property(qv => qv.ID)
                    .IsRequired()
                    .HasColumnName("vote_id")
                    .ValueGeneratedOnAdd();
                entBuilder.Property(qv => qv.QuoteID)
                    .IsRequired()
                    .HasColumnName("quote_id");
                entBuilder.Property(qv => qv.VoterLowercase)
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasColumnName("voter_lowercase");
                entBuilder.Property(qv => qv.Points)
                    .IsRequired()
                    .HasColumnName("points");

                entBuilder.HasOne(qv => qv.Quote)
                    .WithMany(q => q.Votes)
                    .HasForeignKey(qv => qv.QuoteID);
            });
        }
    }
}
