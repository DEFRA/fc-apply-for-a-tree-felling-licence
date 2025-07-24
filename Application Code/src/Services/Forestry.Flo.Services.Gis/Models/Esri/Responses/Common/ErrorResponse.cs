using Newtonsoft.Json;
using System.Numerics;

namespace Forestry.Flo.Services.Gis.Models.Esri.Responses.Common
{
    /// <summary>
    /// Esri to this thing where they can't just return server error codes,
    /// Even if the service fails it returns a 200 with a JSON error.
    /// </summary>
    public class EsriErrorResponse<T>
    {
        /// <summary>
        /// The Error of the 
        /// </summary>
        [JsonProperty("error")]
        public EsriError<T>? Error { get; set; }
    }

    public class EsriError<T>
    {
        /// <summary>
        /// THe HTTP error code
        /// </summary>
        [JsonProperty("code", NullValueHandling = NullValueHandling.Ignore)]
        public BigInteger Code { get; set; }


        [JsonProperty("messageCode", NullValueHandling = NullValueHandling.Ignore)]
        public string MessageCode { get; set; } = null!;

        /// <summary>
        /// The message to go with the error code
        /// </summary>
        [JsonProperty("message", NullValueHandling = NullValueHandling.Ignore)]
        public string? Message { get; set; }

        /// <summary>
        /// The ID given to the request. AGOL Error handling
        /// </summary>
        [JsonProperty("requestId", NullValueHandling = NullValueHandling.Ignore)]
        public string? RequestID { get; set; }

        /// <summary>
        /// The ID given to the error to trace in AGOL 
        /// </summary>
        [JsonProperty("traceId", NullValueHandling = NullValueHandling.Ignore)]
        public string? TraceID { get; set; }


        /// <summary>
        /// Extended Error code
        /// </summary>
        [JsonProperty("extendedCode", NullValueHandling = NullValueHandling.Ignore)]
        public Int64 ExtendedCode { get; set; }

        /// <summary>
        /// The Details to go with the error
        /// </summary>
        [JsonProperty("details", NullValueHandling = NullValueHandling.Ignore)]
        public  T? Details { get; set; }
    }
}
