using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Messenger.ORM
{
    [Table("quiescences", Schema = "messenger")]
    public class Quiescence
    {
        [Key]
        [Required]
        [Column("user_lowercase", Order = 1)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public string UserLowercase { get; set; }

        [Required]
        [Column("end_timestamp", Order = 2)]
        public DateTimeOffset EndTimestamp { get; set; }
    }
}
