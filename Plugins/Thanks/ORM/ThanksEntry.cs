﻿using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using JetBrains.Annotations;

namespace Thanks.ORM
{
    [Table("thanks", Schema = "thanks")]
    public class ThanksEntry
    {
        [Key]
        [Required]
        [Column("thanks_id", Order = 1)]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long ID { get; set; }

        [Required]
        [Column("timestamp", Order = 2)]
        public DateTime Timestamp { get; set; }

        [Required]
        [Column("thanker_lowercase", Order = 3)]
        [MaxLength(255)]
        public string ThankerLowercase { get; set; }

        [Required]
        [Column("thankee_lowercase", Order = 4)]
        [MaxLength(255)]
        public string ThankeeLowercase { get; set; }

        [Required]
        [Column("channel", Order = 5)]
        [MaxLength(255)]
        public string Channel { get; set; }

        [CanBeNull, DefaultValue(null)]
        [Column("reason", Order = 6)]
        [MaxLength(255)]
        public string Reason { get; set; }

        [Required]
        [Column("deleted", Order = 7)]
        public bool Deleted { get; set; }
    }
}
