using Ardalis.GuardClauses;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Infrastructure;
using Forestry.Flo.Services.Common.Services;
using Forestry.Flo.Services.FellingLicenceApplications.Configuration;
using Forestry.Flo.Services.FellingLicenceApplications.DataImports;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.FellingLicenceApplications.Services.WoodlandOfficerReviewSubstatuses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Forestry.Flo.Services.FellingLicenceApplications;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds all services to the provided <see cref="ServiceCollection"/> made available for the applicants service.
    /// </summary>
    /// <param name="services">The collection of services to register against.</param>
    /// <param name="configuration">An <see cref="IConfiguration"/> representing the application configuration settings.</param>
    /// <param name="options">A callback for configuration of the EF database context.</param>
    public static IServiceCollection AddFellingLicenceApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<DbContextOptionsBuilder> options)
    {
        Guard.Against.Null(services);
        Guard.Against.Null(options);

        services.AddOptions<FellingLicenceApplicationOptions>()
            .BindConfiguration(FellingLicenceApplicationOptions.ConfigurationKey);

        services.AddDbContextFactory<FellingLicenceApplicationsContext>(options);
        services.AddSingleton<IDbContextFactorySource<FellingLicenceApplicationsContext>, CustomDbContextFactorySource<FellingLicenceApplicationsContext>>();
        services.AddSingleton<IApplicationReferenceHelper, ApplicationReferenceHelper>();
        services.AddScoped<IAmendCaseNotes, AmendCaseNotes>();
        services.AddScoped<IFellingLicenceApplicationExternalRepository, ExternalUserContextFlaRepository>();
        services.AddScoped<IFellingLicenceApplicationInternalRepository, InternalUserContextFlaRepository>();
        services.AddScoped<IFellingLicenceApplicationReferenceRepository, FellingLicenceApplicationReferenceRepository>();
        services.AddScoped<IViewCaseNotesService, ViewCaseNotesService>();
        services.AddScoped<IRemoveDocumentService, RemoveDocumentService>();
        services.AddScoped<IAddDocumentService, AddDocumentService>();
        services.AddScoped<IGetDocumentServiceInternal, InternalGetDocumentService>();
        services.AddScoped<IGetDocumentServiceExternal, ExternalGetDocumentService>();
        services.AddScoped<ConstraintCheckerService>();
        services.AddScoped<IGetWoodlandOfficerReviewService, GetWoodlandOfficerReviewService>();
        services.AddScoped<IApproverReviewService, ApproverReviewService>();
        services.AddScoped<IApprovedInErrorService, ApprovedInErrorService>();
        services.AddScoped<IExternalConsulteeReviewService, ExternalConsulteeReviewService>();
        services.AddScoped<IUpdateConfirmedFellingAndRestockingDetailsService, UpdateConfirmedFellingAndRestockingDetailsService>();
        services.Configure<WoodlandOfficerReviewOptions>(configuration.GetSection("WoodlandOfficerReview"));
        services.AddScoped<IUpdateWoodlandOfficerReviewService, UpdateWoodlandOfficerReviewService>();
        services.AddScoped<IActivityFeedService, ActivityFeedCaseNotesService>();
        services.AddScoped<IActivityFeedService, ActivityFeedAssigneeHistoryService>();
        services.AddScoped<IActivityFeedService, ActivityFeedStatusHistoryService>();
        services.AddScoped<IActivityFeedService, ActivityFeedConsulteeCommentService>();
        services.AddScoped<IActivityFeedService, ActivityFeedAmendmentReviewService>();
        services.AddScoped<IUpdateAdminOfficerReviewService, UpdateAdminOfficerReviewService>();
        services.AddScoped<IGetAdminOfficerReview, GetAdminOfficerReviewService>();
        services.AddScoped<ISubmitFellingLicenceService, SubmitFellingLicenceService>();
        services.AddScoped<IWithdrawFellingLicenceService, WithdrawFellingLicenceService>();
        services.AddScoped<IDeleteFellingLicenceService, DeleteFellingLicenceService>();
        services.AddScoped<IExtendApplications, ExtendApplicationsService>();
        services.AddScoped<IVoluntaryWithdrawalNotificationService, VoluntaryWithdrawalNotificationService>();
        services.AddScoped<IWithdrawalNotificationService, WithdrawalNotificationService>();
        services.AddScoped<ILateAmendmentResponseWithdrawalService, LateAmendmentResponseWithdrawalService>();
        services.AddScoped<IGetFellingLicenceApplicationForInternalUsers, GetFellingLicenceApplicationForInternalUsersService>();
        services.AddScoped<IGetFellingLicenceApplicationForExternalUsers, GetFellingLicenceApplicationForExternalUsersService>();
        services.AddScoped<IUpdateFellingLicenceApplication, UpdateFellingLicenceApplicationService>();
        services.Configure<PDFGeneratorAPIOptions>(configuration.GetSection("PDFGeneratorAPI"));
        services.AddScoped<ICreateApplicationSnapshotDocumentService, CreateApplicationSnapshotDocumentService>();
        services.AddScoped<IUpdateCentrePoint, UpdateCentrePointService>();
        services.AddScoped<IUpdateApplicationFromForesterLayers, UpdateApplicationFromForesterLayersService>();
        services.AddScoped<IHabitatRestorationService, HabitatRestorationService>();

        services.AddScoped<IImportApplications, ImportApplicationsService>();
        services.AddScoped<IReportQueryService, ReportQueryService>();
        services.AddScoped<FlaStatusDurationCalculator>();
        services.AddScoped<IGetConfiguredFcAreas, GetConfiguredFcAreasService>();
        services.AddScoped<IUpdateFellingLicenceApplicationForExternalUsers, UpdateFellingLicenceApplicationForExternalUsersService>();
        services.AddScoped<ILarchCheckService, LarchCheckService>();

        services.AddScoped<ISubStatusSpecification, AmendmentsWithApplicantSpecification>();
        services.AddScoped<ISubStatusSpecification, OnPublicRegisterSpecification>();
        services.AddScoped<ISubStatusSpecification, ConsultationSpecification>();
        services.AddScoped<IWoodlandOfficerReviewSubStatusService, WoodlandOfficerReviewSubStatusService>();

        return services;
    }
}