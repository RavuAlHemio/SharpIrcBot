using Microsoft.EntityFrameworkCore;

namespace SharpIrcBot.Plugins.Demoderation.ORM
{
    public class DemoderationContext : DbContext
    {
        public DbSet<Criterion> Criteria { get; set; }
        public DbSet<Ban> Bans { get; set; }
        public DbSet<Abuse> Abuses { get; set; }
        public DbSet<Immunity> Immunities { get; set; }
        public DbSet<Permaban> Permabans { get; set; }

        public DemoderationContext(DbContextOptions<DemoderationContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.IfNpgsql(Database, npb =>
            {
                npb.HasSequence<long>("seq__criteria__id", schema: "demoderation")
                    .StartsAt(1);
                npb.HasSequence<long>("seq__bans__id", schema: "demoderation")
                    .StartsAt(1);
                npb.HasSequence<long>("seq__abuses__id", schema: "demoderation")
                    .StartsAt(1);
                npb.HasSequence<long>("seq__immunities__id", schema: "demoderation")
                    .StartsAt(1);
                npb.HasSequence<long>("seq__permabans__id", schema: "demoderation")
                    .StartsAt(1);
            });

            builder.Entity<Criterion>(criterion =>
            {
                criterion.ToTable("criteria", schema: "demoderation");
                criterion.HasKey(c => c.ID);

                criterion.Property(c => c.ID)
                    .IsRequired()
                    .HasColumnName("id")
                    .ValueGeneratedOnAdd()
                    .IfNpgsql(Database, npc =>
                        npc.HasDefaultValueSql("nextval('demoderation.seq__criteria__id')")
                    );

                criterion.Property(c => c.Name)
                    .IsRequired()
                    .HasColumnName("name");

                criterion.Property(c => c.Channel)
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasColumnName("channel");

                criterion.Property(c => c.DetectionRegex)
                    .IsRequired()
                    .HasColumnName("detection_regex");

                criterion.Property(c => c.Enabled)
                    .IsRequired()
                    .HasColumnName("enabled")
                    .HasDefaultValue(true);

                criterion.HasAlternateKey(c => new { c.Name, c.Channel });
            });

            builder.Entity<Ban>(ban =>
            {
                ban.ToTable("bans", schema: "demoderation");
                ban.HasKey(b => b.ID);

                ban.Property(b => b.ID)
                    .IsRequired()
                    .HasColumnName("id")
                    .ValueGeneratedOnAdd()
                    .IfNpgsql(Database, npb =>
                        npb.HasDefaultValueSql("nextval('demoderation.seq__bans__id')")
                    );

                ban.Property(b => b.CriterionID)
                    .IsRequired()
                    .HasColumnName("criterion_id")
                    .ValueGeneratedNever();

                ban.Property(b => b.OffenderNickname)
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasColumnName("offender_nickname");

                ban.Property(b => b.OffenderUsername)
                    .IsRequired(false)
                    .HasMaxLength(255)
                    .HasColumnName("offender_username");

                ban.Property(b => b.BannerNickname)
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasColumnName("banner_nickname");

                ban.Property(b => b.BannerUsername)
                    .IsRequired(false)
                    .HasMaxLength(255)
                    .HasColumnName("banner_username");

                ban.Property(b => b.Timestamp)
                    .IsRequired()
                    .HasColumnName("timestamp")
                    .IfNpgsql(Database, npb =>
                        npb.HasColumnType("timestamp (0) with timezone")
                    );

                ban.Property(b => b.BanUntil)
                    .IsRequired()
                    .HasColumnName("ban_until")
                    .IfNpgsql(Database, npb =>
                        npb.HasColumnType("timestamp (0) with timezone")
                    );

                ban.Property(b => b.Lifted)
                    .IsRequired()
                    .HasColumnName("lifted")
                    .HasDefaultValue(false);

                ban.HasOne(b => b.Criterion)
                    .WithMany()
                    .HasForeignKey(b => b.CriterionID);
            });

            builder.Entity<Abuse>(abuse =>
            {
                abuse.ToTable("abuses", schema: "demoderation");
                abuse.HasKey(a => a.ID);

                abuse.Property(a => a.ID)
                    .IsRequired()
                    .HasColumnName("id")
                    .ValueGeneratedOnAdd()
                    .IfNpgsql(Database, npa =>
                        npa.HasDefaultValueSql("nextval('demoderation.seq__abuses__id')")
                    );

                abuse.Property(a => a.BanID)
                    .IsRequired()
                    .HasColumnName("ban_id")
                    .ValueGeneratedNever();

                abuse.Property(a => a.OpNickname)
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasColumnName("op_nickname");

                abuse.Property(a => a.OpUsername)
                    .IsRequired(false)
                    .HasMaxLength(255)
                    .HasColumnName("op_username");

                abuse.Property(a => a.Timestamp)
                    .IsRequired()
                    .HasColumnName("timestamp")
                    .IfNpgsql(Database, npa =>
                        npa.HasColumnType("timestamp (0) with timezone")
                    );

                abuse.Property(a => a.BanUntil)
                    .IsRequired()
                    .HasColumnName("ban_until")
                    .IfNpgsql(Database, npa =>
                        npa.HasColumnType("timestamp (0) with timezone")
                    );

                abuse.Property(a => a.LockUntil)
                    .IsRequired()
                    .HasColumnName("lock_until")
                    .IfNpgsql(Database, npa =>
                        npa.HasColumnType("timestamp (0) with timezone")
                    );

                abuse.Property(a => a.Lifted)
                    .IsRequired()
                    .HasColumnName("lifted")
                    .HasDefaultValue(false);

                abuse.HasOne(a => a.Ban)
                    .WithMany()
                    .HasForeignKey(a => a.BanID);
            });

            builder.Entity<Immunity>(immunity =>
            {
                immunity.ToTable("immunities", schema: "demoderation");
                immunity.HasKey(a => a.ID);

                immunity.Property(i => i.ID)
                    .IsRequired()
                    .HasColumnName("id")
                    .ValueGeneratedOnAdd()
                    .IfNpgsql(Database, npi =>
                        npi.HasDefaultValueSql("nextval('demoderation.seq__immunities__id')")
                    );

                immunity.Property(i => i.NicknameOrUsername)
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasColumnName("nickname_or_username");

                immunity.Property(i => i.Channel)
                    .IsRequired(false)
                    .HasMaxLength(255)
                    .HasColumnName("channel");
            });

            builder.Entity<Permaban>(permaban =>
            {
                permaban.ToTable("permabans", schema: "demoderation");
                permaban.HasKey(b => b.ID);

                permaban.Property(b => b.ID)
                    .IsRequired()
                    .HasColumnName("id")
                    .ValueGeneratedOnAdd()
                    .IfNpgsql(Database, npb =>
                        npb.HasDefaultValueSql("nextval('demoderation.seq__permabans__id')")
                    );

                permaban.Property(b => b.NicknameOrUsername)
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasColumnName("nickname_or_username");

                permaban.Property(b => b.Channel)
                    .IsRequired(false)
                    .HasMaxLength(255)
                    .HasColumnName("channel");
            });
        }
    }
}
