using Newtonsoft.Json;
using System.Collections.Specialized;
using System.Text;
using System.Web;

namespace Forestry.Flo.Services.Gis.Models.Esri.RequestObjects.Form
{
    public class GenerateParameters : CommonParameters
    {
        /// <summary>
        /// The type of file being upload. 
        /// Possible values are:
        /// <list type="bullet">
        /// <item>shapefile</item>
        /// <item>csv</item>
        /// <item>gpx</item>
        /// <item>geojson</item>
        /// </list>
        /// "shapefile" is set by default
        /// </summary>
        [JsonProperty("fileType")]
        public string FileType { get; set; }

        /// <summary>
        /// The publish parameters to be set.
        /// </summary>
        [JsonProperty("publishParameters")]
        public PublishParameters? PublishParameters { get; set; }

        public string? Text { get; set; }

        public GenerateParameters()
            : base()
        {
            FileType = "shapefile";
        }

        public string GetQuery()
        {
            var data = new Dictionary<string, string>() {
                { "f", RequestFormat },
                { "filetype", FileType },
                { "publishParameters", JsonConvert.SerializeObject(PublishParameters) }
            };

            return GetQueryString(data);
        }

        public override FormUrlEncodedContent ToFormUrlEncodedContentFormData()
        {
            var data = new Dictionary<string, string>() {
                { "text", Text?? "" },
                {"filetype", FileType },
                {"f", RequestFormat},
                {"publishParameters", JsonConvert.SerializeObject(PublishParameters).ToString()}
            };

            if (TokenString.HasValue)
            {
                data.Add("token", TokenString.Value);
            }
            return new FormUrlEncodedContent(data);
        }

        private string GetQueryString(Dictionary<string, string> queryStringParams)
        {
            var startingQuestionMarkAdded = false;
            var sb = new StringBuilder();
            foreach (var parameter in queryStringParams.Where(parameter => parameter.Value != null))
            {
                sb.Append(startingQuestionMarkAdded ? '&' : '?');
                sb.Append(parameter.Key);
                sb.Append('=');
                sb.Append(parameter.Value);
                startingQuestionMarkAdded = true;
            }
            return sb.ToString();
        }
    }
}
