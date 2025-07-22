using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Forestry.Flo.Services.Gis.Models.Esri.Responses.Common
{
    public class EsriTokenResponse
    {
        /// <summary>
        /// The token to use in  the request
        /// </summary>
        [JsonProperty("token", NullValueHandling = NullValueHandling.Ignore)]
        public string? TokenString { get; set; }

        /// <summary>
        /// When the Token Expires
        /// </summary>
        [JsonProperty("expires", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(TokenStampConvertor))]
        public DateTime? TimeStamp { get; set; }

        [JsonProperty("access_token", NullValueHandling = NullValueHandling.Ignore)]
        public string? AccessToken  { get { return TokenString; } set { TokenString = value; } }

        [JsonProperty("expires_in", NullValueHandling = NullValueHandling.Ignore)]
        public int? ExpiresIn { get; set; }

        public DateTime Expiry { get; set; }

        

        public EsriTokenResponse()
        {
            TokenString = "";
        }
    }
}
