using System;

namespace SharpIrcBot
{
    public interface IDatabaseModuleConfig
    {
        string DatabaseProvider { get; }
        string DatabaseConnectionString { get; }
    }
}

