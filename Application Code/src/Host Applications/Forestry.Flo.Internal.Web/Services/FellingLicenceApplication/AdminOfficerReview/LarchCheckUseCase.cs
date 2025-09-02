using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Infrastructure;
using Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.Common.Services;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.InternalUsers.Services;
using Microsoft.Extensions.Options;
using System;
using Forestry.Flo.Services.Common.User;
using Result = CSharpFunctionalExtensions.Result;

namespace Forestry.Flo.Internal.Web.Services.FellingLicenceApplication.AdminOfficerReview;

public class LarchCheckUseCase(
    IUserAccountService internalUserAccountService,
    IRetrieveUserAccountsService externalUserAccountService,
    ILogger<LarchCheckUseCase> logger,
    IFellingLicenceApplicationInternalRepository fellingLicenceApplicationInternalRepository,
    IRetrieveWoodlandOwners woodlandOwnerService,
    IUpdateAdminOfficerReviewService updateAdminOfficerReviewService,
    IGetFellingLicenceApplicationForInternalUsers getFellingLicenceApplication,
    IAgentAuthorityService agentAuthorityService,
    IAuditService<AdminOfficerReviewUseCaseBase> auditService,
    ILarchCheckService larchCheckService,
    IActivityFeedItemProvider activityFeedItemProvider,
    IOptions<LarchOptions> larchOptions,
    IGetConfiguredFcAreas getConfiguredFcAreasService,
    RequestContext requestContext) : AdminOfficerReviewUseCaseBase(
        internalUserAccountService,
        externalUserAccountService,
        logger,
        fellingLicenceApplicationInternalRepository,
        woodlandOwnerService,
        updateAdminOfficerReviewService,
        getFellingLicenceApplication,
        auditService,
        agentAuthorityService,
        getConfiguredFcAreasService,
        requestContext)
{
    private readonly ILarchCheckService _larchCheckService = larchCheckService;
    private readonly IActivityFeedItemProvider _activityFeedItemProvider = activityFeedItemProvider;
    private readonly LarchOptions _larchOptions = larchOptions.Value;

    public async Task<Result<LarchCheckModel>> GetLarchCheckModelAsync(
        Guid applicationId,
        InternalUser user,
        CancellationToken cancellationToken)
    {
        var (_, licenceRetrievalFailure, fellingLicence) = await GetFellingLicenceApplication.GetApplicationByIdAsync(applicationId, cancellationToken);

        if (licenceRetrievalFailure)
        {
            Logger.LogError("Unable to retrieve felling licence application {ApplicationId}", applicationId);
            return Result.Failure<LarchCheckModel>("Unable to retrieve felling licence application");
        }

        var applicationSummary = await ExtractApplicationSummaryAsync(fellingLicence, cancellationToken);

        if (applicationSummary.IsFailure)
        {
            Logger.LogError("Unable to retrieve application summary for application {id}", fellingLicence.Id);
            return Result.Failure<LarchCheckModel>("Unable to retrieve application summary");
        }

        var editable = applicationSummary.Value.AssigneeHistories.Any(x =>
           (x.Role is AssignedUserRole.AdminOfficer || x.Role is AssignedUserRole.WoodlandOfficer)
           && x.UserAccount?.Id == user.UserAccountId
           && x.TimestampUnassigned.HasValue is false)
           && (applicationSummary.Value.Status is FellingLicenceStatus.AdminOfficerReview
            || applicationSummary.Value.Status is FellingLicenceStatus.WoodlandOfficerReview);

        var larchCheckDetails = await _larchCheckService.GetLarchCheckDetailsAsync(applicationId, cancellationToken);

        var providerModel = new ActivityFeedItemProviderModel()
        {
            FellingLicenceId = applicationId,
            FellingLicenceReference = applicationSummary.Value.ApplicationReference,
            ItemTypes = [ActivityFeedItemType.LarchCheckComment],
        };

        var activityFeedItems = await _activityFeedItemProvider.RetrieveAllRelevantActivityFeedItemsAsync(
            providerModel,
            ActorType.InternalUser,
            cancellationToken);

        if (activityFeedItems.IsFailure)
        {
            logger.LogError("Unable to retrieve activity feed items with id {id} due to: {error}", applicationId, activityFeedItems.Error);
            return activityFeedItems.ConvertFailure<LarchCheckModel>();
        }

        var model = new LarchCheckModel
        {
            ApplicationId = applicationId,
            FellingLicenceApplicationSummary = applicationSummary.Value,
            AllSpecies = applicationSummary.Value.AllSpeciesBoldLarch,
            ExtendedFAD = applicationSummary.Value.FadLarchExtension(_larchOptions),
            FlyoverPeriod = FlyoverPeriodToString(_larchOptions),
            MoratoriumPeriod = MoratoriumDatesToString(_larchOptions),
            InMoratorium = InMoratorium(applicationSummary.Value.DateReceived, _larchOptions),
            Disabled = !editable,
            ActivityFeedItems = activityFeedItems.Value,
        };

        model.CombineZones(applicationSummary.Value.DetailsList);
        if (larchCheckDetails.HasValue)
        {
            model.ConfirmLarchOnly = larchCheckDetails.Value.ConfirmLarchOnly;
            model.Zone1 = larchCheckDetails.Value.Zone1;
            model.Zone2 = larchCheckDetails.Value.Zone2;
            model.Zone3 = larchCheckDetails.Value.Zone3;
            model.ConfirmMoratorium = larchCheckDetails.Value.ConfirmMoratorium;
            model.ConfirmInspectionLog = larchCheckDetails.Value.ConfirmInspectionLog;
            model.RecommendSplitApplicationDue = (RecommendSplitApplicationEnum)larchCheckDetails.Value.RecommendSplitApplicationDue;
        }
        return model;
    }

    /// <summary>
    /// Completes the mapping check task in the admin officer review.
    /// </summary>
    /// <param name="viewModel">The viewModel for the application.</param>
    /// <param name="performingUserId">The identifier for the internal user completing the check.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A result indicating whether the mapping check has been updated successfully.</returns>
    public async Task<Result> SaveLarchCheckAsync(
        LarchCheckModel viewModel,
        Guid performingUserId,
        CancellationToken cancellationToken)
    {
        var larchCheckDetails = new LarchCheckDetailsModel
        {
            FellingLicenceApplicationId = viewModel.ApplicationId,
            ConfirmLarchOnly = viewModel.ConfirmLarchOnly,
            Zone1 = viewModel.Zone1,
            Zone2 = viewModel.Zone2,
            Zone3 = viewModel.Zone3,
            ConfirmMoratorium = viewModel.ConfirmMoratorium,
            ConfirmInspectionLog = viewModel.Zone1 ? viewModel.ConfirmInspectionLog : false,
            RecommendSplitApplicationDue = viewModel.RecommendSplitApplicationDue.HasValue
                ? (int)viewModel.RecommendSplitApplicationDue.Value
                : 0
        };

        var saveResult = await _larchCheckService.SaveLarchCheckDetailsAsync(
            viewModel.ApplicationId,
            larchCheckDetails,
            performingUserId,
            cancellationToken);

        if (saveResult.IsFailure)
        {
            Logger.LogError("Failed to save larch check details: {Error}", saveResult.Error);
            return saveResult;
        }

        await AuditAdminOfficerReviewUpdateAsync(
            viewModel.ApplicationId,
            true,
            performingUserId,
            cancellationToken);

        return saveResult;
    }

    public async Task<Result<LarchFlyoverModel>> GetLarchFlyoverModelAsync(
        Guid applicationId,
        InternalUser user,
        CancellationToken cancellationToken)
    {
        var (_, licenceRetrievalFailure, fellingLicence) = await GetFellingLicenceApplication.GetApplicationByIdAsync(applicationId, cancellationToken);

        if (licenceRetrievalFailure)
        {
            Logger.LogError("Unable to retrieve felling licence application {ApplicationId}", applicationId);
            return Result.Failure<LarchFlyoverModel>("Unable to retrieve felling licence application");
        }

        var applicationSummary = await ExtractApplicationSummaryAsync(fellingLicence, cancellationToken);

        if (applicationSummary.IsFailure)
        {
            Logger.LogError("Unable to retrieve application summary for application {id}", fellingLicence.Id);
            return Result.Failure<LarchFlyoverModel>("Unable to retrieve application summary");
        }

        var editable = applicationSummary.Value.AssigneeHistories.Any(x =>
           (x.Role is AssignedUserRole.AdminOfficer || x.Role is AssignedUserRole.WoodlandOfficer)
           && x.UserAccount?.Id == user.UserAccountId
           && x.TimestampUnassigned.HasValue is false)
           && (applicationSummary.Value.Status is FellingLicenceStatus.AdminOfficerReview
            || applicationSummary.Value.Status is FellingLicenceStatus.WoodlandOfficerReview);

        var larchCheckDetails = await _larchCheckService.GetLarchCheckDetailsAsync(applicationId, cancellationToken);

        var providerModel = new ActivityFeedItemProviderModel()
        {
            FellingLicenceId = applicationId,
            FellingLicenceReference = applicationSummary.Value.ApplicationReference,
            ItemTypes = [ActivityFeedItemType.LarchCheckComment],
        };

        var activityFeedItems = await _activityFeedItemProvider.RetrieveAllRelevantActivityFeedItemsAsync(
            providerModel,
            ActorType.InternalUser,
            cancellationToken);

        if (activityFeedItems.IsFailure)
        {
            logger.LogError("Unable to retrieve activity feed items with id {id} due to: {error}", applicationId, activityFeedItems.Error);
            return activityFeedItems.ConvertFailure<LarchFlyoverModel>();
        }

        var model = new LarchFlyoverModel
        {
            ApplicationId = applicationId,
            FellingLicenceApplicationSummary = applicationSummary.Value,
            Disabled = !editable,
            ActivityFeedItems = activityFeedItems.Value,
            FlyoverDate = larchCheckDetails.Value.FlightDate != null
                    ? new DatePart(larchCheckDetails.Value.FlightDate.Value.ToLocalTime(), "felling-end")
                    : null,
            SubmissionDate = applicationSummary.Value.DateReceived,
            FlightObservations = larchCheckDetails.HasValue ? larchCheckDetails.Value.FlightObservations : null,
        };
        return model;
    }

    /// <summary>
    /// Completes the mapping check task in the admin officer review.
    /// </summary>
    /// <param name="viewModel">The viewModel for the application.</param>
    /// <param name="performingUserId">The identifier for the internal user completing the check.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A result indicating whether the mapping check has been updated successfully.</returns>
    public async Task<Result> SaveLarchFlyoverAsync(
    LarchFlyoverModel viewModel,
    Guid performingUserId,
    CancellationToken cancellationToken)
    {
        DateTime flightDate = viewModel.FlyoverDate!.CalculateDate().ToUniversalTime();

        var saveResult = await _larchCheckService.SaveLarchFlyoverAsync(
            viewModel.ApplicationId,
            flightDate,
            viewModel.FlightObservations!,
            performingUserId,
            cancellationToken);

        if (saveResult.IsFailure)
        {
            Logger.LogError("Failed to save larch check details: {Error}", saveResult.Error);
            return saveResult;
        }

        await AuditAdminOfficerReviewUpdateAsync(
            viewModel.ApplicationId,
            true,
            performingUserId,
            cancellationToken);

        return saveResult;
    }

    private string FlyoverPeriodToString(LarchOptions o)
    {
        var startDate = new DateTime(1, o.FlyoverPeriodStartMonth, o.FlyoverPeriodStartDay);
        var endDate = new DateTime(1, o.FlyoverPeriodEndMonth, o.FlyoverPeriodEndDay);

        return $"{startDate:dd MMMM} to {endDate:dd MMMM}";
    }

    private string MoratoriumDatesToString(LarchOptions o)
    {
        var endDate = new DateTime(1, o.FlyoverPeriodStartMonth, o.FlyoverPeriodStartDay).AddDays(-1);
        var startDate = new DateTime(1, o.FlyoverPeriodEndMonth, o.FlyoverPeriodEndDay).AddDays(1);

        return $"{startDate:dd MMMM} to {endDate:dd MMMM}";
    }

    private bool InMoratorium(DateTime? submissionDate, LarchOptions o)
    {
        if (!submissionDate.HasValue)
            return false;

        DateTime flyoverPeriodStartDate = new DateTime(DateTime.UtcNow.Year, o.FlyoverPeriodStartMonth, o.FlyoverPeriodStartDay, 0, 0, 0, DateTimeKind.Utc);
        DateTime flyoverPeriodEndDate = new DateTime(DateTime.UtcNow.Year, o.FlyoverPeriodEndMonth, o.FlyoverPeriodEndDay, 0, 0, 0, DateTimeKind.Utc);
        
        return !(submissionDate.Value >= flyoverPeriodStartDate && submissionDate.Value <= flyoverPeriodEndDate);
    }
}