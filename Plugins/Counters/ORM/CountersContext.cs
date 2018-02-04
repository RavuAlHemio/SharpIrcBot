using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace SharpIrcBot.Plugins.Counters.ORM
{
    public class CountersContext : DbContext
    {
        public DbSet<CounterEntry> Entries { get; set; }

        public CountersContext(DbContextOptions<CountersContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.IfNpgsql(Database, b =>
            {
                b.HasSequence("seq__entries__id", schema: "counters")
                    .StartsAt(1);
            });

            builder.Entity<CounterEntry>(entBuilder =>
            {
                entBuilder.ToTable("entries", schema: "counters");
                entBuilder.HasKey(ce => ce.ID);
                entBuilder.HasIndex(ce => ce.Command)
                    .IfNpgsql(Database, b =>
                        b.HasName("idx__entries__command")
                    );

                entBuilder.Property(ce => ce.ID)
                    .IsRequired()
                    .HasColumnName("id")
                    .ValueGeneratedOnAdd()
                    .IfNpgsql(Database, b =>
                        b.HasDefaultValueSql("nextval('counters.seq__entries__id')")
                    );

                entBuilder.Property(ce => ce.Command)
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasColumnName("command");

                entBuilder.Property(ce => ce.HappenedTimestamp)
                    .IsRequired()
                    .HasColumnName("happened_timestamp");

                entBuilder.Property(ce => ce.CountedTimestamp)
                    .IsRequired()
                    .HasColumnName("counted_timestamp");

                entBuilder.Property(ce => ce.Channel)
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasColumnName("channel");

                entBuilder.Property(ce => ce.PerpNickname)
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasColumnName("perp_nickname");

                entBuilder.Property(ce => ce.PerpUsername)
                    .IsRequired(false)
                    .HasMaxLength(255)
                    .HasColumnName("perp_username");

                entBuilder.Property(ce => ce.CounterNickname)
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasColumnName("counter_nickname");

                entBuilder.Property(ce => ce.CounterUsername)
                    .IsRequired(false)
                    .HasMaxLength(255)
                    .HasColumnName("counter_username");

                entBuilder.Property(ce => ce.Message)
                    .IsRequired()
                    .HasColumnName("message");

                entBuilder.Property(ce => ce.Expunged)
                    .IsRequired()
                    .HasColumnName("expunged");
            });
        }
    }
}
