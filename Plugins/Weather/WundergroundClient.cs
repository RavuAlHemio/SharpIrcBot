using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SharpIrcBot;
using Weather.Wunderground;

namespace Weather
{
    public class WundergroundClient
    {
        private HttpClientHandler _clientHandler;
        private HttpClient _client;

        public TimeSpan Timeout
        {
            get { return _client.Timeout; }
            set { _client.Timeout = value; }
        }

        public WundergroundClient()
        {
            _clientHandler = new HttpClientHandler();
            _clientHandler.CookieContainer = new CookieContainer();
            _client = new HttpClient(_clientHandler);
        }

        public WundergroundResponse GetWeatherForLocation(string apiKey, string location)
        {
            string escapedApiKey = Uri.EscapeUriString(apiKey);
            string escapedLocation = Uri.EscapeUriString(location);

            Task<string> jsonTask = _client.GetStringAsync($"http://api.wunderground.com/api/{escapedApiKey}/geolookup/conditions/forecast/q/{escapedLocation}.json");
            jsonTask.Wait();
            string json = jsonTask.Result;

            var response = new WundergroundResponse();
            JsonConvert.PopulateObject(json, response);
            return response;
        }
    }
}
