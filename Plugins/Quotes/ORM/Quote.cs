using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Quotes.ORM
{
    [Table("quotes", Schema = "quotes")]
    public class Quote
    {
        [Key]
        [Required]
        [Column("message_id", Order = 1)]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long ID { get; set; }

        [Required]
        [Column("timestamp", Order = 2)]
        public DateTime Timestamp { get; set; }

        [Required]
        [Column("channel", Order = 3)]
        [MaxLength(255)]
        public string Channel { get; set; }

        [Required]
        [Column("author", Order = 4)]
        [MaxLength(255)]
        public string Author { get; set; }

        [Required]
        [Column("author_lowercase", Order = 5)]
        [MaxLength(255)]
        public string AuthorLowercase { get; set; }

        [Required]
        [Column("message_type", Order = 6)]
        [MaxLength(1)]
        public string MessageType { get; set; }

        [Required]
        [Column("body", Order = 7)]
        public string Body { get; set; }

        [Required]
        [Column("body_lowercase", Order = 8)]
        public string BodyLowercase { get; set; }
    }
}
