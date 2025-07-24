using Newtonsoft.Json;

namespace Forestry.Flo.Services.Gis.Models.Esri.RequestObjects.Json.Export;
public class WebMap_Json
{
    [JsonProperty("operationalLayers")]
    public List<WebMap_OperationalLayerBase> OperationalLayers { get; set; } = new();

    [JsonProperty("mapOptions")]
    public WebMap_MapOptions MapOptions { get; set; } = new();

    [JsonProperty("exportOptions")]
    public WebMap_ExportOptions ExportOptions { get; set; } = new();

    [JsonProperty("layoutOptions", NullValueHandling = NullValueHandling.Ignore)]
    public WebMap_LayoutOptions? LayoutOptions { get; set; }

    [JsonProperty("baseMap")]
    public List<WebMap_BaseMap> BaseMap { get; set; } = new() { new WebMap_BaseMap() };
}
