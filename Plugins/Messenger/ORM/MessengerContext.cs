using System.Data.Common;
using System.Data.Entity;

namespace Messenger.ORM
{
    public class MessengerContext : DbContext
    {
        public DbSet<Message> Messages { get; set; }
        public DbSet<MessageOnRetainer> MessagesOnRetainer { get; set; }
        public DbSet<ReplayableMessage> ReplayableMessages { get; set; }
        public DbSet<IgnoreEntry> IgnoreList { get; set; }
        public DbSet<Quiescence> Quiescences { get; set; }
        public DbSet<PrivateMessage> PrivateMessages { get; set; }

        static MessengerContext()
        {
            Database.SetInitializer<MessengerContext>(null);
        }

        /*
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<IgnoreEntry>().HasKey(ie => new {
                ie.SenderFolded,
                ie.RecipientFolded
            });
        }
        */

        public MessengerContext(DbConnection connectionToOwn) : base(connectionToOwn, true)
        {
        }
    }
}
