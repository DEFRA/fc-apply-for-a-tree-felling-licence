using CSharpFunctionalExtensions;

namespace Forestry.Flo.Services.Gis.Models.Esri.RequestObjects.Form
{
    public class BaseParameter
    {
        /// <summary>
        /// The response format. <br /><br />
        /// Possible values:
        /// <list type="bullet">
        /// <item>html</item> 
        /// <item>json</item>
        /// </list>
        /// <remarks>Default value = "json"</remarks>
        /// </summary>
        public string RequestFormat { get; set; }

        /// <summary>
        /// The key to accessing the ESRI systems.
        /// <see cref="https://developers.arcgis.com/rest/users-groups-and-items/generate-token.htm"/>
        /// </summary>
        public Maybe<string> TokenString { get; set; } = Maybe<string>.None;

        public BaseParameter()
        {
            RequestFormat = "json";
        }

        /// <summary>
        /// To encode the request settings into FormUrlEncodedContentFormData
        /// </summary>
        /// <returns>The object in the FormUrlEncodedContent format</returns>
        public virtual FormUrlEncodedContent ToFormUrlEncodedContentFormData()
        {
            return new FormUrlEncodedContent(Array.Empty<KeyValuePair<string, string>>());
        }
    }
}
