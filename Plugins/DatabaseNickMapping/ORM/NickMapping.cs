using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace DatabaseNickMapping.ORM
{
    [Table("nick_mappings", Schema = "nick_mapping")]
    public class NickMapping
    {
        [Key]
        [Required]
        [Column("base_nickname", Order = 1)]
        public string BaseNickname { get; set; }

        [ForeignKey("BaseNickname")]
        public BaseNickname BaseNicknameObject { get; set; }

        [Key]
        [Required]
        [Column("mapped_nickname_lower", Order = 2)]
        public string MappedNicknameLowercase { get; set; }
    }
}
