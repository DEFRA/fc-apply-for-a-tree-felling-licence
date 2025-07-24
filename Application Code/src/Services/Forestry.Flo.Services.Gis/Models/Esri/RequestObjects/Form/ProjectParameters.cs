using Forestry.Flo.Services.Gis.Models.Esri.Common;
using Forestry.Flo.Services.Gis.Models.MapObjects;
using Newtonsoft.Json;

namespace Forestry.Flo.Services.Gis.Models.Esri.RequestObjects.Form
{
    public class ProjectParameters<T> : CommonParameters
    {
        public Geometries<T> Shapes { get; set; } = null!;

        public int OutSR { get; set; }

        public int InSR { get; set; }

        public override FormUrlEncodedContent ToFormUrlEncodedContentFormData()
        {
            var data = new List<KeyValuePair<string, string>> {
                new KeyValuePair<string, string>("geometries", JsonConvert.SerializeObject(Shapes)),
                new KeyValuePair<string, string>("outSR", OutSR.ToString()),
                new KeyValuePair<string, string>("inSR", InSR.ToString()),
                new KeyValuePair<string, string>("f", RequestFormat)
            };
            if (TokenString.HasValue)
            {
                data.Add(new KeyValuePair<string, string>("token", TokenString.Value));
            }
            return new FormUrlEncodedContent(data.ToArray());
        }

    }
}
