using JetBrains.Annotations;
using Newtonsoft.Json.Linq;

namespace SharpIrcBot
{
    public interface IReloadableConfiguration
    {
        void ReloadConfiguration([NotNull] JObject newConfig);
    }
}
