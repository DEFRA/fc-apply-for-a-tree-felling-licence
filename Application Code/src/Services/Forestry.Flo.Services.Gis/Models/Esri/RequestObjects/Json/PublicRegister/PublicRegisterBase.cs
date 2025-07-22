using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace Forestry.Flo.Services.Gis.Models.Esri.RequestObjects.Json.PublicRegister
{
    public class PublicRegisterBase<T>
    {
        [JsonProperty("objectid")]
        public T ObjectId { get; set; } = default!;

        [JsonProperty("case_reference", NullValueHandling = NullValueHandling.Ignore)]
        [MaxLength(50)]
        public string CaseReference { get; set; } = null!;
    }
}
