using Ardalis.GuardClauses;
using Forestry.Flo.Services.DataImport.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Forestry.Flo.Services.DataImport;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds all services to the provided <see cref="ServiceCollection"/> made available for the data imports services.
    /// </summary>
    /// <param name="services">The collection of services to register against.</param>
    public static IServiceCollection AddDataImportServices(this IServiceCollection services)
    {
        Guard.Against.Null(services);

        services.AddScoped<IImportData, ImportDataService>();
        services.AddScoped<IValidateImportFileSets, ValidateImportFileSetsService>();
        services.AddScoped<IReadImportFileCollections, ReadInputFileCollectionService>();

        return services;
    }
}