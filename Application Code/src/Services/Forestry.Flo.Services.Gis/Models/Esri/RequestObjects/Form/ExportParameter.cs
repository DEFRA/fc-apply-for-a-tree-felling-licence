using Forestry.Flo.Services.Gis.Models.Esri.RequestObjects.Json.Export;
using Newtonsoft.Json;
using static System.Net.Mime.MediaTypeNames;

namespace Forestry.Flo.Services.Gis.Models.Esri.RequestObjects.Form
{
    public class ExportParameter : CommonParameters
    {
        public WebMap_Json MapJson { get; set; } = new();

        public string FileFormat { get; set; } = "PNG8";

        public string? LayoutTemplate { get; set; }

        public override FormUrlEncodedContent ToFormUrlEncodedContentFormData()
        {
            var data = new Dictionary<string, string>() {
                { "Web_Map_as_JSON", JsonConvert.SerializeObject(MapJson).ToString() },
                {"Format", FileFormat },
                {"f", RequestFormat}
            };

            if (!string.IsNullOrEmpty(LayoutTemplate))
            {
                data.Add("Layout_Template", LayoutTemplate);
            }

            if (TokenString.HasValue)
            {
                data.Add("token", TokenString.Value);
            }
            return new FormUrlEncodedContent(data);
        }


    }
}
