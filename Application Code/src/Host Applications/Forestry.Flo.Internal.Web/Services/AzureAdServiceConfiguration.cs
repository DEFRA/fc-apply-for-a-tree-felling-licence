namespace Forestry.Flo.Internal.Web.Services
{
    public class AzureAdServiceConfiguration
    {
        /// <summary>
        /// instance of Azure AD, for example public Azure or a Sovereign cloud (government, etc ...)
        /// </summary>
        public string Instance { get; set; }

        /// <summary>
        /// Graph API endpoint, public Azure (default) or a Sovereign cloud (government, etc ...)
        /// </summary>
        public string ApiUrl { get; set; }

        /// <summary>
        /// Directory (tenant) ID:
        /// Either the tenant ID of the Azure AD tenant in which this application is registered (a guid)
        /// or a domain name associated with the tenant, or 'organizations' (for a multi-tenant application)
        /// </summary>
        public string TenantId { get; set; }

        /// <summary>
        /// Application (client) ID:
        /// Guid used by the application to uniquely identify itself to Azure AD
        /// </summary>
        public string ClientId { get; set; }

        /// <summary>
        /// URL of the authority
        /// </summary>
        public string Authority => $"{Instance}{TenantId}";

        /// <summary>
        /// Client secret (application password)
        /// </summary>
        public string ClientSecret { get; set; }
    }
}
