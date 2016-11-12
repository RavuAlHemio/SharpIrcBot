using JetBrains.Annotations;

namespace SharpIrcBot
{
    public interface IDatabaseModuleConfig
    {
        [NotNull]
        string DatabaseProviderAssembly { get; }

        [NotNull]
        string DatabaseConfiguratorClass { get; }

        [NotNull]
        string DatabaseConfiguratorMethod { get; }

        [NotNull]
        string DatabaseConnectionString { get; }
    }
}

