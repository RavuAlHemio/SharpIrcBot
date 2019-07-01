using Microsoft.EntityFrameworkCore;
using SharpIrcBot.Util;

namespace SharpIrcBot.Plugins.ClientVersion.ORM
{
    public class ClientVersionContext : DbContext
    {
        public DbSet<VersionEntry> VersionEntries { get; set; }

        public ClientVersionContext(DbContextOptions<ClientVersionContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.IfNpgsql(Database, b =>
            {
                b.HasSequence("seq__version_entries__id", schema: "client_version")
                    .StartsAt(1);
            });

            builder.Entity<VersionEntry>(entBuilder =>
            {
                entBuilder.ToTable("version_entries", schema: "client_version");
                entBuilder.HasKey(ve => ve.ID);

                entBuilder.Property(ve => ve.ID)
                    .IsRequired()
                    .HasColumnName("id")
                    .ValueGeneratedOnAdd()
                    .IfNpgsql(Database, npc =>
                        npc.HasDefaultValueSql("nextval('client_version.seq__version_entries__id')")
                    );
                entBuilder.Property(ve => ve.Nickname)
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasColumnName("nickname");
                entBuilder.Property(ve => ve.VersionInfo)
                    .IsRequired()
                    .HasColumnName("version_info");
                entBuilder.Property(ve => ve.Timestamp)
                    .IsRequired()
                    .HasColumnName("timestamp");

                entBuilder.HasIndex(ve => ve.Nickname)
                    .IsUnique()
                    .IfNpgsql(Database, npa =>
                        npa.HasName("uq__client_version__nickname")
                    );
            });
        }
    }
}
