using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DatabaseNickMapping.ORM
{
    [Table("base_nicknames", Schema = "nick_mapping")]
    public class BaseNickname
    {
        [Key]
        [Required]
        [Column("nickname", Order = 1)]
        [MaxLength(255)]
        public string Nickname { get; set; }

        [Required]
        [Column("nickname_lower", Order = 2)]
        [MaxLength(255)]
        public string NicknameLowercase { get; set; }

        public virtual ICollection<NickMapping> Mappings { get; set; }
    }
}
