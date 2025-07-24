using Ardalis.GuardClauses;
using Forestry.Flo.Services.Gis.Interfaces;
using Forestry.Flo.Services.Gis.Models.Esri.Configuration;

namespace Forestry.Flo.Services.Gis.Services
{
    /// <summary>
    /// For accessing information from the configuration file
    /// </summary>
    public class SupportedConfig:ISupportedConfig
    {
        private readonly EsriConfig _config;
        public SupportedConfig(EsriConfig esriConfig)
        {
            Guard.Against.Null(esriConfig);
            _config = esriConfig;
        }

        ///<inheritdoc />
        public string[] GetSupportedFileTypes()
        {
            Guard.Against.Null(_config.Forestry, message: "Settings not correctly set");
            Guard.Against.Null(_config.Forestry.FeaturesService, message: "Settings not correctly set");
            Guard.Against.Null(_config.Forestry.FeaturesService.GenerateService, message: "Settings not correctly set");

            return Guard.Against.Null(_config.Forestry.FeaturesService.GenerateService.SupportedFileImports,
                message: "Settings not correctly set");
        }
    }
}
