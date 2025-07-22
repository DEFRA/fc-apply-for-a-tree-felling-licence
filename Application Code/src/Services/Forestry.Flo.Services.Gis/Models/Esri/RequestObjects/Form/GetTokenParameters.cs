using System.Runtime.CompilerServices;

namespace Forestry.Flo.Services.Gis.Models.Esri.RequestObjects.Form
{
    /// <summary>
    /// THe Token object to reqeust a token to log in to the service
    /// </summary>
    public class GetTokenParameters : BaseParameter
    {
        /// <summary>
        /// If the service isn't running under Oauth then this is "Username"
        /// If it is then this is the ClientID.
        /// </summary>
        public string RunningAccountID { get; set; }

        /// <summary>
        /// The Secret for the running Account:
        /// If the service isn't running under Oauth then this is "Username"
        /// If it is then this is the ClientID.
        /// </summary>
        public string Secret { get; set; }

        /// <summary>
        /// How long in minutes the token should last
        ///  <remarks>Default value = 60 </remarks>
        /// </summary>
        public int Expiration { get; set; }

        /// <summary>
        /// If the Service will generate and OAuth request or a simple user request
        /// </summary>
        public bool IsOauth { get; private set; }

        /// <summary>
        /// Generates an object to pass to the service to login
        /// </summary>
        /// <param name="id">The username to use to log into the service</param>
        /// <param name="secret">The password to use to send to the service</param>
        public GetTokenParameters(string id, string secret, bool isOauth =false)
            : base()
        {
            RunningAccountID = id;
            Secret = secret;
            IsOauth = isOauth;  
            Expiration = 60;
        }

        /// <summary>
        /// Converts the object to FormUrlEncodedContent
        /// </summary>
        /// <returns>The object settings in FormUrlEncodedContent</returns>
        public override FormUrlEncodedContent ToFormUrlEncodedContentFormData()
        {
            Dictionary<string, string> data = new Dictionary<string, string>(){
                {   "f", RequestFormat }
            };
            if (IsOauth)
            {
                data.Add("client_id", RunningAccountID);
                data.Add("client_secret", Secret);
                data.Add("grant_type", "client_credentials");
            }
            else
            {
                data.Add("username", RunningAccountID);
                data.Add("password", Secret);
            }
            return new FormUrlEncodedContent(data.ToArray());
        }

    }
}
