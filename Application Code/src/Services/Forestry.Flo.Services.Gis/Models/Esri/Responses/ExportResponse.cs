using Newtonsoft.Json;
namespace Forestry.Flo.Services.Gis.Models.Esri.Responses;

public class ExportResponse
{
    [JsonProperty("jobId")]
    public string Id { get; set; }

    [JsonProperty("jobStatus")]
    public string JobStatus { get; set; }
}

