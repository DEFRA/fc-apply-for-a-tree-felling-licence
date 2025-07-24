using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace Forestry.Flo.Services.Gis.Models.Esri.RequestObjects.Json.PublicRegister
{
    public class ForesterCompartment<T> : PublicRegisterBase<T>
    {
        [JsonProperty("label", NullValueHandling = NullValueHandling.Ignore)]
        [MaxLength(50)]
        public string? CompartmentLabel { get; set; }

        [JsonProperty("case_status", NullValueHandling = NullValueHandling.Ignore)]
        [MaxLength(50)]
        public string? Status { get; set; }

        [JsonProperty("decision_on_pr", NullValueHandling = NullValueHandling.Ignore)]
        public string? ONPublicRegister { get; set; }

        [JsonProperty("compartment_no", NullValueHandling = NullValueHandling.Ignore)]
        [MaxLength(50)]
        public string? CompartmentNumber { get; set; }

        [JsonProperty("subcompartment", NullValueHandling = NullValueHandling.Ignore)]
        [MaxLength(50)]
        public string? SubCompartmentNo { get; set; }

        [JsonProperty("public_register_start_date", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(MicrosecondEpochConverter))]
        public DateTime? PublicRegStartDate { get; set; }
    }
}
