using FluentValidation;
using Forestry.Flo.External.Web.Services;
using Forestry.Flo.External.Web.Services.AccountAdministration;
using Forestry.Flo.External.Web.Services.ExternalApi;
using Forestry.Flo.External.Web.Services.MassTransit.Consumers;
using Forestry.Flo.Services.AdminHubs;
using Forestry.Flo.Services.Applicants;
using Forestry.Flo.Services.Applicants.Configuration;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Infrastructure;
using Forestry.Flo.Services.DataImport;
using Forestry.Flo.Services.FellingLicenceApplications;
using Forestry.Flo.Services.FileStorage;
using Forestry.Flo.Services.FileStorage.Configuration;
using Forestry.Flo.Services.Notifications;
using Forestry.Flo.Services.PropertyProfiles;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth.Claims;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Forestry.Flo.Services.Gis;
using Forestry.Flo.Services.InternalUsers;
using MassTransit;
using Forestry.Flo.External.Web.Services.AgentAuthority;
using Forestry.Flo.External.Web.Services.FcUser;
using Forestry.Flo.Services.Common.Analytics;
using Forestry.Flo.Services.ConditionsBuilder;
using Forestry.Flo.Services.FellingLicenceApplications.Configuration;
using Forestry.Flo.Services.Gis.Infrastructure;
using Forestry.Flo.Services.Gis.Interfaces;
using Forestry.Flo.Services.Gis.Services;
using GovUk.OneLogin.AspNetCore;

namespace Forestry.Flo.External.Web.Infrastructure;

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
            .AddAuthorization(options =>  
            {  
                options.AddPolicy(AuthorizationPolicyConstants.FcUserPolicyName, policy =>
                    policy.RequireClaim(FloClaimTypes.FcUser, "true"));  
            })  
            .AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = FloAuthenticationScheme.SignIn;
            })
            .AddCookie(options =>
            {
                options.EventsType = typeof(CustomCookieAuthenticationEvents);
            })
            .AddOpenIdConnect(FloAuthenticationScheme.SignUp, options =>
            {
                options.Authority = settings.SignUpAuthority;
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
                options.CallbackPath = "/signup-oidc";

                options.Events.OnRedirectToIdentityProvider = async context =>
                {
                    if (context.Request.Query.TryGetValue("token", out var inviteToken))
                    {
                        context.ProtocolMessage.State = inviteToken;
                    }
                    await Task.CompletedTask.ConfigureAwait(false);
                };

                options.Events.OnTokenValidated = async context =>
                {
                    var principal = context.Principal;
                    var userSignIn = context.HttpContext.RequestServices.GetService<ISignInApplicant>();

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
                    catch(Exception)
                    {
                        context.Response.Redirect("/Home/Error");
                        context.HandleResponse();
                    }
                };

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

                    var result = await cca
                        .AcquireTokenByAuthorizationCode(options.Scope, code)
                        .ExecuteAsync();

                    context.HandleCodeRedemption(result.AccessToken, result.IdToken);
                };
            })
            .AddOpenIdConnect(FloAuthenticationScheme.SignIn, options =>
            {
                options.Authority = settings.SignInAuthority;
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
                options.CallbackPath = "/signin-oidc";

                options.Events.OnRedirectToIdentityProvider = async context =>
                {
                    if (context.Request.Query.TryGetValue("token", out var inviteToken))
                    {
                        context.ProtocolMessage.State = inviteToken;
                    }
                    await Task.CompletedTask.ConfigureAwait(false);
                };

                options.Events.OnTokenValidated = async context =>
                {
                    var principal = context.Principal;
                    var userSignIn = context.HttpContext.RequestServices.GetService<ISignInApplicant>();

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

                    var result = await cca
                        .AcquireTokenByAuthorizationCode(options.Scope, code)
                        .ExecuteAsync();

                    context.HandleCodeRedemption(result.AccessToken, result.IdToken);
                };
            });

        services.AddScoped<CustomCookieAuthenticationEvents>();

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
            .AddAuthorization(options =>
            {
                options.AddPolicy(AuthorizationPolicyConstants.FcUserPolicyName, policy =>
                    policy.RequireClaim(FloClaimTypes.FcUser, "true"));
            })
            .AddAuthentication(options =>
            {
                options.DefaultScheme = "Cookies";
                options.DefaultChallengeScheme = OneLoginDefaults.AuthenticationScheme;
            })
            .AddCookie(options =>
            {
                options.EventsType = typeof(CustomCookieAuthenticationEvents);
            })
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
                    var userSignIn = context.HttpContext.RequestServices.GetService<ISignInApplicant>();

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

        services.AddScoped<CustomCookieAuthenticationEvents>();

        return services;
    }

    public static IServiceCollection AddFloServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<ValidationProvider>();
        services.AddValidatorsFromAssemblyContaining<ValidationProvider>();
        services.AddTransient<AppVersionService>();
        services.AddScoped<BackLinkService>();

        //Site analytics:
        //Note, the service is registered as singleton - despite accessing HTTP Context for requests to check for cookies,
        //as IHttpContextAccessor.HttpContext is scoped to the current request, and safe to be used in singleton services. 
        services.AddOptions<SiteAnalyticsSettings>().BindConfiguration(SiteAnalyticsSettings.ConfigurationKey);
        services.AddSingleton<SiteSiteAnalyticsService>(); 
        
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
        services.AddApplicantServices(dbContextOptions);
        services.AddInternalUsersServices(dbContextOptions);
        services.AddPropertyProfilesServices(dbContextOptions);
        services.AddFellingLicenceApplicationServices(configuration, dbContextOptions);
        services.AddNotificationsServices(configuration, dbContextOptions);
        services.AddGisServices(configuration);
        services.AddAdminHubServices(dbContextOptions);
        services.AddDataImportServices();
        services.AddFileStorageServices(configuration, environment);
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
            x.AddConsumer<GeneratePdfPreviewConsumer>();
            x.AddConsumer<CentrePointCalculationConsumer>();
            x.AddConsumer<InternalFcUserAccountApprovedEventConsumer>();
            x.AddConsumer<AssignWoodlandOfficerConsumer>();
            x.AddConsumer<GetLarchRiskZonesConsumer>();

            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(options.Url, h =>
                {
                    h.Username(options.Username);
                    h.Password(options.Password);
                });

                ReceiveEndpoint<GeneratePdfPreviewConsumer>("GeneratePdfPreview", cfg, context, options);
                ReceiveEndpoint<CentrePointCalculationConsumer>("CentrePointCalculation", cfg, context, options);
                ReceiveEndpoint<InternalFcUserAccountApprovedEventConsumer>("InternalFcUserAccountApprovedEvent", cfg, context, options);
                ReceiveEndpoint<AssignWoodlandOfficerConsumer>("AssignWoodlandOfficer", cfg, context, options);
                ReceiveEndpoint<GetLarchRiskZonesConsumer>("GetLarchRiskZones", cfg, context, options);
            });
        });

        return services;
    }

    public static IServiceCollection AddFakeServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var woReviewOptions = configuration.GetSection("DevelopmentConfigOptions").Get<DevelopmentConfigOptions>();
        if (woReviewOptions.UseDevPublicRegister)
        {
            services.RemoveService<IPublicRegister>();
            services.AddScoped<IPublicRegister, DevelopmentPublicRegister>();
        }
        else
        {
            services.AddScoped<IPublicRegister, PublicRegister>();
        }

        return services;
    }

    /// <summary>
    /// Removes all registered registrations of <see cref="TService"/> from an <see cref="IServiceCollection"/>.
    /// </summary>
    /// <typeparam name="TService">The type of service interface which needs to be removed.</typeparam>
    /// <param name="services"></param>
    private static void RemoveService<TService>(this IServiceCollection services)
    {
        if (services.Any(x => x.ServiceType == typeof(TService)))
        {
            var serviceDescriptors = services.Where(x => x.ServiceType == typeof(TService)).ToList();
            foreach (var serviceDescriptor in serviceDescriptors)
            {
                services.Remove(serviceDescriptor);
            }
        }
    }

    private static void ReceiveEndpoint<T>(
        string queueName,
        IRabbitMqBusFactoryConfigurator cfg,
        IRegistrationContext context,
        RabbitMqOptions options) where T : class, IConsumer
    {
        cfg.ReceiveEndpoint(queueName, r =>
        {
            r.QueueExpiration = TimeSpan.FromSeconds(options.QueueExpiration);
            r.SetQuorumQueue();
            r.SetQueueArgument("declare", "lazy");
            r.ConfigureConsumer<T>(context);
            r.PrefetchCount = options.PrefetchCount;
            r.UseMessageRetry(i =>
            {
                i.Interval(options.RetryCount, options.RetryIntervalMilliseconds);
            });
        });
    }

    private static void RegisterUseCases(IServiceCollection services, IConfiguration configuration)
    {
        var userInviteConfigSection = configuration.GetSection("UserInvite");
        services.Configure<UserInviteOptions>(userInviteConfigSection);
        services.Configure<UserFileUploadOptions>(configuration.GetSection("UserFileUploadSettings"));
        services.Configure<ApiFileUploadOptions>(configuration.GetSection("ApiFileUploadSettings"));
        services.Configure<SecurityOptions>(configuration.GetSection("Security"));
        services.Configure<DocumentVisibilityOptions>(configuration.GetSection("DocumentVisibilities"));
        services.Configure<FcAgencyOptions>(configuration.GetSection("FcAgency"));
        services.AddOptions<EiaOptions>().BindConfiguration(EiaOptions.ConfigurationKey);
        services.AddOptions<InternalUserSiteOptions>().BindConfiguration(InternalUserSiteOptions.ConfigurationKey);

        services.AddScoped<RegisterUserAccountUseCase>();
        services.AddScoped<UploadShapeFileUseCase>();
        services.AddScoped<WoodlandOwnerHomePageUseCase>();
        services.AddScoped<ManagePropertyProfileUseCase>();
        services.AddScoped<ManageGeographicCompartmentUseCase>();
        services.AddScoped<InviteWoodlandOwnerToOrganisationUseCase>();
        services.AddScoped<InviteAgentToOrganisationUseCase>();
        services.AddScoped<CreateFellingLicenceApplicationUseCase>();
        services.AddScoped<AddSupportingDocumentsUseCase>();
        services.AddScoped<RemoveSupportingDocumentUseCase>();
        services.AddScoped<AgentUserHomePageUseCase>();
        services.AddScoped<AddDocumentFromExternalSystemUseCase>();
        services.AddScoped<RunApplicantConstraintCheckUseCase>();
        services.AddScoped<GetSupportingDocumentUseCase>();
        services.AddScoped<AgentAuthorityFormUseCase>();
        services.AddScoped<GetAgentAuthorityFormDocumentsUseCase>();
        services.AddScoped<RemoveAgentAuthorityFormDocumentUseCase>();
        services.AddScoped<DownloadAgentAuthorityFormDocumentUseCase>();
        services.AddScoped<AddAgentAuthorityFormDocumentFilesUseCase>();
        services.AddScoped<ListWoodlandOwnerUsersUseCase>();
        services.AddScoped<FcAgentCreatesWoodlandOwnerUseCase>();
        services.AddScoped<GeneratePdfApplicationUseCase>();
        services.AddScoped<DataImportUseCase>();
        services.AddScoped<ManageWoodlandOwnerDetailsUseCase>();
        services.AddScoped<AmendExternalUserUseCase>();
        services.AddScoped<ValidateShapeUseCase>();
        services.AddScoped<CalculateCentrePointUseCase>();
        services.AddScoped<CreateExternalUserProfileForInternalFcUserUseCase>();
        services.AddScoped<GetDataForFcUserHomepageUseCase>();
        services.AddScoped<FcUserCreateAgencyUseCase>();
        services.AddScoped<AssignWoodlandOfficerAsyncUseCase>();
        services.AddScoped<EnvironmentalImpactAssessmentUseCase>();
        services.AddScoped<ReviewFellingAndRestockingAmendmentsUseCase>();
        services.AddScoped<TenYearLicenceUseCase>();
    }
}
