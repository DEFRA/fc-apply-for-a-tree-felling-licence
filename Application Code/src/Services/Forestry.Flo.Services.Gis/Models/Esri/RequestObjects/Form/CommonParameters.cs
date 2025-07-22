namespace Forestry.Flo.Services.Gis.Models.Esri.RequestObjects.Form
{
    public class CommonParameters : BaseParameter
    {


        /// <summary>
        /// Specifies whether the edits should be applied only if all submitted edits succeed. If false, the server will apply the edits that succeed even if some of the submitted edits fail. If true, the server will apply the edits only if all edits succeed. The default value is true.
        /// Not all data supports setting this parameter.Query the supportsRollbackonFailureParameter property of the layer to determine whether a layer supports setting this parameter.If supportsRollbackOnFailureParameter = false for a layer, then when editing this layer, rollbackOnFailure will always be true, regardless of how the parameter is set.However, if supportsRollbackonFailureParameter = true, the rollbackOnFailure parameter value will be honored on edit operations.
        /// </summary>
        public bool RollbackOnError { get; set; }


        public CommonParameters() :
            base()
        {
            RollbackOnError = true;
        }

        public override FormUrlEncodedContent ToFormUrlEncodedContentFormData()
        {
            var data = new List<KeyValuePair<string, string>>
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
