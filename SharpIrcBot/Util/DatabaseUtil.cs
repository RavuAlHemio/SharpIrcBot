using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace SharpIrcBot.Util
{
    public static class DatabaseUtil
    {
        public static TBuilder IfNpgsql<TBuilder>(
            this TBuilder builder, DatabaseFacade database, Action<TBuilder> confAction
        )
        {
            if (database.IsNpgsql())
            {
                confAction.Invoke(builder);
            }
            return builder;
        }

        public static DbContextOptions<T> GetContextOptions<T>(IDatabaseModuleConfig config)
            where T : DbContext
        {
            var builder = new DbContextOptionsBuilder<T>();

            // SomeMethod(DbContextOptionsBuilder builder, string connectionString [, optionally more parameters with default values])
            Assembly ass = Assembly.Load(new AssemblyName(config.DatabaseProviderAssembly));
            Type configuratorType = ass.GetType(config.DatabaseConfiguratorClass);

            MethodInfo configuratorMethod = null;
            ParameterInfo[] configuratorParameters = null;
            foreach (MethodInfo candidateMethod in configuratorType.GetMethods())
            {
                if (candidateMethod.Name != config.DatabaseConfiguratorMethod)
                {
                    continue;
                }

                if (!candidateMethod.IsPublic || !candidateMethod.IsStatic)
                {
                    continue;
                }

                configuratorParameters = candidateMethod.GetParameters();
                if (configuratorParameters.Length < 2)
                {
                    continue;
                }

                if (!configuratorParameters[0].ParameterType.IsAssignableFrom(builder.GetType()))
                {
                    continue;
                }

                if (configuratorParameters[1].ParameterType != typeof(string))
                {
                    continue;
                }

                if (!configuratorParameters.Skip(2).All(param => param.HasDefaultValue))
                {
                    continue;
                }

                configuratorMethod = candidateMethod;
                break;
            }

            if (configuratorMethod == null)
            {
                throw new KeyNotFoundException($"no viable configurator method named {config.DatabaseConfiguratorMethod} found in {ass.FullName}");
            }

            var parameters = new object[configuratorParameters.Length];
            parameters[0] = builder;
            parameters[1] = config.DatabaseConnectionString;
            for (int i = 2; i < parameters.Length; ++i)
            {
                parameters[i] = configuratorParameters[i].DefaultValue;
            }

            configuratorMethod.Invoke(null, parameters);

            return builder.Options;
        }
    }
}
