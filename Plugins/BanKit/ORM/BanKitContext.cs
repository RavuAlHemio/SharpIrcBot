using Microsoft.EntityFrameworkCore;
using SharpIrcBot.Util;

namespace SharpIrcBot.Plugins.BanKit.ORM
{
    public class BanKitContext : DbContext
    {
        public DbSet<BanEntry> BanEntries { get; set; }

        public BanKitContext(DbContextOptions<BanKitContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.IfNpgsql(Database, b =>
            {
                b.HasSequence("seq__ban_entries__id", schema: "ban_kit")
                    .StartsAt(1);
            });

            builder.Entity<BanEntry>(entBuilder =>
            {
                entBuilder.ToTable("ban_entries", schema: "ban_kit");
                entBuilder.HasKey(be => be.ID);

                entBuilder.Property(be => be.ID)
                    .IsRequired()
                    .HasColumnName("id")
                    .ValueGeneratedOnAdd()
                    .IfNpgsql(Database, npc =>
                        npc.HasDefaultValueSql("nextval('ban_kit.seq__ban_entries__id')")
                    );
                entBuilder.Property(be => be.BannedNick)
                    .IsRequired(false)
                    .HasMaxLength(255)
                    .HasColumnName("banned_nick");
                entBuilder.Property(be => be.BannedMask)
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasColumnName("banned_mask");
                entBuilder.Property(be => be.BannerNick)
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasColumnName("banner_nick");
                entBuilder.Property(be => be.Channel)
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasColumnName("channel");
                entBuilder.Property(be => be.TimestampBanStart)
                    .IsRequired()
                    .HasColumnName("timestamp_ban_start");
                entBuilder.Property(be => be.TimestampBanEnd)
                    .IsRequired()
                    .HasColumnName("timestamp_ban_end");
                entBuilder.Property(be => be.Reason)
                    .IsRequired(false)
                    .HasColumnName("reason");
                entBuilder.Property(be => be.Lifted)
                    .IsRequired()
                    .HasColumnName("lifted");
            });
        }
    }
}
