using Newtonsoft.Json;

namespace Time.GeoNames
{
    [JsonObject]
    public class GeoName
    {
        [JsonProperty("adminCode1")]
        public string AdminCode1 { get; set; }

        [JsonProperty("adminName1")]
        public string AdminName1 { get; set; }

        [JsonProperty("countryCode")]
        public string CountryCode { get; set; }

        [JsonProperty("countryId")]
        public string CountryID { get; set; }

        [JsonProperty("countryName")]
        public string CountryName { get; set; }

        [JsonProperty("fcl")]
        public string FCL { get; set; }

        [JsonProperty("fclName")]
        public string FCLName { get; set; }

        [JsonProperty("fcode")]
        public string FCode { get; set; }

        [JsonProperty("fcodeName")]
        public string FCodeName { get; set; }

        [JsonProperty("geonameId")]
        public long GeoNameID { get; set; }

        [JsonProperty("lat")]
        public decimal Latitude { get; set; }

        [JsonProperty("lng")]
        public decimal Longitude { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("population")]
        public long Population { get; set; }

        [JsonProperty("toponymName")]
        public string ToponymName { get; set; }
    }
}
