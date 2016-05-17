using JetBrains.Annotations;

namespace SharpIrcBot
{
    public interface IDatabaseModuleConfig
    {
        [NotNull]
        string DatabaseProvider { get; }
        [NotNull]
        string DatabaseConnectionString { get; }
    }
}

