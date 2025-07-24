using Forestry.Flo.Services.Gis.Models.Helpers;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
namespace Forestry.Flo.Services.Gis.Models.Esri.RequestObjects.Json.Mobile;

public class MobileCompartment<T>
{
    [JsonProperty("objectid", NullValueHandling = NullValueHandling.Ignore)]
    public T? ObjectId { get; set; }

    [JsonProperty("globalid", NullValueHandling = NullValueHandling.Ignore)]
    public Guid? EsriId { get; set; }

    [JsonProperty("CreationDate", NullValueHandling = NullValueHandling.Ignore)]
    [JsonConverter(typeof(UnixTimestampConverter))]
    public DateTime? CreationDate { get; set; }

    [JsonProperty("Creator", NullValueHandling = NullValueHandling.Ignore), MaxLength(128)]
    public string? CreatedBy { get; set; }

    [JsonProperty("EditDate", NullValueHandling = NullValueHandling.Ignore)]
    [JsonConverter(typeof(UnixTimestampConverter))]
    public DateTime? EditDate { get; set; }

    [JsonProperty("Editor", NullValueHandling = NullValueHandling.Ignore), MaxLength(128)]
    public string? Editor { get; set; }

    [JsonProperty("case_reference", NullValueHandling = NullValueHandling.Ignore), MaxLength(255)]
    public string? CaseReference { get; set; }

    [JsonProperty("property_name", NullValueHandling = NullValueHandling.Ignore), MaxLength(255)]
    public string? PropertyName { get; set; }

    [JsonProperty("woodland_name", NullValueHandling = NullValueHandling.Ignore), MaxLength(255)]
    public string? WoodlandName { get; set; }

    [JsonProperty("date_of_site_visit", NullValueHandling = NullValueHandling.Ignore), MaxLength(255)]
    [JsonConverter(typeof(UnixTimestampConverter))]
    public DateTime? DateOfVisit { get; set; }

    [JsonProperty("notes_remarks_for_file_only_sit", NullValueHandling = NullValueHandling.Ignore), MaxLength(1000)]
    public string? NotesForFile { get; set; }

    [JsonProperty("notes_remarks_for_licence_advis", NullValueHandling = NullValueHandling.Ignore), MaxLength(1000)]
    public string? NotesForLicence { get; set; }

}

