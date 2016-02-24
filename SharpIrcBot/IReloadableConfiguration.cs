using Newtonsoft.Json.Linq;

namespace SharpIrcBot
{
    public interface IReloadableConfiguration
    {
        void ReloadConfiguration(JObject newConfig);
    }
}
