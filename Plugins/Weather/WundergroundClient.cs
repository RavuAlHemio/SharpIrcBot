using System;
using Newtonsoft.Json;
using SharpIrcBot;
using Weather.Wunderground;

namespace Weather
{
    public class WundergroundClient
    {
        private CookieWebClient _client;

        public TimeSpan Timeout
        {
            get { return _client.Timeout; }
            set { _client.Timeout = value; }
        }

        public WundergroundClient()
        {
            _client = new CookieWebClient();
        }

        public WundergroundResponse GetWeatherForLocation(string apiKey, string location)
        {
            var escapedApiKey = Uri.EscapeUriString(apiKey);
            var escapedLocation = Uri.EscapeUriString(location);
            var json = _client.DownloadString($"http://api.wunderground.com/api/{escapedApiKey}/geolookup/conditions/forecast/q/{escapedLocation}.json");

            var response = new WundergroundResponse();
            JsonConvert.PopulateObject(json, response);
            return response;
        }
    }
}
