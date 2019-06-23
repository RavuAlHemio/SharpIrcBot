namespace SharpIrcBot.Plugins.Weather
{
    public interface IWeatherProvider
    {
        string GetWeatherDescriptionForCoordinates(decimal latitudeDegNorth, decimal longitudeDegEast);
    }
}
