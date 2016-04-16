using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LinkInfoOptIn.ORM
{
    [Table("opted_in_users", Schema = "link_info_opt_in")]
    public class OptedInUser
    {
        [Key]
        [Required]
        [Column("user_name", Order = 1)]
        public string UserName { get; set; }
    }
}
