using Weather;
using Xunit;

namespace RegexTests
{
    public class WeatherTests
    {
        private static void TestWeatherRegexValid(string testString, bool isLucky, string location)
        {
            var match = WeatherPlugin.WeatherRegex.Match(testString);
            Assert.True(match.Success);
            Assert.Equal(isLucky, match.Groups["lucky"].Success);
            if (location == null)
            {
                Assert.False(match.Groups["location"].Success);
            }
            else
            {
                Assert.True(match.Groups["location"].Success);
                Assert.Equal(location, match.Groups["location"].Value);
            }
        }

        private static void TestWeatherRegexInvalid(string testString)
        {
            Assert.False(WeatherPlugin.WeatherRegex.IsMatch(testString));
        }

        [Fact]
        public void TestWeatherRegex()
        {
            foreach (var testString in RegexTestUtils.SpaceOut("!weather"))
            {
                TestWeatherRegexValid(testString, false, null);
            }
            foreach (var testString in RegexTestUtils.SpaceOut("!weather", "Vienna"))
            {
                TestWeatherRegexValid(testString, false, "Vienna");
            }
            foreach (var testString in RegexTestUtils.SpaceOut("!lweather"))
            {
                TestWeatherRegexValid(testString, true, null);
            }
            foreach (var testString in RegexTestUtils.SpaceOut("!lweather", "Vienna"))
            {
                TestWeatherRegexValid(testString, true, "Vienna");
            }

            // wrong command
            foreach (var testString in RegexTestUtils.SpaceOut("!leather"))
            {
                TestWeatherRegexInvalid(testString);
            }
            foreach (var testString in RegexTestUtils.SpaceOut("!leather", "Vienna"))
            {
                TestWeatherRegexInvalid(testString);
            }

            // unspaced location
            foreach (var testString in RegexTestUtils.SpaceOut("!weatherVienna"))
            {
                TestWeatherRegexInvalid(testString);
            }
        }
    }
}
