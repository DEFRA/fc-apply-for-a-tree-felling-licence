using Newtonsoft.Json;

namespace Forestry.Flo.Services.Gis.Models.Esri.RequestObjects.Form;

public partial class PublishParameters
{
    /// <summary>
    /// The name of the file to be up loaded
    /// </summary>
    [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
    public string? FileName { get; set; }

    /// <summary>
    /// The spatial reference to use.
    /// <seealso cref="EsriConfig.SpatialReference"/>
    /// </summary>
    [JsonProperty("targetSR")]
    public required SpatialReference SpatialReference { get; set; }

    /// <summary>
    /// The maximum number of records to upload
    /// <seealso cref="UploadSettings.MaxRecords"/>
    /// </summary>
    [JsonProperty("maxRecordCount")]
    public int MaxNumberOfRecords { get; set; }

    /// <summary>
    /// Enforce a limit on the upload size
    /// <seealso cref="UploadSettings.EnforceInputFileSizeLimit"/>
    /// </summary>
    [JsonProperty("enforceInputFileSizeLimit")]
    public bool EnforceInputFileSizeLimit { get; set; }

    /// <summary>
    /// Enforce a limit on the output size.
    /// <seealso cref="UploadSettings.EnforceOutputJsonSizeLimit"/>
    /// </summary>
    [JsonProperty("enforceOutputJsonSizeLimit")]
    public bool EnforceOutputJsonSizeLimit { get; set; }

    ///// <summary>
    ///// Generalize the shape output
    ///// </summary>
    //[JsonProperty("generalize", NullValueHandling = NullValueHandling.Ignore)]
    //public bool GeneralizeOutput { get; set; }

    ///// <summary>
    ///// The maximum offset allowed on item
    ///// </summary>
    //[JsonProperty("maxAllowableOffset", NullValueHandling = NullValueHandling.Ignore)]
    //public int MaxAllowableOffset { get; set; }

    ///// <summary>
    ///// Reduces the precision of the items generated
    ///// </summary>
    //[JsonProperty("reducePrecision", NullValueHandling = NullValueHandling.Ignore)]
    //public bool ReducePrecision { get; set; }

    ///// <summary>
    ///// The number of decimal points to round to
    ///// </summary>
    //[JsonProperty("numberOfDigitsAfterDecimal", NullValueHandling = NullValueHandling.Ignore)]
    //public int? NumberOfDigitsAfterDecimal { get; set; }
}

public class SpatialReference
{
    [JsonProperty("wkid")]
    public int Wkid { get; set; }
}