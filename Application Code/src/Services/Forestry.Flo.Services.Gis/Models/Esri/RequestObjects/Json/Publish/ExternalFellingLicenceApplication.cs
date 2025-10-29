using Forestry.Flo.Services.Gis.Models.Esri.Responses.Query;
using Newtonsoft.Json;

namespace Forestry.Flo.Services.Gis.Models.Esri.RequestObjects.Json.Publish
{
    /// <summary>
    /// Represents an external felling licence application used within the GIS services.
    /// </summary>
    /// <remarks>
    /// This class is utilized for handling JSON serialization and deserialization of external felling licence applications.
    /// It includes properties such as application reference, status, expiry category, conditions, and expiry date.
    /// </remarks>
    public class ExternalFellingLicenceApplication<T> : ObjectIdResponse<T>
    {

        [JsonProperty("application_reference", NullValueHandling = NullValueHandling.Ignore)]
        public string? ApplicationReference { get; set; }

        [JsonProperty("application_status", NullValueHandling = NullValueHandling.Ignore)]
        public string? ApplicationStatus { get; set; }

        [JsonProperty("expiry_category", NullValueHandling = NullValueHandling.Ignore)]
        public string? ExpiryCategory { get; set; }

        [JsonProperty("conditions", NullValueHandling = NullValueHandling.Ignore)]
        public string? Conditions { get; set; }

        [JsonProperty("expiry_date", NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? ExpiryDate { get; set; }
    }
}
