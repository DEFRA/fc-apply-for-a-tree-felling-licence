using Ardalis.GuardClauses;
using Forestry.Flo.Services.InternalUsers.Repositories;
using Forestry.Flo.Services.InternalUsers.Services;
using Forestry.Flo.Services.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace Forestry.Flo.Services.InternalUsers;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds all services to the provided <see cref="ServiceCollection"/> made available for the applicants service.
    /// </summary>
    /// <param name="services">The collection of services to register against.</param>
    /// <param name="options">A callback for configuration of the EF database context.</param>
    public static IServiceCollection AddInternalUsersServices(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> options)
    {
        Guard.Against.Null(services);
        Guard.Against.Null(options);

        services.AddDbContextFactory<InternalUsersContext>(options);
        services.AddSingleton<IDbContextFactorySource<InternalUsersContext>, CustomDbContextFactorySource<InternalUsersContext>>();
        services.AddScoped<IUserAccountRepository, UserAccountRepository>();
        services.AddScoped<IUserAccountService, UserAccountService>();
        services.AddScoped<ISignInInternalUser, SignInInternalUserWithEf>();

        return services;
    }
}