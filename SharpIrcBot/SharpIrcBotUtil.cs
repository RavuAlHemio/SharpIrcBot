using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using Newtonsoft.Json.Linq;

namespace SharpIrcBot
{
    public static class SharpIrcBotUtil
    {
        public static BotConfig LoadConfig()
        {
            return new BotConfig(JObject.Parse(File.ReadAllText(Path.Combine(AppDirectory, "Config.json"), Encoding.UTF8)));
        }

        public static string AppDirectory
        {
            get { return Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath); }
        }

        /// <summary>
        /// Converts a string into Unicode code points, handling surrogate pairs gracefully.
        /// </summary>
        /// <returns>The code points.</returns>
        /// <param name="str">The string to convert to code points.</param>
        public static IEnumerable<string> StringToCodePointStrings(string str)
        {
            char precedingLeadSurrogate = (char)0;
            bool awaitingTrailSurrogate = false;

            foreach (char c in str)
            {
                if (awaitingTrailSurrogate)
                {
                    if (c >= 0xDC00 && c <= 0xDFFF)
                    {
                        // SMP code point
                        yield return new string(new [] { precedingLeadSurrogate, c });
                    }
                    else
                    {
                        // lead surrogate without trail surrogate
                        // return both independently
                        yield return new string(precedingLeadSurrogate, 1);
                        yield return new string(c, 1);
                    }

                    awaitingTrailSurrogate = false;
                }
                else if (c >= 0xD800 && c <= 0xDBFF)
                {
                    precedingLeadSurrogate = c;
                    awaitingTrailSurrogate = true;
                }
                else
                {
                    yield return new string(c, 1);
                }
            }
        }

        public static string RemoveControlCharactersAndTrim(string text)
        {
            var ret = new StringBuilder();
            foreach (var cp in StringToCodePointStrings(text))
            {
                var cat = char.GetUnicodeCategory(cp, 0);
                if (cat == UnicodeCategory.Control || cat == UnicodeCategory.Format)
                {
                    continue;
                }
                ret.Append(cp);
            }
            return ret.ToString().Trim();
        }

        /// <summary>
        /// Returns the string as a C# string literal.
        /// </summary>
        /// <returns>The C# string literal.</returns>
        /// <param name="str">The string to return as a C# literal.</param>
        public static string LiteralString(string str)
        {
            var ret = new StringBuilder("\"");
            foreach (var pStr in StringToCodePointStrings(str))
            {
                var p = Char.ConvertToUtf32(pStr, 0);
                switch (p)
                {
                    case '\0':
                        ret.Append("\\0");
                        break;
                    case '\\':
                        ret.Append("\\\\");
                        break;
                    case '"':
                        ret.Append("\\\"");
                        break;
                    case '\a':
                        ret.Append("\\a");
                        break;
                    case '\b':
                        ret.Append("\\b");
                        break;
                    case '\f':
                        ret.Append("\\f");
                        break;
                    case '\n':
                        ret.Append("\\n");
                        break;
                    case '\r':
                        ret.Append("\\r");
                        break;
                    case '\t':
                        ret.Append("\\t");
                        break;
                    case '\v':
                        ret.Append("\\v");
                        break;
                    default:
                        if (p < ' ' || (p > '~' && p <= 0xFFFF))
                        {
                            ret.AppendFormat("\\u{0:X4}", p);
                        }
                        else if (p > 0xFFFF)
                        {
                            ret.AppendFormat("\\U{0:X8}", p);
                        }
                        else
                        {
                            ret.Append((char)p);
                        }
                        break;
                }
            }
            ret.Append('"');
            return ret.ToString();
        }

        public static DateTime ToUniversalTimeForDatabase(this DateTime dt)
        {
            return DateTime.SpecifyKind(dt.ToUniversalTime(), DateTimeKind.Unspecified);
        }

        public static DateTime ToLocalTimeFromDatabase(this DateTime dt)
        {
            return DateTime.SpecifyKind(dt, DateTimeKind.Utc).ToLocalTime();
        }

        public static DbConnection GetDatabaseConnection(IDatabaseModuleConfig config)
        {
            var conn = DbProviderFactories.GetFactory(config.DatabaseProvider).CreateConnection();
            conn.ConnectionString = config.DatabaseConnectionString;
            return conn;
        }
    }
}
