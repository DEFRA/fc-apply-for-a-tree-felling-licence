using Forestry.Flo.Services.Gis.Models.Esri.Responses.Common;
using Forestry.Flo.Services.Gis.Models.Esri.Responses.Query;
using Newtonsoft.Json;

namespace Forestry.Flo.Services.Gis.Models.Esri.Responses.Layers
{
    /// <summary>
    /// This is an example of what we would expect to be returned from the mobile app
    /// This code is to allow UX Layout testing
    /// </summary>
    /// <typeparam name="T">The Data type of the objectID (Probably int or GUID)</typeparam>
    public class SiteVisitNotes<T> : ObjectIdResponse<T>
    {
        [JsonProperty("CaseReference", NullValueHandling = NullValueHandling.Ignore)]
        public string? CaseReference { get; set; }

        [JsonProperty("Officer", NullValueHandling = NullValueHandling.Ignore)]
        public string? VisitOfficer { get; set; }

        [JsonProperty("Notes", NullValueHandling = NullValueHandling.Ignore)]
        public string? Notes { get; set; }


        [JsonProperty("Notes", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(MicrosecondEpochConverter))]
        public DateTime? VisitDateTime { get; set; }

        [JsonProperty("Attachments", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public List<AttachmentDetails<T>> AttachmentDetails { get; set; }

        public SiteVisitNotes()
        {
            AttachmentDetails = new List<AttachmentDetails<T>>();
        }

    }
}
