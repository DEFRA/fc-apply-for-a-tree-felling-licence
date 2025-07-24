using Newtonsoft.Json;

namespace Forestry.Flo.Services.Gis.Models.Esri.RequestObjects.Form
{
    public class AddFeaturesParameter<T> : CommonParameters
    {
        /// <summary>
        /// The features to send. Note 
        /// </summary>
        public T Features { get; set; }

        public AddFeaturesParameter(T features)
        {
            Features = features;
        }

        /// <inheritdoc />
        public override FormUrlEncodedContent ToFormUrlEncodedContentFormData()
        {
            var data = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("features", JsonConvert.SerializeObject(Features).ToString()),
                new KeyValuePair<string, string>("rollbackOnFailure", RollbackOnError.ToString()),
                new KeyValuePair<string, string>("gdbVersion", ""),
                new KeyValuePair<string, string>("f", RequestFormat)
            };

            if (TokenString.HasValue)
            {
                data.Add(new KeyValuePair<string, string>("token", TokenString.Value));
            }
            return new FormUrlEncodedContent(data);
        }

    }
}
