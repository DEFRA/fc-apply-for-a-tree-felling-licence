using Ardalis.GuardClauses;
using Forestry.Flo.Services.Gis.Services;
using Forestry.Flo.Services.Gis.Interfaces;
using Forestry.Flo.Services.Gis.Models.Esri.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Forestry.Flo.Services.Gis
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddGisServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            Guard.Against.Null(services);
            Guard.Against.Null(configuration);

            //Add Esri settings
            var section = configuration.GetSection(nameof(EsriConfig));
            services.AddSingleton(section.Get<EsriConfig>());
            
            services.AddHttpClient<IForestryServices, ForestryServices>("EsriMessageClient");
            services.AddHttpClient<Interfaces.IForesterServices, ForesterServices>("ForesterMessageClient");

            services.AddSingleton<ISupportedConfig, SupportedConfig>();
            services.AddScoped<IForestryServices, ForestryServices>();
            services.AddScoped<IForesterServices, ForesterServices>();

            services.Configure<LandInformationSearchOptions>(configuration.GetSection("LandInformationSearch"));
            services.AddScoped<ILandInformationSearch, LandInformationSearch>();

            return services;
        }
    }
}