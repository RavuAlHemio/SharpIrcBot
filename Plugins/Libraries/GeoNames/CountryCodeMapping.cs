using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using SharpIrcBot.Util;

namespace SharpIrcBot.Plugins.Libraries.GeoNames
{
    public static class CountryCodeMapping
    {
        /// <summary>
        /// Set of valid ISO 3166-1 alpha-2 country codes.
        /// </summary>
        public static ImmutableHashSet<string> Alpha2 { get; set; }

        /// <summary>
        /// Dictionary mapping international licence plate country codes (as defined by the United Nations Economic
        /// Commission for Europe) to ISO 3166-1 alpha-2 country codes.
        /// </summary>
        public static ImmutableDictionary<string, string> LicencePlatesToAlpha2 { get; private set; }

        /// <summary>
        /// Dictionary mapping ISO 3166-1 alpha-3 country codes to ISO 3166-1 alpha-2 country codes.
        /// </summary>
        public static ImmutableDictionary<string, string> Alpha3ToAlpha2 { get; private set; }

        static CountryCodeMapping()
        {
            var countryCodes = new List<CountryCode>();

            string filePath = Path.Combine(SharpIrcBotUtil.AppDirectory, "CountryCodes.json");
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
            using (var reader = new StreamReader(stream, StringUtil.Utf8NoBom, detectEncodingFromByteOrderMarks: true))
            {
                JsonSerializer.Create().Populate(reader, countryCodes);
            }

            Alpha2 = countryCodes
                .Where(cc => cc.Alpha2Code != null)
                .Select(cc => cc.Alpha2Code)
                .ToImmutableHashSet();

            LicencePlatesToAlpha2 = countryCodes
                .Where(cc => cc.LicensePlateCode != null && cc.Alpha2Code != null)
                .Select(cc => KVP(cc.LicensePlateCode, cc.Alpha2Code))
                .ToImmutableDictionary();

            Alpha3ToAlpha2 = countryCodes
                .Where(cc => cc.Alpha3Code != null && cc.Alpha2Code != null)
                .Select(cc => KVP(cc.Alpha3Code, cc.Alpha2Code))
                .ToImmutableDictionary();
        }

        private static KeyValuePair<TKey, TValue> KVP<TKey, TValue>(TKey key, TValue value)
            => new KeyValuePair<TKey, TValue>(key, value);

        [JsonObject(MemberSerialization.OptIn)]
        class CountryCode
        {
            [JsonProperty("plate")] public string LicensePlateCode { get; set; }
            [JsonProperty("alpha2")] public string Alpha2Code { get; set; }
            [JsonProperty("alpha3")] public string Alpha3Code { get; set; }
        }
    }
}
