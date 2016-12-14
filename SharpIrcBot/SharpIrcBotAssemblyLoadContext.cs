#if NETCORE
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;

namespace SharpIrcBot
{
    public class SharpIrcBotAssemblyLoadContext : AssemblyLoadContext
    {
        private static SharpIrcBotAssemblyLoadContext _instance = null;

        protected Dictionary<string, Assembly> LoadedAssemblies { get; }

        public static SharpIrcBotAssemblyLoadContext Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new SharpIrcBotAssemblyLoadContext();
                }
                return _instance;
            }
        }

        internal SharpIrcBotAssemblyLoadContext()
        {
            LoadedAssemblies = new Dictionary<string, Assembly>();
        }

        protected override Assembly Load(AssemblyName name)
        {
            Assembly ret;
            if (LoadedAssemblies.TryGetValue(name.Name, out ret))
            {
                return ret;
            }

            ret = base.LoadFromAssemblyPath(Path.Combine(SharpIrcBotUtil.AppDirectory, name.Name + ".dll"));
            LoadedAssemblies[name.Name] = ret;
            return ret;
        }
    }
}
#endif
