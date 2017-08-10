using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SharpIrcBot.Plugins.Libraries.GeoNames
{
    [JsonObject(MemberSerialization.OptOut)]
    public class GeoNamesClient
    {
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

                var geoSearchUri = new Uri($"http://api.geonames.org/searchJSON?maxRows=1&q={WebUtility.UrlEncode(location)}&username={WebUtility.UrlEncode(Config.Username)}");
                string geoSearchResponse = await client.GetStringAsync(geoSearchUri);

                using (var sr = new StringReader(geoSearchResponse))
                {
                    JsonSerializer.Create().Populate(sr, geoSearchResult);
                }
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

                using (var sr = new StringReader(timeZoneSearchResponse))
                {
                    JsonSerializer.Create().Populate(sr, geoTimeZoneResult);
                }
            }
            return geoTimeZoneResult;
        }

        private static string Inv(FormattableString formattable)
        {
            return formattable.ToString(CultureInfo.InvariantCulture);
        }
    }
}
