using System;
using System.Linq;
using System.Reflection;

namespace SharpIrcBot.Plugins.UnoBot.RuntimeTweaking
{
    public static class ConfigTweaking
    {
        public static void TweakConfig<T>(T config, string propertyName, string valueString)
        {
            // find the property
            Type configType = typeof(T);
            PropertyInfo property = configType.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            if (property == null)
            {
                throw new ArgumentException(string.Format("property {0} not found", propertyName), "propertyName");
            }

            // is it tweakable at runtime?
            var notTweakableAttributes = property.GetCustomAttributes<NotTweakableAtRuntimeAttribute>();
            if (notTweakableAttributes != null && notTweakableAttributes.Any())
            {
                throw new ArgumentException(string.Format("property {0} is not tweakable at runtime", propertyName), "propertyName");
            }

            // find its type and set it
            if (property.PropertyType.IsAssignableFrom(typeof(int)))
            {
                int val;
                try
                {
                    val = int.Parse(valueString);
                }
                catch (FormatException e)
                {
                    throw new ArgumentException(string.Format("property value {0} cannot be parsed as int", valueString), "valueString", e);
                }
                property.SetValue(config, val);
            }
            else if (property.PropertyType.IsAssignableFrom(typeof(long)))
            {
                long val;
                try
                {
                    val = long.Parse(valueString);
                }
                catch (FormatException e)
                {
                    throw new ArgumentException(string.Format("property value {0} cannot be parsed as long", valueString), "valueString", e);
                }
                property.SetValue(config, val);
            }
            else if (property.PropertyType.IsAssignableFrom(typeof(bool)))
            {
                bool val;
                try
                {
                    val = bool.Parse(valueString);
                }
                catch (FormatException e)
                {
                    throw new ArgumentException(string.Format("property value {0} cannot be parsed as bool", valueString), "valueString", e);
                }
                property.SetValue(config, val);
            }
            else if (property.PropertyType.IsAssignableFrom(typeof(string)))
            {
                property.SetValue(config, valueString);
            }
            else
            {
                throw new ArgumentException(string.Format("property {0} cannot be set at runtime", propertyName), "propertyName");
            }
        }
    }
}
