namespace Forestry.Flo.Services.Gis.Models.Esri.RequestObjects.Form
{
    internal class AttachmentQuery: QueryFeatureServiceParameters
    {

        public AttachmentQuery()
        {
            OutFields = new List<string>();
            WhereString = string.Empty;
            ReturnCountOnly = false;
        }

        /// <inheritdoc />
        public override FormUrlEncodedContent ToFormUrlEncodedContentFormData()
        {

            var data = new List<KeyValuePair<string, string>>()
            {
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
