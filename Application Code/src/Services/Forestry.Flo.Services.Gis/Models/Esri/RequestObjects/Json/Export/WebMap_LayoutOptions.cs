using Newtonsoft.Json;


namespace Forestry.Flo.Services.Gis.Models.Esri.RequestObjects.Json.Export;
public class WebMap_LayoutOptions
{
    [JsonProperty("titleText", NullValueHandling = NullValueHandling.Ignore)]
    public string? Title_Text { get; set; } 

    [JsonProperty("authorText", NullValueHandling = NullValueHandling.Ignore)]
    public string? Author_Text { get; set; }

    [JsonProperty("copyrightText", NullValueHandling = NullValueHandling.Ignore)]
    public string? Copyright_Text { get; set; }

    [JsonProperty("customTextElements", NullValueHandling = NullValueHandling.Ignore)]
    public  Dictionary<string, string>? CustomTextElements { get; set; }

    [JsonProperty("scaleBarOptions", NullValueHandling = NullValueHandling.Ignore)]
    public ScalebarOptions? ScaleBar_Options { get; set; }

    [JsonProperty("legendOptions", NullValueHandling = NullValueHandling.Ignore)]
    public LegendOptions? Legend_Options { get; set; }
}

public class ScalebarOptions { }

public class LegendOptions
{
    [JsonProperty("operationalLayers", NullValueHandling = NullValueHandling.Ignore)]
    public List<string>? Operational_Layers { get; set; }
}
