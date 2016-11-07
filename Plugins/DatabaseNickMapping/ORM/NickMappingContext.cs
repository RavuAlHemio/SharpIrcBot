using Microsoft.EntityFrameworkCore;

namespace DatabaseNickMapping.ORM
{
    public class NickMappingContext : DbContext
    {
        public DbSet<BaseNickname> BaseNicknames { get; set; }
        public DbSet<NickMapping> NickMappings { get; set; }

        public NickMappingContext(DbContextOptions<NickMappingContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<BaseNickname>(entBuilder =>
            {
                entBuilder.ToTable("base_nicknames", schema: "nick_mapping");
                entBuilder.HasKey(bn => bn.Nickname);

                entBuilder.Property(bn => bn.Nickname)
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasColumnName("nickname");
            });

            builder.Entity<NickMapping>(entBuilder =>
            {
                entBuilder.ToTable("nick_mappings", schema: "nick_mapping");
                entBuilder.HasKey(nm => new { nm.BaseNickname, nm.MappedNicknameLowercase });

                entBuilder.Property(nm => nm.BaseNickname)
                    .IsRequired()
                    .HasColumnName("base_nickname");

                entBuilder.Property(nm => nm.MappedNicknameLowercase)
                    .IsRequired()
                    .HasColumnName("mapped_nickname_lower");

                entBuilder.HasOne(nm => nm.BaseNicknameObject)
                    .WithMany(bn => bn.Mappings)
                    .HasForeignKey(nm => nm.BaseNickname);
            });
        }
    }
}
