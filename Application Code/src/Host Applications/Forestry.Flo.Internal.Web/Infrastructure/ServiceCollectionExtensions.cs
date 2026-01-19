using FluentValidation;
using Forestry.Flo.Internal.Web.Infrastructure.Fakes;
using Forestry.Flo.Internal.Web.Services;
using Forestry.Flo.Internal.Web.Services.AccountAdministration;
using Forestry.Flo.Internal.Web.Services.AdminHub;
using Forestry.Flo.Internal.Web.Services.ExternalConsulteeReview;
using Forestry.Flo.Internal.Web.Services.FellingLicenceApplication;
using Forestry.Flo.Internal.Web.Services.FellingLicenceApplication.AdminOfficerReview;
using Forestry.Flo.Internal.Web.Services.FellingLicenceApplication.ApproverReview;
using Forestry.Flo.Internal.Web.Services.FellingLicenceApplication.Api;
using Forestry.Flo.Internal.Web.Services.FellingLicenceApplication.WoodlandOfficerReview;
using Forestry.Flo.Internal.Web.Services.Interfaces;
using Forestry.Flo.Internal.Web.Services.MassTransit.Consumers;
using Forestry.Flo.Internal.Web.Services.Reports;
using Forestry.Flo.Internal.Web.Services.Validation;
using Forestry.Flo.Services.AdminHubs;
using Forestry.Flo.Services.Applicants;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Analytics;
using Forestry.Flo.Services.Common.Infrastructure;
using Forestry.Flo.Services.ConditionsBuilder;
using Forestry.Flo.Services.FellingLicenceApplications;
using Forestry.Flo.Services.FileStorage;
using Forestry.Flo.Services.FileStorage.Configuration;
using Forestry.Flo.Services.Gis;
using Forestry.Flo.Services.InternalUsers;
using Forestry.Flo.Services.InternalUsers.Services;
using Forestry.Flo.Services.Notifications;
using Forestry.Flo.Services.PropertyProfiles;
using GovUk.OneLogin.AspNetCore;
using MassTransit;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth.Claims;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IO;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc;

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
                        context.Response.Redirect("/Home/Error");
                        context.HandleResponse();
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

                    var newIdentity = new System.Security.Claims.ClaimsIdentity([
                        new System.Security.Claims.Claim(FloClaimTypes.AuthenticationProvider, nameof(AuthenticationProvider.Azure))
                    ]);

                    context.Principal!.AddIdentity(newIdentity);

                    try
                    {
                        if (principal != null && userSignIn != null)
                        {
                            await userSignIn.HandleUserLoginAsync(principal, context.ProtocolMessage.State);
                        }
                    }
                    catch (Exception)
                    {
                        context.Response.Redirect("/Home/Error");
                        context.HandleResponse();
                    }
                };
            });

        return services;
    }

    public static IServiceCollection AddOneLoginServices(this IServiceCollection services, IConfiguration configuration)
    {
        var settings = new AzureAdB2COptions();
        configuration.Bind("AzureAdB2C", settings);

        services
            .AddHttpClient()
            .ConfigureHttpClientDefaults(defaults =>
            {
                defaults.AddHttpMessageHandler(_ => new UserAgentHandler());
            })
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
                options.DefaultScheme = "Cookies";
                options.DefaultChallengeScheme = OneLoginDefaults.AuthenticationScheme;
            })
            .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddOneLogin(options =>
            {
                var govUkOptions = configuration.GetSection(GovUkOneLoginOptions.ConfigurationKey).Get<GovUkOneLoginOptions>();
                if (govUkOptions is null)
                {
                    throw new InvalidOperationException("GovUkOneLoginOptions configuration section is missing or invalid.");
                }

                options.InitialiseGovUkOneLogin(govUkOptions);

                options.Events.OnTokenValidated = context =>
                {
                    var token = context.ProtocolMessage.State;

                    var newIdentity = new System.Security.Claims.ClaimsIdentity([
                        new System.Security.Claims.Claim(FloClaimTypes.AuthenticationProvider, nameof(AuthenticationProvider.OneLogin))
                    ]);

                    if (!string.IsNullOrEmpty(token))
                    {
                        newIdentity.AddClaim(new System.Security.Claims.Claim("token", context.ProtocolMessage.State));
                    }

                    context.Principal!.AddIdentity(newIdentity);
                    return Task.CompletedTask;
                };

                options.Events.OnTicketReceived = async context =>
                {
                    var principal = context.Principal;
                    var userSignIn = context.HttpContext.RequestServices.GetService<ISignInInternalUser>();

                    var token = context.Principal?.Claims.FirstOrDefault(x => x.Type == "token")?.Value;

                    try
                    {
                        if (principal != null && userSignIn != null)
                        {
                            await userSignIn.HandleUserLoginAsync(principal, token);
                        }
                    }
                    catch (Exception)
                    {
                        context.Response.Redirect("/Home/Error");
                        context.HandleResponse();
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

        services.AddSingleton<IValidationProvider, ValidationProvider>();
        services.AddValidatorsFromAssemblyContaining<ValidationProvider>(filter: s => 
            s.ValidatorType != typeof(ConfirmedFellingOperationCrossValidator)
            && s.ValidatorType != typeof(ConfirmedRestockingOperationCrossValidator));
        services.AddTransient<AppVersionService>();
        services.AddTransient<IUserAccountService, UserAccountService>();

        services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();
        services.AddScoped<IUrlHelper>(x => {
            var actionContext = x.GetRequiredService<IActionContextAccessor>().ActionContext;
            var factory = x.GetRequiredService<IUrlHelperFactory>();
            return factory.GetUrlHelper(actionContext!);
        });

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
            x.AddConsumer<GenerateSubmittedPdfPreviewConsumer>();

            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(options.Url, h =>
                {
                    h.Username(options.Username);
                    h.Password(options.Password);
                });

                ReceiveEndpoint<GenerateSubmittedPdfPreviewConsumer>(
                    GenerateSubmittedPdfPreviewConsumer.QueueName, 
                    cfg,
                    context);
            });
        });

        return services;
    }

    private static void ReceiveEndpoint<T>(
        string queueName,
        IRabbitMqBusFactoryConfigurator cfg,
        IRegistrationContext context) where T : class, IConsumer
    {
        cfg.ReceiveEndpoint(queueName, r =>
        {
            r.SetQuorumQueue();
            r.SetQueueArgument("declare", "lazy");
            r.ConfigureConsumer<T>(context);
        });
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
        services.AddOptions<EiaOptions>().BindConfiguration(EiaOptions.ConfigurationKey);
        services.AddOptions<ReviewAmendmentsOptions>().BindConfiguration(ReviewAmendmentsOptions.ConfigurationKey);

        services.AddScoped<IRegisterUserAccountUseCase, RegisterUserAccountUseCase>();
        services.AddScoped<IExternalConsulteeInviteUseCase, ExternalConsulteeInviteUseCase>();
        services.AddScoped<IExternalConsulteeReviewUseCase, ExternalConsulteeReviewUseCase>();
        services.AddScoped<IFellingLicenceApplicationUseCase, FellingLicenceApplicationUseCase>();
        services.AddScoped<IAssignToUserUseCase, AssignToUserUseCase>();
        services.AddScoped<IAssignToApplicantUseCase, AssignToApplicantUseCase>();
        services.AddScoped<IWoodlandOfficerReviewUseCase, WoodlandOfficerReviewUseCase>();
        services.AddScoped<IPublicRegisterUseCase, PublicRegisterUseCase>();
        services.AddScoped<ISiteVisitUseCase, SiteVisitUseCase>();
        services.AddScoped<IPw14UseCase, Pw14UseCase>();
        services.AddScoped<IDesignationsUseCase, DesignationsUseCase>();
        services.AddScoped<IAdminOfficerReviewUseCase, AdminOfficerReviewUseCase>();
        services.AddScoped<IAddDocumentFromExternalSystemUseCase, AddDocumentFromExternalSystemUseCase>();
        services.AddScoped<IManageAdminHubUseCase, ManageAdminHubUseCase>();
        services.AddScoped<IConfirmedFellingAndRestockingDetailsUseCase, ConfirmedFellingAndRestockingDetailsUseCase>();
        services.AddScoped<IGetSupportingDocumentUseCase, GetSupportingDocumentUseCase>();
        services.AddScoped<IRunFcInternalUserConstraintCheckUseCase, RunFcInternalUserConstraintCheckUseCase>();
        services.AddScoped<IAddSupportingDocumentsUseCase, AddSupportingDocumentsUseCase>();
        services.AddScoped<IRemoveSupportingDocumentUseCase, RemoveSupportingDocumentUseCase>();
        services.AddScoped<IConditionsUseCase, ConditionsUseCase>();
        services.AddScoped<IExtendApplicationsUseCase, ExtendApplicationsUseCase>();
        services.AddScoped<IApproveRefuseOrReferApplicationUseCase, ApproveRefuseOrReferApplicationUseCase>();
        services.AddScoped<IReturnApplicationUseCase, ReturnApplicationUseCase>();
        services.AddScoped<IPublicRegisterExpiryUseCase, PublicRegisterExpiryUseCase>();
        services.AddScoped<IPublicRegisterCommentsUseCase, PublicRegisterCommentsUseCase>();
        services.AddScoped<IGetFcStaffMembersUseCase, GetFcStaffMembersUseCase>();
        services.AddScoped<ICloseFcStaffAccountUseCase, CloseFcStaffAccountUseCase>();
        services.AddScoped<IGetApplicantUsersUseCase, GetApplicantUsersUseCase>();
        services.AddScoped<IAmendExternalUserUseCase, AmendExternalUserUseCase>();
        services.AddScoped<IVoluntaryWithdrawalNotificationUseCase, VoluntaryWithdrawalNotificationUseCase>();
        services.AddScoped<IAutomaticWithdrawalNotificationUseCase, AutomaticWithdrawalNotificationUseCase>();
        services.AddScoped<IGenerateReportUseCase, GenerateReportUseCase>();
        services.AddScoped<IGeneratePdfApplicationUseCase, GeneratePdfApplicationUseCase>();
        services.AddScoped<IViewAgentAuthorityFormUseCase, ViewAgentAuthorityFormUseCase>();
        services.AddScoped<IAgentAuthorityFormCheckUseCase, AgentAuthorityFormCheckUseCase>();
        services.AddScoped<IMappingCheckUseCase, MappingCheckUseCase>();
        services.AddScoped<ILarchCheckUseCase, LarchCheckUseCase>();
        services.AddScoped<ICBWCheckUseCase, CBWCheckUseCase>();
        services.AddScoped<IConstraintsCheckUseCase, ConstraintsCheckUseCase>();
        services.AddScoped<IApproverReviewUseCase, ApproverReviewUseCase>();
        services.AddScoped<IApprovedInErrorUseCase, ApprovedInErrorUseCase>();
        services.AddScoped<IRevertApplicationFromWithdrawnUseCase, RevertApplicationFromWithdrawnUseCase>();
        services.AddScoped<IEnvironmentalImpactAssessmentAdminOfficerUseCase, EnvironmentalImpactAssessmentAdminOfficerUseCase>();
        services.AddScoped<ILateAmendmentResponseWithdrawalUseCase, LateAmendmentResponseWithdrawalUseCase>();
        services.AddScoped<IAdminOfficerTreeHealthCheckUseCase, AdminOfficerTreeHealthCheckUseCase>();
        services.AddScoped<IWoodlandOfficerTreeHealthCheckUseCase, WoodlandOfficerTreeHealthCheckUseCase>();
        services.AddScoped<PriorityOpenHabitatUseCase>();
    }
}