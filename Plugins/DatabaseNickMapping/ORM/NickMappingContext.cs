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
    }
}
