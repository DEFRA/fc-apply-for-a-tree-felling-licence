using Newtonsoft.Json;

namespace Forestry.Flo.Services.Gis.Models.Esri.RequestObjects.Json.Export;

public class WebMap_ExportOptions
{
    [JsonProperty("dpi")]
    public int DotsPerInch { get; set; } = 96;

    [JsonProperty("outputSize", NullValueHandling = NullValueHandling.Ignore)]
    public List<int>? Size { get; set; } = null;
}
