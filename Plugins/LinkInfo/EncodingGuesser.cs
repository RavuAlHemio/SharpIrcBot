using System;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;

namespace SharpIrcBot.Plugins.LinkInfo
{
    static class EncodingGuesser
    {
        public static readonly Regex MetaCharsetRegex = new Regex("<meta\\s+.*?charset\\s*=\\s*[\"]?\\s*([A-Za-z0-9_-]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

        static string TryEncoding(byte[] data, string encName)
        {
            Encoding enc;
            try
            {
                enc = Encoding.GetEncoding(encName, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
            }
            catch (ArgumentException)
            {
                return null;
            }

            try
            {
                return enc.GetString(data);
            }
            catch (DecoderFallbackException)
            {
                return null;
            }
            catch (NullReferenceException)
            {
                // HACK
                // System.Text.Encoding.CodePages <= 4.3.0 has a bug that throws
                // NullReferenceException when attempting to decode an invalid
                // codepoint with ExceptionFallback. Work around this.
                return null;
            }
        }

        public static string GuessEncodingAndDecode(byte[] data, MediaTypeHeaderValue contentTypeHeader)
        {
            string ret;

            // try scanning the header
            if (contentTypeHeader?.CharSet != null)
            {
                ret = TryEncoding(data, contentTypeHeader.CharSet.Trim('\'', '"'));
                if (ret != null)
                {
                    return ret;
                }
            }

            // try finding a <meta charset="..." /> tag
            var oneTwoFiveTwo = Encoding.GetEncoding("windows-1252").GetString(data);
            var metaMatch = MetaCharsetRegex.Match(oneTwoFiveTwo);
            if (metaMatch.Success)
            {
                var encodingName = metaMatch.Groups[1].Value.ToLowerInvariant();
                if (encodingName == "unicode" || encodingName == "utf-16")
                {
                    encodingName = "utf-8";
                }

                ret = TryEncoding(data, encodingName);
                if (ret != null)
                {
                    return ret;
                }
            }

            // try UTF-8
            ret = TryEncoding(data, "utf-8");
            if (ret != null)
            {
                return ret;
            }

            // try Windows-1252
            ret = TryEncoding(data, "windows-1252");
            if (ret != null)
            {
                return ret;
            }

            // *sigh*
            return new string(data.Select(b => (char)b).ToArray());
        }
    }
}
