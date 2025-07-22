namespace Forestry.Flo.Services.Applicants.Configuration
{
    /// <summary>
    /// Forestry Commission Agency configuration
    /// </summary>
    public class FcAgencyOptions
    {
        /// <summary>
        /// List of email domains which are checked against before assigning a user the ability to
        /// act as an FC Agent user.
        /// </summary>
        public List<string> PermittedEmailDomainsForFcAgent { get; set; } = new()
        {
            "qxlva.com",
            "forestrycommission.gov.uk",
            "harriscomputer.com"
        };
    }
}
