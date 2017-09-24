using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SharpIrcBot.Plugins.Libraries.GeoNames
{
    [JsonObject(MemberSerialization.OptOut)]
    public class GeoNamesClient
    {
        public static readonly Regex PostCodeRegex = new Regex("^(?<country>[A-Z]{1,3})-(?<postcode>[A-Z0-9- ]+)$", RegexOptions.Compiled);

        public GeoNamesConfig Config { get; set; }

        public GeoNamesClient(GeoNamesConfig config)
        {
            Config = config;
        }

        public async Task<GeoSearchResult> SearchForLocation(string location)
        {
            var geoSearchResult = new GeoSearchResult();
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.ConnectionClose = true;

                var geoSearchUri = new Uri(
                    $"http://api.geonames.org/searchJSON?maxRows=1&q={WebUtility.UrlEncode(location)}&username={WebUtility.UrlEncode(Config.Username)}"
                );
                string geoSearchResponse = await client.GetStringAsync(geoSearchUri);

                PopulateWithJsonString(geoSearchResult, geoSearchResponse);
            }
            return geoSearchResult;
        }

        public async Task<GeoTimeZoneResult> GetTimezone(decimal latitude, decimal longitude)
        {
            var geoTimeZoneResult = new GeoTimeZoneResult();
            using (var client = new HttpClient())
            {
                var timeZoneSearchUri = new Uri(Inv($"http://api.geonames.org/timezoneJSON?lat={latitude}&lng={longitude}&username={WebUtility.UrlEncode(Config.Username)}"));
                string timeZoneSearchResponse = await client.GetStringAsync(timeZoneSearchUri);

                PopulateWithJsonString(geoTimeZoneResult, timeZoneSearchResponse);
            }
            return geoTimeZoneResult;
        }

        public async Task<PostCodeSearchResult> SearchForPostCode(string postCodeString)
        {
            Match postCodeMatch = PostCodeRegex.Match(postCodeString);
            if (!postCodeMatch.Success)
            {
                return null;
            }

            string country = postCodeMatch.Groups["country"].Value;
            string countryAlpha2 = CountryCodeToAlpha2(country);
            if (countryAlpha2 == null)
            {
                return null;
            }

            string postCode = postCodeMatch.Groups["postcode"].Value;

            var postCodeSearchResult = new PostCodeSearchResult();
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.ConnectionClose = true;

                var postCodeSearchUri = new Uri(
                    $"http://api.geonames.org/postalCodeSearchJSON?postalcode={WebUtility.UrlEncode(postCode)}&country={WebUtility.UrlEncode(countryAlpha2)}&maxRows=1&username={WebUtility.UrlEncode(Config.Username)}"
                );
                string postCodeSearchResponse = await client.GetStringAsync(postCodeSearchUri);

                PopulateWithJsonString(postCodeSearchResult, postCodeSearchResponse);
            }
            return postCodeSearchResult;
        }

        public async Task<GeoName> GetFirstGeoName(string query)
        {
            PostCodeSearchResult postCodes = await SearchForPostCode(query);
            if (postCodes != null)
            {
                return postCodes.PostCodeEntries.Any()
                    ? postCodes.PostCodeEntries[0]
                    : null;
            }

            GeoSearchResult result = await SearchForLocation(query);
            if (result == null)
            {
                return null;
            }

            return result.GeoNames.Any()
                ? result.GeoNames[0]
                : null;
        }

        private static string Inv(FormattableString formattable)
        {
            return formattable.ToString(CultureInfo.InvariantCulture);
        }

        protected static void PopulateWithJsonString(object obj, string jsonString)
        {
            using (var sr = new StringReader(jsonString))
            {
                JsonSerializer.Create().Populate(sr, obj);
            }
        }

        protected static string CountryCodeToAlpha2(string countryCode)
        {
            if (CountryCodeMapping.Alpha2.Contains(countryCode))
            {
                return countryCode;
            }

            string ret;
            if (CountryCodeMapping.Alpha3ToAlpha2.TryGetValue(countryCode, out ret))
            {
                return ret;
            }
            if (CountryCodeMapping.LicencePlatesToAlpha2.TryGetValue(countryCode, out ret))
            {
                return ret;
            }

            return null;
        }
    }
}
