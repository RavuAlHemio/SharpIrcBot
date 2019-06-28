using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using SharpIrcBot.Commands;
using SharpIrcBot.Config;
using SharpIrcBot.Events.Irc;
using SharpIrcBot.Plugins.Libraries.GeoNames;
using SharpIrcBot.Util;

namespace SharpIrcBot.Plugins.Weather
{
    public class WeatherPlugin : IPlugin, IReloadableConfiguration
    {
        protected static readonly Regex LatLonRegex = new Regex(
            "^" +
            "\\s*" +
            "(?<Latitude>[0-9]+(?:[.][0-9]*)?)" +
            "," +
            "\\s*" +
            "(?<Longitude>[0-9]+(?:[.][0-9]*)?)" +
            "\\s*" +
            "$"
        );

        private static readonly LoggerWrapper Logger = LoggerWrapper.Create<WeatherPlugin>();

        protected IConnectionManager ConnectionManager { get; }
        protected WeatherConfig Config { get; set; }
        protected List<IWeatherProvider> WeatherProviders { get; set; }

        public WeatherPlugin(IConnectionManager connMgr, JObject config)
        {
            ConnectionManager = connMgr;
            Config = new WeatherConfig(config);
            WeatherProviders = new List<IWeatherProvider>();

            ConnectionManager.CommandManager.RegisterChannelMessageCommandHandler(
                new Command(
                    CommandUtil.MakeNames("weather", "lweather"),
                    CommandUtil.NoOptions,
                    CommandUtil.MakeArguments(
                        RestTaker.Instance // location
                    ),
                    CommandUtil.MakeTags("fun"),
                    forbiddenFlags: MessageFlags.UserBanned
                ),
                HandleWeatherCommand
            );

            ReloadProviders();
        }

        public virtual void ReloadConfiguration(JObject newConfig)
        {
            Config = new WeatherConfig(newConfig);
            PostConfigReload();
        }

        protected virtual void PostConfigReload()
        {
            ReloadProviders();
        }

        protected virtual void ReloadProviders()
        {
            WeatherProviders.Clear();

            foreach (PluginConfig config in Config.WeatherProviders.Where(wp => wp.Enabled))
            {
                Assembly ass = Assembly.Load(new AssemblyName(config.Assembly));
                Type type = ass.GetType(config.Class);
                if (!typeof(IWeatherProvider).GetTypeInfo().IsAssignableFrom(type))
                {
                    throw new ArgumentException($"class {type.FullName} is not a weather provider");
                }
                ConstructorInfo ctor = type.GetTypeInfo().GetConstructor(new [] {typeof(JObject)});
                var pluginObject = (IWeatherProvider)ctor.Invoke(new object[] {config.Config});
                WeatherProviders.Add(pluginObject);
            }
        }

        protected virtual void OutputWeather(string locName, string channel, string nick, string weather, bool showLocName = true)
        {
            ConnectionManager.SendChannelMessage(
                channel,
                (showLocName && locName != null)
                    ? $"{nick}: {locName}: {weather}"
                    : $"{nick}: {weather}"
            );
        }

        protected virtual void GetWeatherForLocation(string location, string channel, string nick, bool lookupAlias = true, bool showLocName = true)
        {
            if (lookupAlias)
            {
                string aliasedLocation;
                if (Config.LocationAliases.TryGetValue(location, out aliasedLocation))
                {
                    location = aliasedLocation;
                }
            }

            // try specials first
            var providersWeathers = new List<(IWeatherProvider Provider, string WeatherString)>();
            foreach (IWeatherProvider provider in WeatherProviders)
            {
                // returns null if unsuccessful
                string weatherDescr = provider.GetWeatherDescriptionForSpecial(location);
                providersWeathers.Add((provider, weatherDescr));
            }

            if (providersWeathers.Any(pw => pw.WeatherString != null))
            {
                // some special; skip the geocoding
                foreach (var (provider, weather) in providersWeathers)
                {
                    if (weather == null)
                    {
                        continue;
                    }

                    OutputWeather(null, channel, nick, weather, showLocName);
                }
                return;
            }

            // geocode
            var geoClient = new GeoNamesClient(Config.GeoNames);

            Match latLonMatch = LatLonRegex.Match(location);
            decimal latitude, longitude;
            string locName = null;
            if (latLonMatch.Success)
            {
                latitude = ParseDecimalInv(latLonMatch.Groups["Latitude"].Value);
                longitude = ParseDecimalInv(latLonMatch.Groups["Longitude"].Value);
                if (showLocName)
                {
                    locName = geoClient.GetFirstReverseGeo(latitude, longitude).Result;
                }
            }
            else
            {
                // find the location using GeoNames (Wunderground's geocoding is really bad)
                GeoName loc = geoClient.GetFirstGeoName(location).Result;
                if (loc == null)
                {
                    ConnectionManager.SendChannelMessage(channel, $"{nick}: GeoNames cannot find that location!");
                    return;
                }
                latitude = loc.Latitude;
                longitude = loc.Longitude;
                locName = loc.NameAndCountryName;
            }

            foreach (var (provider, weather) in providersWeathers)
            {
                string finalWeather = weather;
                if (finalWeather == null)
                {
                    finalWeather = provider.GetWeatherDescriptionForCoordinates(latitude, longitude);
                }
                OutputWeather(locName, channel, nick, finalWeather, showLocName);
            }
        }

        protected virtual void HandleWeatherCommand(CommandMatch cmd, IChannelMessageEventArgs msg)
        {
            string location = ((string)cmd.Arguments[0]).Trim();
            if (location.Length == 0)
            {
                location = Config.DefaultLocation;
            }

            GetWeatherForLocation(location, msg.Channel, msg.SenderNickname, showLocName: cmd.CommandName == "weather");
        }

        protected virtual string FormatTimeSpan(TimeSpan span)
        {
            return FormatTimeSpanImpl(span);
        }

        public static string FormatTimeSpanImpl(TimeSpan span)
        {
            bool ago = false;

            if (span.Ticks < 0)
            {
                span = span.Negate();
                ago = true;
            }

            if (span.TotalSeconds < 1.0)
            {
                return "now";
            }

            var oTemporaOMores = new List<Tuple<long, string>>
            {
                TT(span.Days, "day", "days"),
                TT(span.Hours, "hour", "hours"),
                TT(span.Minutes, "minute", "minutes"),
                TT(span.Seconds, "second", "seconds")
            };

            // remove the empty large units
            while (oTemporaOMores.Count > 0 && oTemporaOMores[0].Item1 == 0)
            {
                oTemporaOMores.RemoveAt(0);
            }

            // show two consecutive units at most
            if (oTemporaOMores.Count > 2)
            {
                oTemporaOMores.RemoveRange(2, oTemporaOMores.Count - 2);
            }

            // delete the second unit if it is zero
            if (oTemporaOMores.Count > 1 && oTemporaOMores[0].Item1 == 0)
            {
                oTemporaOMores.RemoveAt(1);
            }

            // fun!
            string joint = oTemporaOMores.Select(t => t.Item2).StringJoin(" ");
            return ago
                ? (joint + " ago")
                : ("in " + joint)
            ;
        }

        // "time tuple"
        private static Tuple<long, string> TT(long time, string singular, string plural)
            => Tuple.Create(time, string.Format("{0} {1}", time, (time == 1) ? singular : plural));

        private static string Inv(FormattableString formattable)
        {
            return formattable.ToString(CultureInfo.InvariantCulture);
        }

        private static decimal ParseDecimalInv(string decimalStr)
        {
            return decimal.Parse(
                decimalStr,
                NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign,
                CultureInfo.InvariantCulture
            );
        }
    }
}
