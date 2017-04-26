using Microsoft.EntityFrameworkCore;

namespace SharpIrcBot.Plugins.Messenger.ORM
{
    public class MessengerContext : DbContext
    {
        public DbSet<Message> Messages { get; set; }
        public DbSet<MessageOnRetainer> MessagesOnRetainer { get; set; }
        public DbSet<ReplayableMessage> ReplayableMessages { get; set; }
        public DbSet<IgnoreEntry> IgnoreList { get; set; }
        public DbSet<Quiescence> Quiescences { get; set; }
        public DbSet<PrivateMessage> PrivateMessages { get; set; }

        public MessengerContext(DbContextOptions<MessengerContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            BuildMessageEntity<Message>(builder, "messages");
            BuildMessageEntity<MessageOnRetainer>(builder, "messages_on_retainer");
            BuildMessageEntity<PrivateMessage>(builder, "private_messages");
            BuildMessageEntity<ReplayableMessage>(builder, "replayable_messages");

            builder.Entity<IgnoreEntry>(entBuilder =>
            {
                entBuilder.ToTable("ignore_list", schema: "messenger");
                entBuilder.HasKey(ie => new { ie.SenderLowercase, ie.RecipientLowercase });

                entBuilder.Property(ie => ie.SenderLowercase)
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasColumnName("sender_lowercase");
                entBuilder.Property(ie => ie.RecipientLowercase)
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasColumnName("recipient_lowercase");
            });

            builder.Entity<Quiescence>(entBuilder =>
            {
                entBuilder.ToTable("quiescences", schema: "messenger");
                entBuilder.HasKey(q => q.UserLowercase);

                entBuilder.Property(q => q.UserLowercase)
                    .IsRequired()
                    .HasColumnName("user_lowercase");
                entBuilder.Property(q => q.EndTimestamp)
                    .IsRequired()
                    .HasColumnName("end_timestamp");
            });
        }

        protected void BuildMessageEntity<T>(ModelBuilder builder, string tableName)
            where T : class, IMessage
        {
            builder.Entity<T>(entBuilder =>
            {
                entBuilder.ToTable(tableName, schema: "messenger");
                entBuilder.HasKey(m => m.ID);

                entBuilder.Property(m => m.ID)
                    .IsRequired()
                    .HasColumnName("message_id")
                    .ValueGeneratedOnAdd();
                entBuilder.Property(m => m.Timestamp)
                    .IsRequired()
                    .HasColumnName("timestamp");
                entBuilder.Property(m => m.SenderOriginal)
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasColumnName("sender_original");
                entBuilder.Property(m => m.RecipientLowercase)
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasColumnName("recipient_lowercase");
                entBuilder.Property(m => m.Body)
                    .IsRequired()
                    .HasColumnName("body");
                entBuilder.Property(m => m.ExactNickname)
                    .IsRequired()
                    .HasDefaultValue(false)
                    .HasColumnName("exact_nickname");
            });
        }
    }
}
