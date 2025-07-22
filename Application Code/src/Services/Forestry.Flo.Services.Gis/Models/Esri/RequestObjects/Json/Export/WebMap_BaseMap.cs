using Newtonsoft.Json;

namespace Forestry.Flo.Services.Gis.Models.Esri.RequestObjects.Json.Export
{
    public class WebMap_BaseMap
    {
        [JsonProperty("title")]
        public string Title { get; set; } = "BaseMap_OS_VML_MasterMap";

        [JsonProperty("baseMapLayers")]
        public List<BaseMapLayer> Layers { get; set; } = new();
    }
}

public class BaseMapLayer
{
    [JsonProperty("url")]
    public string URL { get; set; }

    public BaseMapLayer(string url)
    {
        URL = url;
    }
}
