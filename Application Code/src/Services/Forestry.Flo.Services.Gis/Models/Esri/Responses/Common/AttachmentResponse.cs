using System;
using Newtonsoft.Json;

namespace Forestry.Flo.Services.Gis.Models.Esri.Responses.Common
{
    public class AttachmentResponse<T>
    {
        [JsonProperty("attachmentInfos")]
        public List<AttachmentDetails<T>> Attachments { get; set; }
    }

    public class AttachmentDetails<T>
    {
        [JsonProperty("id")]
        public T ID { get; set; }

        [JsonProperty("contentType")]
        public string ContentType { get; set; }

        [JsonProperty("size")]
        public int Size { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("file", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public byte[] File { get; set; }
    }
}
