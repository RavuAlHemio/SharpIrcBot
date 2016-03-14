using System;
using System.Net;
using Newtonsoft.Json;
using Weather.Wunderground;

namespace Weather
{
    public class WundergroundClient
    {
        private WebClient _client;

        public WundergroundClient()
        {
            _client = new WebClient();
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
