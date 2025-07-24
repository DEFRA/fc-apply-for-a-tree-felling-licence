using Ardalis.GuardClauses;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.PropertyProfiles.DataImports;
using Forestry.Flo.Services.PropertyProfiles.Repositories;
using Forestry.Flo.Services.PropertyProfiles.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace Forestry.Flo.Services.PropertyProfiles;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds all services to the provided <see cref="ServiceCollection"/> made available for the applicants service.
    /// </summary>
    /// <param name="services">The collection of services to register against.</param>
    /// <param name="options">A callback for configuration of the EF database context.</param>
    public static IServiceCollection AddPropertyProfilesServices(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> options)
    {
        Guard.Against.Null(services);
        Guard.Against.Null(options);

        services.AddDbContextFactory<PropertyProfilesContext>(options);
        services.AddSingleton<IDbContextFactorySource<PropertyProfilesContext>,
            CustomDbContextFactorySource<PropertyProfilesContext>>();

        services.AddScoped<IGetPropertyProfiles, GetPropertyProfilesService>();
        services.AddScoped<IGetCompartments, GetCompartmentsService>();

        services.AddScoped<IPropertyProfileRepository, PropertyProfileRepository>();
        services.AddScoped<ICompartmentRepository, CompartmentRepository>();

        services.AddScoped<IGetPropertiesForWoodlandOwner, GetPropertiesForWoodlandOwnerService>();

        return services;
    }
}









