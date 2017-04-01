using Microsoft.EntityFrameworkCore;

namespace SharpIrcBot.Plugins.Trivia.ORM
{
    public class TriviaContext : DbContext
    {
        public DbSet<LeaderboardEntry> LeaderboardEntries { get; set; }

        public TriviaContext(DbContextOptions<TriviaContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<LeaderboardEntry>(entBuilder =>
            {
                entBuilder.ToTable("leaderboard", schema: "trivia");
                entBuilder.HasKey(le => le.NicknameLowercase);

                entBuilder.Property(le => le.NicknameLowercase)
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasColumnName("nickname_lowercase");
                entBuilder.Property(le => le.CorrectAnswers)
                    .IsRequired()
                    .HasColumnName("correct_answers");
            });
        }
    }
}
