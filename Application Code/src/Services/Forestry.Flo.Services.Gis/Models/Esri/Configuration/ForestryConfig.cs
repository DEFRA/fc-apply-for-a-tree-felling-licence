using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.Services.Gis.Models.Esri.Configuration
{
    public class ForestryConfig : BaseConfigAGOL<OAuthServiceSettingsClient>
    {
        /// <summary>
        /// The BaseUrl resource only returns the version of the containing portal.
        /// Esri referer to it as the "ROOT".
        /// <see cref="https://developers.arcgis.com/rest/users-groups-and-items/root.htm"/>
        /// </summary>
        [Required]
        public string BaseUrl { get; set; } = null!;


        /// <summary>
        /// Contains all the settings to call the services under the feature service
        /// </summary>
        public FeaturesServiceSettings FeaturesService { get; set; } = null!;

          }


    public class OAuthServiceSettingsClient : BaseEsriServiceConfig
    {
        /// <summary>
        /// The ID of the registered application. This is also referred to as APPID.
        /// </summary>
        public string ClientID { get; set; } = null!;

        /// <summary>
        /// The secret of the registered application. This is also referred to as APPSECRET.
        /// </summary>
        public string ClientSecret { get; set; } = null!;
    }

    public class OAuthServiceSettingsUser : BaseEsriServiceConfig
    {
        /// <summary>
        /// The ID of the user
        /// </summary>
        public string Username { get; set; } = null!;

        /// <summary>
        /// The password for said user
        /// </summary>
        public string Password { get; set; } = null!;
    }



    /// <summary>
    /// Contains all the settings to call the services under the feature service
    /// </summary>
    public class FeaturesServiceSettings : BaseEsriAccessSettingConfig
    {
        /// <summary>
        /// Contains all the "Server" side settings needed to upload and convert a shape file
        /// </summary>
        public GenerateServiceSettings GenerateService { get; set; } = null!;
    }

    /// <summary>
    /// Contains all the "Server" side settings needed to upload and convert a shape file
    /// </summary>
    public class GenerateServiceSettings : BaseEsriAccessSettingConfig
    {
        /// <summary>
        /// The supported file extensions that the upload service 
        /// will accept
        /// </summary>
        public string[] SupportedFileImports { get; set; } = null!;
        /// <summary>
        /// The max number of records to process 
        /// </summary>
        public int? MaxRecords { get; set; }


        /// <summary>
        /// The MaxFileSize that can be upload to Esri
        /// </summary>
        public int MaxFileSizeBytes { get; set; }

        /// <summary>
        /// Should file limits be enforced
        /// </summary>
        public Boolean EnforceInputFileSizeLimit { get; set; }

        /// <summary>
        /// Should the output file be enforced
        /// </summary>
        public Boolean EnforceOutputJsonSizeLimit { get; set; }

    }


    /// <summary>
    /// Contains all the settings to call the services under the Geometry service
    /// </summary>
    public class GeometryServiceSettings : BaseEsriAccessSettingConfig

    {
        /// <summary>
        /// Contains all the settings needed to work out if the shapes intersect
        /// </summary>
        public BaseEsriServiceConfig IntersectService { get; set; } = null!;

        /// <summary>
        /// Merges all the polygons together
        /// </summary>
        public BaseEsriServiceConfig UnionService { get; set; } = null!;

        /// <summary>
        /// Converts the projection system into another
        /// </summary>
        public ProjectServiceSettings ProjectService { get; set; } = null!;
    }

    public class ProjectServiceSettings : BaseEsriServiceConfig
    {
        /// <summary>
        /// The Spatial Ref that the new points should be generated in.
        /// </summary>
        public int OutSR { get; set; }
        
        /// <summary>
        /// The Grid Length of the OS string
        /// </summary>
        public int GridLength { get; set; }

        public bool IncludeSpaces { get; set; }

    }
}


