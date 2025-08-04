using FluentValidation;
using Forestry.Flo.Internal.Web.Infrastructure.Fakes;
using Forestry.Flo.Internal.Web.Services;
using Forestry.Flo.Internal.Web.Services.AccountAdministration;
using Forestry.Flo.Internal.Web.Services.ExternalConsulteeReview;
using Forestry.Flo.Internal.Web.Services.FellingLicenceApplication;
using Forestry.Flo.Services.AdminHubs;
using Forestry.Flo.Services.Applicants;
using Forestry.Flo.Services.InternalUsers;
using Forestry.Flo.Services.InternalUsers.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.ConditionsBuilder;
using Forestry.Flo.Services.FellingLicenceApplications;
using Forestry.Flo.Services.FileStorage;
using Forestry.Flo.Services.FileStorage.Configuration;
using Forestry.Flo.Services.Gis;
using Forestry.Flo.Services.Notifications;
using Forestry.Flo.Services.PropertyProfiles;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth.Claims;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Forestry.Flo.Internal.Web.Services.AdminHub;
using Forestry.Flo.Internal.Web.Services.FellingLicenceApplication.WoodlandOfficerReview;
using Forestry.Flo.Internal.Web.Services.Reports;
using Forestry.Flo.Services.Common.Infrastructure;
using MassTransit;
using Microsoft.IO;
using Forestry.Flo.Internal.Web.Services.FellingLicenceApplication.AdminOfficerReview;
using Forestry.Flo.Services.Common.Analytics;

namespace Forestry.Flo.Internal.Web.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAzureAdB2CServices(this IServiceCollection services, IConfiguration configuration)
    {
        var settings = new AzureAdB2COptions();
        configuration.Bind("AzureAdB2C", settings);

        services
            .AddCors(options =>
            {
                options.AddPolicy("CorsPolicy", policyBuilder => policyBuilder
                    .WithOrigins(settings.Instance)
                    .SetIsOriginAllowedToAllowWildcardSubdomains()
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials());
            })
            .AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            })
            .AddCookie()
            .AddOpenIdConnect(options =>
            {
                options.Authority = settings.Authority;
                options.ClientId = settings.ClientId;
                options.ClientSecret = settings.ClientSecret;
                options.UseTokenLifetime = true;
                options.GetClaimsFromUserInfoEndpoint = true;
                options.ResponseType = OpenIdConnectResponseType.Code;
                options.ResponseMode = OpenIdConnectResponseMode.Query;
                options.UsePkce = false;
                options.AccessDeniedPath = new PathString("/Home");
                options.Scope.Add("offline_access");
                options.SaveTokens =
                    true; // this causes the framework code to store access and refresh tokens as part of the auth cookie, making things just easier for us
                options.ClaimActions.Add(new MapAllClaimsAction());
                options.Events.OnAuthorizationCodeReceived = async context =>
                {
                    // Microsoft-specific kind of nonsense for resolving a code to an access token
                    var code = context.ProtocolMessage.Code;
                    var request = context.HttpContext.Request;
                    string currentUri = UriHelper.BuildAbsolute(
                        request.Scheme,
                        request.Host,
                        request.PathBase,
                        options.CallbackPath);

                    IConfidentialClientApplication cca = ConfidentialClientApplicationBuilder.Create(options.ClientId)
                        .WithB2CAuthority(options.Authority)
                        .WithRedirectUri(currentUri)
                        .WithClientSecret(options.ClientSecret)
                        .Build();

                    try
                    {
                        AuthenticationResult result =
                            await cca.AcquireTokenByAuthorizationCode(options.Scope, code).ExecuteAsync();
                        context.HandleCodeRedemption(result.AccessToken, result.IdToken);
                    }
                    catch (Exception ex)
                    {
                        //TODO: Handle
                        throw;
                    }
                };

                options.Events.OnRedirectToIdentityProvider = async context =>
                {
                    if (context.Request.Query.TryGetValue("token", out var inviteToken))
                    {
                        context.ProtocolMessage.State= inviteToken;
                    }
                    await Task.CompletedTask.ConfigureAwait(false);
                };
                
                options.Events.OnTokenValidated = async context =>
                {
                    var principal = context.Principal;
                    var userSignIn = context.HttpContext.RequestServices.GetService<ISignInInternalUser>();
                    if (principal != null && userSignIn != null)
                    {
                        await userSignIn.HandleUserLoginAsync(principal, context.ProtocolMessage.State);
                    }
                };
            });

        return services;
    }

    public static IServiceCollection AddAzureAdServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddTransient<IAzureAdService, AzureAdService>();

        var azureAdConfigSection = configuration.GetSection("AzureAd");
        services.Configure<AzureAdServiceConfiguration>(azureAdConfigSection);

        return services;
    }

    public static IServiceCollection AddFloServices(this IServiceCollection services, IConfiguration configuration)
    {
        var blockSize = 1024;
        var largeBufferMultiple = 1024 * 1024;
        var maximumBufferSize = 16 * largeBufferMultiple;
        var maximumFreeLargePoolBytes = maximumBufferSize * 4;
        var maximumFreeSmallPoolBytes = 250 * blockSize;

        var options = new RecyclableMemoryStreamManager.Options
        {
            BlockSize = blockSize,
            LargeBufferMultiple = largeBufferMultiple,
            MaximumBufferSize = maximumBufferSize,
            MaximumLargePoolFreeBytes = maximumFreeLargePoolBytes,
            MaximumSmallPoolFreeBytes = maximumFreeSmallPoolBytes,
            GenerateCallStacks = false, // Set to true if needed for debugging.
            AggressiveBufferReturn = true
        };

        services.AddSingleton(new RecyclableMemoryStreamManager(options));


        services.AddSingleton<ValidationProvider>();
        services.AddValidatorsFromAssemblyContaining<ValidationProvider>();
        services.AddTransient<AppVersionService>();
        services.AddTransient<IUserAccountService, UserAccountService>();

        //Site analytics:
        //Note, the service is registered as singleton - despite accessing HTTP Context for requests to check for cookies,
        //as IHttpContextAccessor.HttpContext is scoped to the current request, and safe to be used in singleton services. 
        services.AddOptions<SiteAnalyticsSettings>().BindConfiguration(SiteAnalyticsSettings.ConfigurationKey);
        services.AddSingleton<SiteSiteAnalyticsService>();

        services.AddScoped<BackLinkService>();

        RegisterUseCases(services, configuration);

        return services;
    }

    public static IServiceCollection AddDomainServices(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> dbContextOptions,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        services.AddCommonServices(dbContextOptions);
        services.AddInternalUsersServices(dbContextOptions);
        services.AddApplicantServices(dbContextOptions);
        services.AddFellingLicenceApplicationServices(configuration, dbContextOptions);
        services.AddFileStorageServices(configuration, environment);
        services.AddAdminHubServices(dbContextOptions);
        services.AddPropertyProfilesServices(dbContextOptions);
        services.AddNotificationsServices(configuration, dbContextOptions);
        services.AddGisServices(configuration);
        services.AddConditionsBuilderServices(configuration, dbContextOptions);

        services.AddFakeServices(configuration);

        return services;
    }

    public static IServiceCollection RegisterMassTransit(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<RabbitMqOptions>().BindConfiguration("RabbitMqOptions");

        var options = new RabbitMqOptions();
        configuration.GetSection("RabbitMqOptions").Bind(options);

        services.AddMassTransit(x =>
        {
            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(options.Url, h =>
                {
                    h.Username(options.Username);
                    h.Password(options.Password);
                });
            });
        });

        return services;
    }

    private static void RegisterUseCases(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<UserInviteOptions>(configuration.GetSection("UserInvite"));
        services.Configure<UserFileUploadOptions>(configuration.GetSection("UserFileUpload"));
        services.Configure<ApiFileUploadOptions>(configuration.GetSection("ApiFileUpload"));
        services.Configure<ApiSecurityOptions>(configuration.GetSection("ApiSecurityOptions"));
        services.Configure<ExternalApplicantSiteOptions>(configuration.GetSection("ExternalApplicantSite"));
        services.Configure<DocumentVisibilityOptions>(configuration.GetSection("DocumentVisibilities"));
        services.Configure<ApplicationExtensionOptions>(configuration.GetSection("ApplicationExtension"));
        services.Configure<PublicRegisterExpiryOptions>(configuration.GetSection("PublicRegisterExpiry"));
        services.Configure<VoluntaryWithdrawalNotificationOptions>(configuration.GetSection("VoluntaryWithdrawApplication"));
        services.Configure<LarchOptions>(configuration.GetSection("LarchOptions"));

        services.AddScoped<RegisterUserAccountUseCase>();
        services.AddScoped<ExternalConsulteeInviteUseCase>();
        services.AddScoped<ExternalConsulteeReviewUseCase>();
        services.AddScoped<FellingLicenceApplicationUseCase>();
        services.AddScoped<AssignToUserUseCase>();
        services.AddScoped<AssignToApplicantUseCase>();
        services.AddScoped<WoodlandOfficerReviewUseCase>();
        services.AddScoped<PublicRegisterUseCase>();
        services.AddScoped<SiteVisitUseCase>();
        services.AddScoped<Pw14UseCase>();
        services.AddScoped<AdminOfficerReviewUseCase>();
        services.AddScoped<AddDocumentFromExternalSystemUseCase>();
        services.AddScoped<ManageAdminHubUseCase>();
        services.AddScoped<ConfirmedFellingAndRestockingDetailsUseCase>();
        services.AddScoped<GetSupportingDocumentUseCase>();
        services.AddScoped<RunFcInternalUserConstraintCheckUseCase>();
        services.AddScoped<AddSupportingDocumentsUseCase>();
        services.AddScoped<RemoveSupportingDocumentUseCase>();
        services.AddScoped<ConditionsUseCase>();
        services.AddScoped<ExtendApplicationsUseCase>();
        services.AddScoped<ApproveRefuseOrReferApplicationUseCase>();
        services.AddScoped<ReturnApplicationUseCase>();
        services.AddScoped<PublicRegisterExpiryUseCase>();
        services.AddScoped<GetFcStaffMembersUseCase>();
        services.AddScoped<CloseFcStaffAccountUseCase>();
        services.AddScoped<GetApplicantUsersUseCase>();
        services.AddScoped<AmendExternalUserUseCase>();
        services.AddScoped<VoluntaryWithdrawalNotificationUseCase>();
        services.AddScoped<AutomaticWithdrawalNotificationUseCase>();
        services.AddScoped<GenerateReportUseCase>();
        services.AddScoped<GeneratePdfApplicationUseCase>();
        services.AddScoped<ViewAgentAuthorityFormUseCase>();
        services.AddScoped<AgentAuthorityFormCheckUseCase>();
        services.AddScoped<MappingCheckUseCase>();
        services.AddScoped<LarchCheckUseCase>();
        services.AddScoped<CBWCheckUseCase>();
        services.AddScoped<ConstraintsCheckUseCase>();
        services.AddScoped<ApproverReviewUseCase>();
        services.AddScoped<RevertApplicationFromWithdrawnUseCase>();
    }
}