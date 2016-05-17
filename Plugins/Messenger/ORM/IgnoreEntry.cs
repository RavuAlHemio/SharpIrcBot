using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Messenger.ORM
{
    [Table("ignore_list", Schema = "messenger")]
    public class IgnoreEntry
    {
        [Key]
        [Required]
        [Column("sender_lowercase", Order = 1)]
        [MaxLength(255)]
        public string SenderLowercase { get; set; }

        [Key]
        [Required]
        [Column("recipient_lowercase", Order = 2)]
        [MaxLength(255)]
        public string RecipientLowercase { get; set; }
    }
}
