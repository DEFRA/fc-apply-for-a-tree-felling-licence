using Forestry.Flo.Services.Gis.Models.Esri.RequestObjects.Json.Export;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Forestry.Flo.Services.Gis.Models.Esri.Responses;

public class JobResponse
{
    /// <summary>
    /// The Id of the job.
    /// </summary>
    [JsonProperty("jobId")]
    public string Id { get; set; } = null!;


    /// <summary>
    /// The Overall status of the job
    /// </summary>
    [JsonProperty("jobStatus")]
    public string Status { get; set; } = null!;

    /// <summary>
    /// The messages that are returned from the job
    /// </summary>
    [JsonProperty("messages")]
    public List<Message> Messages { get; set; } = [];

    [JsonProperty("results")]
    public OutputDetails? Results { get; set; }

    /// <summary>
    /// The inputs that are returned from the job
    /// </summary>
    [JsonProperty("inputs")]
    public InputDetails? Inputs { get; set; }

}

public class Message
{
    /// <summary>
    /// The status of the message
    /// </summary>
    [JsonProperty("type")]
    public string Type { get; set; } = null!;

    /// <summary>
    /// The description of the message
    /// </summary>
    [JsonProperty("description")]
    public string Description { get; set; } = null!;
}

public class OutputDetails
{
    [JsonProperty("Output_File")]
    public ParamUrlDetails OutputPath { get; set; } = null!;
}


public class InputDetails
{
    /// <summary>
    /// The URL of the web map that is used for the job
    /// </summary>
    [JsonProperty("Web_Map_as_JSON")]
    public ParamUrlDetails WebMap { get; set; } = null!;

    /// <summary>
    /// The format that is used for the job
    /// </summary>
    [JsonProperty("Format")]
    public ParamUrlDetails Format { get; set; } = null!;

    /// <summary>
    /// The layout template that is used for the job
    /// </summary>
    [JsonProperty("Layout_Template")]
    public ParamUrlDetails LayoutTemplate { get; set; } = null!;
}

public class ParamUrlDetails
{
    /// <summary>
    /// The URL of the input
    /// </summary>
    [JsonProperty("paramUrl")]
    public string Url { get; set; } = null!;
}



