using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using JetBrains.Annotations;

namespace SharpIrcBot.Util
{
    public static class StringUtil
    {
        [NotNull]
        public static readonly Encoding Utf8NoBom = new UTF8Encoding(false, true);

        /// <summary>
        /// Converts a string into Unicode code points, handling surrogate pairs gracefully.
        /// </summary>
        /// <returns>The code points.</returns>
        /// <param name="str">The string to convert to code points.</param>
        [NotNull]
        public static IEnumerable<string> StringToCodePointStrings([NotNull] string str)
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

            if (awaitingTrailSurrogate)
            {
                // lead surrogate at the end of the string
                // return it
                yield return new string(precedingLeadSurrogate, 1);
            }
        }

        [NotNull]
        public static string RemoveControlCharactersAndTrim([NotNull] string text)
        {
            var ret = new StringBuilder();
            foreach (var cp in StringToCodePointStrings(text))
            {
                var cat = CharUnicodeInfo.GetUnicodeCategory(cp, 0);
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
        [NotNull]
        public static string LiteralString([NotNull] string str)
        {
            var ret = new StringBuilder("\"");
            foreach (string pStr in StringToCodePointStrings(str))
            {
                int p = Char.ConvertToUtf32(pStr, 0);
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

        [Pure, CanBeNull]
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
        [CanBeNull]
        public static string UnescapeString([NotNull] string str)
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
            if (escapeState != 0)
            {
                // trailing backslash
                return null;
            }
            return ret.ToString();
        }

        /// <summary>
        /// Attempts to parse an integer in the invariant culture <see cref="CultureInfo.InvariantCulture"/>.
        /// </summary>
        /// <param name="text">The text to attempt to parse as an integer.</param>
        /// <param name="numberStyles">Number styles to allow.</param>
        /// <returns>The parsed integer, or <c>null</c> if parsing failed.</returns>
        [CanBeNull]
        public static int? MaybeParseInt([NotNull] string text, NumberStyles numberStyles = NumberStyles.None)
        {
            int ret;
            if (int.TryParse(text, numberStyles, CultureInfo.InvariantCulture, out ret))
            {
                return ret;
            }
            return null;
        }

        /// <summary>
        /// Attempts to parse a long integer in the invariant culture <see cref="CultureInfo.InvariantCulture"/>.
        /// </summary>
        /// <param name="text">The text to attempt to parse as an integer.</param>
        /// <param name="numberStyles">Number styles to allow.</param>
        /// <returns>The parsed integer, or <c>null</c> if parsing failed.</returns>
        [CanBeNull]
        public static long? MaybeParseLong([NotNull] string text, NumberStyles numberStyles = NumberStyles.None)
        {
            long ret;
            if (long.TryParse(text, numberStyles, CultureInfo.InvariantCulture, out ret))
            {
                return ret;
            }
            return null;
        }

        public static int? MaybeIntFromMatchGroup(Group grp)
        {
            return grp.Success
                ? int.Parse(grp.Value)
                : (int?)null;
        }

        /// <summary>
        /// Calculates the Levenshtein distance between the two given strings.
        /// </summary>
        public static long LevenshteinDistance(string one, string other)
        {
            // https://en.wikipedia.org/wiki/Levenshtein_distance#Iterative_with_two_matrix_rows

            if (one.Length > other.Length)
            {
                return LevenshteinDistance(other, one);
            }

            var prevRow = new long[other.Length + 1];
            var curRow = new long[prevRow.Length];

            for (int i = 0; i < prevRow.Length; ++i)
            {
                prevRow[i] = i;
            }

            for (int i = 0; i < one.Length; ++i)
            {
                curRow[0] = i + 1;

                for (int j = 0; j < other.Length; ++j)
                {
                    long delCost = prevRow[j + 1] + 1;
                    long insCost = curRow[j] + 1;
                    long subCost = (one[i] == other[j])
                        ? prevRow[j]
                        : prevRow[j] + 1
                    ;
                    curRow[j + 1] = Math.Min(delCost, Math.Min(insCost, subCost));
                }

                (prevRow, curRow) = (curRow, prevRow);
            }

            return prevRow[prevRow.Length - 1];
        }
    }
}
