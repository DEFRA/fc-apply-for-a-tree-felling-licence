using Ardalis.GuardClauses;
using Forestry.Flo.Services.Applicants.Repositories;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace Forestry.Flo.Services.Applicants;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds all services to the provided <see cref="ServiceCollection"/> made available for the applicants service.
    /// </summary>
    /// <param name="services">The collection of services to register against.</param>
    /// <param name="options">A callback for configuration of the EF database context.</param>
    public static IServiceCollection AddApplicantServices(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> options)
    {
        Guard.Against.Null(services);
        Guard.Against.Null(options);

        services.AddDbContextFactory<ApplicantsContext>(options);
        services.AddSingleton<IDbContextFactorySource<ApplicantsContext>, CustomDbContextFactorySource<ApplicantsContext>>();
        services.AddScoped<IUserAccountRepository, UserAccountRepository>();
        services.AddScoped<IWoodlandOwnerRepository, WoodlandOwnerRepository>();
        services.AddScoped<IAgencyRepository, AgencyRepository>();
        services.AddScoped<IInvitedUserValidator, InvitedUserValidator>();
        services.AddScoped<ISignInApplicant, SignInApplicantWithEf>();
        services.AddScoped<IAgentAuthorityService, AgentAuthorityService>();
        services.AddScoped<IAgentAuthorityInternalService, AgentAuthorityService>();
        services.AddScoped<IWoodlandOwnerCreationService, WoodlandOwnerCreationService>();
        services.AddScoped<IRetrieveUserAccountsService, RetrieveUserAccountsService>();
        services.AddScoped<IRetrieveWoodlandOwners, RetrieveWoodlandOwnersService>();
        services.AddScoped<IAmendUserAccounts, AmendUserAccountsService>();
        services.AddScoped<IAccountRegistrationService, AccountRegistrationService>();
        services.AddScoped<IRetrieveAgencies, RetrieveAgenciesService>();
        services.AddScoped<IAgencyCreationService, AgencyCreationService>();
        return services;
    }
}