namespace Forestry.Flo.Services.Gis.Models.Esri.Configuration
{
    public class PublicRegistryConfig
    {
        /// <summary>
        /// Full URL to the Land RegistryConfig
        /// </summary>
        public string BaseUrl { get; set; } = null!;

        /// <summary>
        /// If the service needs a token to access
        /// </summary>
        public Boolean NeedsToken { get; set; }

        /// <summary>
        /// Contains all the settings to be able to generate a token for the system.
        /// </summary>
        public OAuthServiceSettingsUser GenerateTokenService { get; set; } = null!;

        public BaseEsriServiceConfig Boundaries { get; set; } = null!;

        public BaseEsriServiceConfig Compartments { get; set; } = null!;

        public BaseEsriServiceConfig Comments { get; set; } = null!;

        public ESRILookUp LookUps { get; set; }
    }

    public class ESRILookUp
    {
        public Statuses Status { get; set; }
    }

    public class Statuses
    {
        public string InitialProposal { get; set; }

        public string Consultation { get; set; }

        public string FinalProposal { get; set; }

        public string Approved { get; set; }

        public string UploadedByGMS { get; set; }
    }
}
