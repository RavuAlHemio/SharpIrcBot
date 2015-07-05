﻿using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Core;
using log4net.Layout;
using log4net.Repository.Hierarchy;
using Newtonsoft.Json.Linq;

namespace SharpIrcBot
{
    public static class SharpIrcBotUtil
    {
        public const string DefaultLogFormat = "%date{yyyy-MM-dd HH:mm:ss} [%15.15thread] %-5level %30.30logger - %message%newline";
        public static readonly Encoding Utf8NoBom = new UTF8Encoding(false, true);
        public static readonly ISet<char> UrlSafeChars = new HashSet<char>("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_.");

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

        [Pure]
        private static byte? FromHexDigit(char c)
        {
            if (c >= '0' && c <= '9')
            {
                return (byte) (c - '0');
            }
            else if (c >= 'a' && c <= 'z')
            {
                return (byte) (c - 'a' + 10);
            }
            else if (c >= 'A' && c <= 'Z')
            {
                return (byte)(c - 'A' + 10);
            }
            return null;
        }

        /// <summary>
        /// Unescapes the C# string literal.
        /// </summary>
        /// <param name="str">The string literal to unescape.</param>
        /// <returns>The unescaped string.</returns>
        public static string UnescapeString(string str)
        {
            var ret = new StringBuilder(str.Length);
            // 0: normal, 1: after backslash, 2-9: digits of \U, 6-9: digits of \u, 8-9: digits of \x
            int escapeState = 0;
            long currentValue = 0;
            byte? digit;
            foreach (char c in str)
            {
                switch (escapeState)
                {
                    case 0:
                        // not escaping
                        if (c == '\\')
                        {
                            // start escaping
                            escapeState = 1;
                            continue;
                        }
                        break;
                    case 1:
                        // right after backslash
                        switch (c)
                        {
                            // standard cases
                            case '0':
                                ret.Append('\0');
                                escapeState = 0;
                                continue;
                            case '\\':
                                ret.Append('\\');
                                escapeState = 0;
                                continue;
                            case '"':
                                ret.Append('"');
                                escapeState = 0;
                                continue;
                            case '\'':
                                ret.Append('\'');
                                escapeState = 0;
                                continue;
                            case 'a':
                                ret.Append('\a');
                                escapeState = 0;
                                continue;
                            case 'b':
                                ret.Append('\b');
                                escapeState = 0;
                                continue;
                            case 'f':
                                ret.Append('\f');
                                escapeState = 0;
                                continue;
                            case 'n':
                                ret.Append('\n');
                                escapeState = 0;
                                continue;
                            case 'r':
                                ret.Append('\r');
                                escapeState = 0;
                                continue;
                            case 't':
                                ret.Append('\t');
                                escapeState = 0;
                                continue;
                            case 'v':
                                ret.Append('\v');
                                escapeState = 0;
                                continue;
                            case 'x':
                                // NOTE: \x## only allows two digits
                                escapeState = 8;
                                currentValue = 0;
                                continue;
                            case 'u':
                                escapeState = 6;
                                currentValue = 0;
                                continue;
                            case 'U':
                                escapeState = 2;
                                currentValue = 0;
                                continue;
                            default:
                                // unexpected escape!
                                return null;
                        }
                    case 2:
                        // 1st digit of \U
                        digit = FromHexDigit(c);
                        if (!digit.HasValue)
                        {
                            return null;
                        }
                        currentValue |= ((long)digit.Value << 28);
                        ++escapeState;
                        continue;
                    case 3:
                        // 2nd digit of \U
                        digit = FromHexDigit(c);
                        if (!digit.HasValue)
                        {
                            return null;
                        }
                        currentValue |= ((long)digit.Value << 24);
                        ++escapeState;
                        continue;
                    case 4:
                        // 3rd digit of \U
                        digit = FromHexDigit(c);
                        if (!digit.HasValue)
                        {
                            return null;
                        }
                        currentValue |= ((long)digit.Value << 20);
                        ++escapeState;
                        continue;
                    case 5:
                        // 4th digit of \U
                        digit = FromHexDigit(c);
                        if (!digit.HasValue)
                        {
                            return null;
                        }
                        currentValue |= ((long)digit.Value << 16);
                        ++escapeState;
                        continue;
                    case 6:
                        // 5th digit of \U or 1st digit of \u
                        digit = FromHexDigit(c);
                        if (!digit.HasValue)
                        {
                            return null;
                        }
                        currentValue |= ((long)digit.Value << 12);
                        ++escapeState;
                        continue;
                    case 7:
                        // 6th digit of \U or 2nd digit of \u
                        digit = FromHexDigit(c);
                        if (!digit.HasValue)
                        {
                            return null;
                        }
                        currentValue |= ((long)digit.Value << 8);
                        ++escapeState;
                        continue;
                    case 8:
                        // 7th digit of \U or 3rd digit of \u or 1st digit of \x
                        digit = FromHexDigit(c);
                        if (!digit.HasValue)
                        {
                            return null;
                        }
                        currentValue |= ((long)digit.Value << 4);
                        ++escapeState;
                        continue;
                    case 9:
                        // 8th digit of \U or 4th digit of \u or 2nd digit of \x
                        digit = FromHexDigit(c);
                        if (!digit.HasValue)
                        {
                            return null;
                        }
                        currentValue |= digit.Value;

                        // append!
                        ret.Append(char.ConvertFromUtf32((int) currentValue));
                        currentValue = 0;
                        escapeState = 0;
                        continue;
                }

                // default (break and not continue): append
                ret.Append(c);
            }
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

        public static void SetupFileLogging(Level level = null)
        {
            var logConfFile = new FileInfo(Path.Combine(AppDirectory, "LogConf.xml"));
            if (logConfFile.Exists)
            {
                // use the XML configurator instead
                XmlConfigurator.Configure(logConfFile);
                return;
            }

            var hierarchy = (Hierarchy) LogManager.GetRepository();
            var rootLogger = hierarchy.Root;
            rootLogger.Level = level ?? Level.Debug;

            var patternLayout = new PatternLayout
            {
                ConversionPattern = DefaultLogFormat
            };
            patternLayout.ActivateOptions();

            var logAppender = new FileAppender
            {
                AppendToFile = true,
                Encoding = Utf8NoBom,
                File = Path.Combine(AppDirectory, "SharpIrcBot.log"),
                Layout = patternLayout
            };
            logAppender.ActivateOptions();

            rootLogger.AddAppender(logAppender);

            hierarchy.Configured = true;
        }

        public static void SetupConsoleLogging(Level level = null)
        {
            var hierarchy = (Hierarchy)LogManager.GetRepository();
            var rootLogger = hierarchy.Root;
            rootLogger.Level = level ?? Level.Debug;

            var patternLayout = new PatternLayout
            {
                ConversionPattern = DefaultLogFormat
            };
            patternLayout.ActivateOptions();

            var logAppender = new ManagedColoredConsoleAppender
            {
                Layout = patternLayout
            };
            logAppender.ActivateOptions();

            rootLogger.AddAppender(logAppender);

            hierarchy.Configured = true;
        }

        /// <summary>
        /// URL-encodes the string.
        /// </summary>
        /// <returns>The URL-encoded string.</returns>
        /// <param name="data">The string to URL-encode.</param>
        /// <param name="charset">The charset being used.</param>
        /// <param name="spaceAsPlus">If true, encodes spaces (U+0020) as pluses (U+002B).
        /// If false, encodes spaces as the hex escape "%20".</param>
        public static string UrlEncode(string data, Encoding charset, bool spaceAsPlus = false)
        {
            var ret = new StringBuilder();
            foreach (string ps in StringToCodePointStrings(data))
            {
                if (ps.Length == 1 && UrlSafeChars.Contains(ps[0]))
                {
                    // URL-safe character
                    ret.Append(ps[0]);
                }
                else if (spaceAsPlus && ps.Length == 1 && ps[0] == ' ')
                {
                    ret.Append('+');
                }
                else
                {
                    // character in the server's encoding?
                    try
                    {
                        // URL-encode
                        foreach (var b in charset.GetBytes(ps))
                        {
                            ret.AppendFormat("%{0:X2}", (int)b);
                        }
                    }
                    catch (EncoderFallbackException)
                    {
                        // unsupported natively by the encoding; perform a URL-encoded HTML escape
                        ret.AppendFormat("%26%23{0}%3B", Char.ConvertToUtf32(ps, 0));
                    }
                }
            }

            return ret.ToString();
        }
    }
}
