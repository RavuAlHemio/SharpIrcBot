#if NETCORE
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Extensions.Logging;

namespace SharpIrcBot
{
    public class SharpIrcBotAssemblyLoadContext : AssemblyLoadContext
    {
        private static readonly ILogger Logger = SharpIrcBotUtil.LoggerFactory.CreateLogger<SharpIrcBotAssemblyLoadContext>();

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

            try
            {
                ret = base.LoadFromAssemblyPath(Path.Combine(SharpIrcBotUtil.AppDirectory, name.Name + ".dll"));
            }
            catch (Exception exc)
            {
                Logger.LogError("error loading assembly {AssemblyName}: {Exception}", name.Name, exc);
                throw;
            }

            LoadedAssemblies[name.Name] = ret;
            return ret;
        }
    }
}
#endif
