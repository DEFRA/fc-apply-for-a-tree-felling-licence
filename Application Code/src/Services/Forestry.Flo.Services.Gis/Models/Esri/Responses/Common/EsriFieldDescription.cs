using Newtonsoft.Json;

namespace Forestry.Flo.Services.Gis.Models.Esri.Responses.Common
{
    public class EsriFieldDescription
    {
        /// <summary>
        /// The Name of the field
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; } = null!;

        /// <summary>
        /// Any Alias given to the field
        /// </summary>
        [JsonProperty("alias")]
        public string FieldAlias { get; set; } = null!;

        /// <summary>
        /// The type of data held in the field
        /// </summary>
        [JsonProperty("type")]
        public string FieldType { get; set; } = null!;

        /// <summary>
        /// Where appropriate the length of the field
        /// </summary>
        [JsonProperty("length")]
        public int? FieldLength { get; set; }
    }
}
