using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quotes.ORM
{
    [Table("quote_votes", Schema = "quotes")]
    public class QuoteVote
    {
        [Key]
        [Required]
        [Column("vote_id", Order = 1)]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long ID { get; set; }

        [Required]
        [Column("quote_id", Order = 2)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long QuoteID { get; set; }

        [Required]
        [Column("voter_lowercase", Order = 3)]
        [MaxLength(255)]
        public string VoterLowercase { get; set; }

        [Required]
        [Column("points", Order = 4)]
        public short Points { get; set; }
    }
}
