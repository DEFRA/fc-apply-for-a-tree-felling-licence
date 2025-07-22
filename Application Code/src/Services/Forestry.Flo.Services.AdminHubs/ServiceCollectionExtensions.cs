using Ardalis.GuardClauses;
using Forestry.Flo.Services.AdminHubs.Repositories;
using Forestry.Flo.Services.AdminHubs.Services;
using Forestry.Flo.Services.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace Forestry.Flo.Services.AdminHubs;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds all services to the provided <see cref="ServiceCollection"/> made available for the admin hub service.
    /// </summary>
    /// <param name="services">The collection of services to register against.</param>
    /// <param name="options">A callback for configuration of the EF database context.</param>
    public static IServiceCollection AddAdminHubServices(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> options)
    {
        Guard.Against.Null(services);
        Guard.Against.Null(options);

        services.AddDbContextFactory<AdminHubContext>(options);
        services.AddSingleton<IDbContextFactorySource<AdminHubContext>, CustomDbContextFactorySource<AdminHubContext>>();
        services.AddScoped<IAdminHubRepository, AdminHubRepository>();
        services.AddScoped<IAdminHubService, AdminHubService>();
        return services;
    }
}