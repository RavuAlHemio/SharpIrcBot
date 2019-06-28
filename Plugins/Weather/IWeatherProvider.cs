namespace SharpIrcBot.Plugins.Weather
{
    public interface IWeatherProvider
    {
        string GetWeatherDescriptionForSpecial(string specialString);
        string GetWeatherDescriptionForCoordinates(decimal latitudeDegNorth, decimal longitudeDegEast);
    }
}
