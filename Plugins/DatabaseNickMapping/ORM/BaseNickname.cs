using System.Collections.Generic;

namespace SharpIrcBot.Plugins.DatabaseNickMapping.ORM
{
    public class BaseNickname
    {
        public string Nickname { get; set; }

        public virtual ICollection<NickMapping> Mappings { get; set; }
    }
}
