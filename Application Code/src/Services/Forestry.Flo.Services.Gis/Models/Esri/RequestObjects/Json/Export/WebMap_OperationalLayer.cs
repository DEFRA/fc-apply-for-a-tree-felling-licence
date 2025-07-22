using Forestry.Flo.Services.Gis.Models.Internal;
using Forestry.Flo.Services.Gis.Models.Internal.MapObjects;
using Newtonsoft.Json;

namespace Forestry.Flo.Services.Gis.Models.Esri.RequestObjects.Json.Export;

public class WebMap_OperationalLayerBase
{
    [JsonProperty("id")]
    public string ID { get; set; }

    [JsonProperty("title")]
    public string Title { get; set; }

    [JsonProperty("opacity")]
    public int Opacity { get; set; }

    [JsonProperty("minScale")]
    public int MinScale { get; set; }

    [JsonProperty("maxScale")]
    public int MaxScale { get; set; }
}

public class WebMap_OperationalLayerMap : WebMap_OperationalLayerBase
{
    [JsonProperty("layerType")]
    public string LayerType { get; set; }

    [JsonProperty("url")]
    public string URL { get; set; }

    [JsonProperty("token", NullValueHandling = NullValueHandling.Ignore)]
    public string? Token { get; set; }
}

public class WebMapOperationalLayerFeatures : WebMap_OperationalLayerBase
{
    [JsonProperty("featureCollection")]
    public FeatureCollection Features { get; set; }

    [JsonProperty("showLabels")] 
    public bool ShowLabels { get; set; } = false;

    public WebMapOperationalLayerFeatures(LayerDefinitionDetails layerDefinitionDetails, FeatureSetDetails featureSet)
    {
        Features = new(layerDefinitionDetails, featureSet);
        Title = featureSet.GeometryTypeName;
        ID = featureSet.GeometryTypeName;
        Opacity = 1;
    }
}


public class FeatureCollection
{
    [JsonProperty("layers")]
    public List<FeatureLayer> Layers { get; set; }

    public FeatureCollection(LayerDefinitionDetails layerDefinitionDetails, FeatureSetDetails featureSet)
    {
        Layers = new() { new FeatureLayer(layerDefinitionDetails, featureSet) };
    }
}

public class FeatureLayer
{
    [JsonProperty("layerDefinition")]
    public LayerDefinitionDetails LayerDefinitionDetails { get; set; }

    [JsonProperty("featureSet")]
    public FeatureSetDetails FeatureSet { get; set; }

    public FeatureLayer(LayerDefinitionDetails layerDefinitionDetails, FeatureSetDetails featureSet)
    {
        LayerDefinitionDetails = layerDefinitionDetails;
        FeatureSet = featureSet;
    }
}

public class LayerDefinitionDetails
{
    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("geometryType")]
    public string GeometryType { get; set; }

    [JsonProperty("drawingInfo", NullValueHandling = NullValueHandling.Ignore)]
    public DrawinginfoDetails? DrawingInfo { get; set; }

    [JsonProperty("fields", NullValueHandling = NullValueHandling.Ignore)]
    public List<FieldDetails>? LayerFields { get; set; }

    public LayerDefinitionDetails(string geoType, DrawinginfoDetails? details, string? name = null)
    {

        GeometryType = geoType;
        Name = name ?? geoType;
        DrawingInfo = details;
    }


}

public class DrawinginfoDetails
{
    [JsonProperty("renderer")]
    public RendererDetails RenderSettings { get; set; }

    [JsonProperty("labelingInfo")]
    public List<LabelingDetails> LabelingInfo { get; set; }

    public DrawinginfoDetails()
    {
        LabelingInfo = new() { new LabelingDetails() };
        RenderSettings = new();
    }
}

public class RendererDetails
{
    [JsonProperty("type")]
    public string RenderType { get; set; } = "simple";

    [JsonProperty("symbol")]
    public SymbolDetails SymbolSettings { get; set; } = new();
}

public class BaseEsriSymbol
{

    [JsonProperty("type")]
    public string SymbolType { get; set; }
}

public class SymbolDetails : BaseEsriSymbol
{

    [JsonProperty("color")]
    public List<int> SymbolColour { get; set; } = new() { 34, 139, 34, 100 };

    [JsonProperty("outline")]
    public OutlineDetails? OutLineSettings { get; set; } = new();

    [JsonProperty("style")]
    public string SymbolStyle { get; set; } = "esriSFSSolid";

    [JsonProperty("angle", NullValueHandling = NullValueHandling.Ignore)]
    public int? Angle { get; set; }

    [JsonProperty("xoffset", NullValueHandling = NullValueHandling.Ignore)]
    public int? XOffset { get; set; }

    [JsonProperty("yoffset", NullValueHandling = NullValueHandling.Ignore)]
    public int? YOffset { get; set; }

    [JsonProperty("size", NullValueHandling = NullValueHandling.Ignore)]
    public int? Size { get; set; }

    [JsonProperty("width", NullValueHandling = NullValueHandling.Ignore)]
    public int? Width { get; set; }

    [JsonProperty("cap", NullValueHandling = NullValueHandling.Ignore)]
    public string? Cap { get; set; }

    public SymbolDetails()
    {
        SymbolType = "esriSFS";
    }
}

public class SymbolText : BaseEsriSymbol
{

    public List<int> Color { get; set; } = new() { 0, 0, 0, 255 };

    [JsonProperty("font")]
    public FontDetails FontSettings { get; set; } = new();

    [JsonProperty("horizontalAlignment")]
    public string HorizontalAlignment { get; set; } = "center";

    [JsonProperty("kerning")]
    public bool Kerning { get; set; } = true;

    [JsonProperty("haloColor")]
    public List<int> HaloColor { get; set; } = new() { 255, 255, 255, 255 };

    [JsonProperty("haloSize")]
    public int HaloSize { get; set; } = 1;

    [JsonProperty("rotated")]
    public bool Rotated { get; set; } = false;

    [JsonProperty("text")]
    public string TextValue { get; set; }

    [JsonProperty("verticalAlignment")]
    public string VerticalAlignment { get; set; } = "baseline";

    [JsonProperty("xoffset")]
    public int X_offset { get; set; } = 0;

    [JsonProperty("yoffset")]
    public int Y_Offset { get; set; } = 0;

    [JsonProperty("angle")]
    public int Angle { get; set; } = 0;

    public SymbolText(string labelValue)
    {
        TextValue = labelValue;
        SymbolType = "esriTS";
    }

    public SymbolText()
    {
        TextValue = "";
        SymbolType = "esriTS";
    }
}

public class OutlineDetails
{
    [JsonProperty("type")]
    public string OutlineType { get; set; } = "esriSLS";

    [JsonProperty("color")]
    public List<int> OutlineColour { get; set; } = new() { 0, 0, 0, 255 };

    [JsonProperty("width")]
    public int OutlineWidth { get; set; } = 1;

    [JsonProperty("style")]
    public string OutlineStyle { get; set; } = "esriSLSSolid";
}

public class LabelingDetails
{
    [JsonProperty("labelExpression")]
    public string LabelExpression { get; set; }

    [JsonProperty("labelExpressionInfo")]
    public LabelExpressionInfoDetails ExpressionInfo { get; set; }

    [JsonProperty("repeatLabel")]
    public bool RepeatLabel { get; set; }

    [JsonProperty("symbol")]
    public SymbolText TextSymbol { get; set; }

    public LabelingDetails()
    {
        LabelExpression = "[compartmentName]";
        ExpressionInfo = new();
        RepeatLabel = true;
        TextSymbol = new();
    }
}

public class LabelExpressionInfoDetails
{
    [JsonProperty("expression")]
    public string Expression { get; set; } = "$feature.compartmentName";
}

public class FontDetails
{
    [JsonProperty("family")]
    public string Family { get; set; } = "sans-serif";

    [JsonProperty("size")]
    public int Size { get; set; } = 12;

    [JsonProperty("weight")]
    public string Weight { get; set; } = "bold";
}

public class FieldDetails
{
    [JsonProperty("alias")]
    public string Alias { get; set; }

    [JsonProperty("editable")]
    public bool IsEditable { get; set; }

    [JsonProperty("length")]
    public int Length { get; set; }

    [JsonProperty("name")]
    public string FieldName { get; set; }

    [JsonProperty("nullable")]
    public bool CanBeNull { get; set; }

    [JsonProperty("type")]
    public string FontType { get; set; }

    public FieldDetails(string name, string typeName)
    {
        Alias = FieldName = name;
        IsEditable = CanBeNull = true;
        Length = -1;
        FontType = typeName;
    }
}

public class FeatureSetDetails
{
    [JsonProperty("geometryType")]
    public string GeometryTypeName { get; set; }

    [JsonProperty("features")]
    public List<FeatureDetails> Shapes { get; set; }

    public FeatureSetDetails(string geometryTypeName, List<FeatureDetails> features)
    {
        GeometryTypeName = geometryTypeName;
        Shapes = features;
    }
}

public class FeatureDetails
{
    [JsonProperty("aggregateGeometries", NullValueHandling = NullValueHandling.Include)]
    public object? AggregateGeometries { get; set; }

    [JsonProperty("geometry")]
    public BaseShape Shape { get; set; }

    [JsonProperty("symbol", NullValueHandling = NullValueHandling.Include)]
    public BaseEsriSymbol? Symbol { get; set; }


    public FeatureDetails(BaseShape geometry)
    {
        Shape = geometry;
    }

    public FeatureDetails(BaseShape geometry, string labelValue)
    {
        Shape = geometry.GetCenterPoint()!;
        Symbol = new SymbolText(labelValue);
    }
}

