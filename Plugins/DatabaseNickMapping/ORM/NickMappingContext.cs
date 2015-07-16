using System.Data.Entity;
using System.Data.Common;

namespace DatabaseNickMapping.ORM
{
    public class NickMappingContext : DbContext
    {
        public DbSet<BaseNickname> BaseNicknames { get; set; }
        public DbSet<NickMapping> NickMappings { get; set; }

        static NickMappingContext()
        {
            Database.SetInitializer<NickMappingContext>(null);
        }

        public NickMappingContext(DbConnection connectionToOwn) : base(connectionToOwn, true)
        {
        }
    }
}
