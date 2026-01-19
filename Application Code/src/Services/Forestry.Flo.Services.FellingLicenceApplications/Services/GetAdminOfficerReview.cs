using CSharpFunctionalExtensions;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Models.AdminOfficerReview;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Forestry.Flo.Services.FellingLicenceApplications.Services;

public class GetAdminOfficerReviewService : IGetAdminOfficerReview
{
    private readonly IFellingLicenceApplicationInternalRepository _repository;
    private readonly ILogger<GetAdminOfficerReviewService> _logger;

    public GetAdminOfficerReviewService(
        IFellingLicenceApplicationInternalRepository repository,
        ILogger<GetAdminOfficerReviewService> logger)
    {
        ArgumentNullException.ThrowIfNull(repository);

        _repository = repository;
        _logger = logger ?? new NullLogger<GetAdminOfficerReviewService>();
    }

    /// <inheritdoc />
    public async Task<AdminOfficerReviewStatusModel> GetAdminOfficerReviewStatusAsync(
        Guid applicationId,
        bool isAgentApplication,
        bool isLarchApplication,
        bool isAssignedWoodlandOfficer,
        bool isCBWApplication,
        bool isEiaApplication,
        bool isTreeHealthApplication,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Attempting to retrieve admins officer review entry for application with id {ApplicationId}",
            applicationId);
        var (_, adminOfficerReview) = await _repository.GetAdminOfficerReviewAsync(applicationId, cancellationToken);

        _logger.LogDebug(
            "Returning status of the admin officer review task list steps for application with id {ApplicationId}",
            applicationId);

        var LarchFlyoverComplete = adminOfficerReview != null && adminOfficerReview.LarchChecked == true &&
                                   adminOfficerReview.FellingLicenceApplication?.LarchCheckDetails?.FlightDate != null;

        // Determine if flyover is required: only required when it's a larch application AND inspection log was confirmed true
        var isFlyoverRequired = isLarchApplication &&
                                (adminOfficerReview?.FellingLicenceApplication?.LarchCheckDetails?.ConfirmInspectionLog == true);

        return new AdminOfficerReviewStatusModel
        {
            AdminOfficerReviewTaskListStates = new AdminOfficerReviewTaskListStates(
                ConvertBoolToInternalReviewStepStatusWithFailureState(
                    adminOfficerReview?.AgentAuthorityFormChecked, adminOfficerReview?.AgentAuthorityCheckPassed),
                ConvertBoolToInternalReviewStepStatusWithFailureState(
                    adminOfficerReview?.MappingChecked, adminOfficerReview?.MappingCheckPassed),
                CalculateConstraintsCheckStatus(isAgentApplication, adminOfficerReview),
                isAssignedWoodlandOfficer
                    ? InternalReviewStepStatus.Completed
                    : InternalReviewStepStatus.NotStarted,
                CalculateLarchCheckStatus(isLarchApplication, adminOfficerReview),
                isFlyoverRequired
                    ? (adminOfficerReview?.LarchChecked != true
                        ? InternalReviewStepStatus.CannotStartYet
                        : LarchFlyoverComplete
                            ? InternalReviewStepStatus.Completed
                            : InternalReviewStepStatus.NotStarted)
                    : InternalReviewStepStatus.NotRequired,
                CalculateCBWStatus(isCBWApplication, adminOfficerReview),
                CalculateEiaStatus(isEiaApplication, adminOfficerReview),
                isTreeHealthApplication
                    ? ConvertBoolToInternalReviewStepStatus(adminOfficerReview?.IsTreeHealthAnswersChecked)
                    : InternalReviewStepStatus.NotRequired,
                isAgentApplication),
        };
    }

    /// <inheritdoc />
    public async Task<bool?> GetCBWReviewStatusAsync(
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        var (_, adminOfficerReview) = await _repository.GetAdminOfficerReviewAsync(applicationId, cancellationToken);
        return adminOfficerReview?.CBWChecked;
    }

    private static InternalReviewStepStatus ConvertBoolToInternalReviewStepStatus(bool? complete)
    {
        return complete switch
        {
            true => InternalReviewStepStatus.Completed,
            false => InternalReviewStepStatus.InProgress,
            null => InternalReviewStepStatus.NotStarted
        };
    }

    private static InternalReviewStepStatus ConvertBoolToInternalReviewStepStatusWithFailureState(
        bool? complete,
        bool? outcome)
    {
        return complete switch
        {
            true => outcome.HasValue && outcome.Value
                ? InternalReviewStepStatus.Completed
                : InternalReviewStepStatus.Failed,
            false => InternalReviewStepStatus.InProgress,
            null => InternalReviewStepStatus.NotStarted
        };
    }

    private static InternalReviewStepStatus CalculateConstraintsCheckStatus(
        bool isAgentApplication,
        AdminOfficerReview? adminOfficerReview)
    {
        // if Agent application, need AAF and mapping check passed to start it
        if (isAgentApplication && adminOfficerReview is { AgentAuthorityCheckPassed: true, MappingCheckPassed: true })
        {
            return ConvertBoolToInternalReviewStepStatus(adminOfficerReview.ConstraintsChecked);
        }

        // if not Agent application, only need mapping check passed to start it
        if (!isAgentApplication && adminOfficerReview is { MappingCheckPassed: true })
        {
            return ConvertBoolToInternalReviewStepStatus(adminOfficerReview.ConstraintsChecked);
        }

        return InternalReviewStepStatus.CannotStartYet;
    }

    private static InternalReviewStepStatus CalculateLarchCheckStatus(
        bool isLarchApplication,
        AdminOfficerReview? adminOfficerReview)
    {
        if (!isLarchApplication)
            return InternalReviewStepStatus.NotRequired;

        if (adminOfficerReview is { ConstraintsChecked: true, MappingCheckPassed: true })
        {
            return adminOfficerReview.LarchChecked switch
            {
                true => InternalReviewStepStatus.Completed,
                false => InternalReviewStepStatus.InProgress,
                null => InternalReviewStepStatus.NotStarted
            };
        }

        return InternalReviewStepStatus.CannotStartYet;
    }

    private static InternalReviewStepStatus CalculateCBWStatus(
        bool isCBWApplication,
        AdminOfficerReview? adminOfficerReview)
    {
        if (!isCBWApplication)
            return InternalReviewStepStatus.NotRequired;

        if (adminOfficerReview is { ConstraintsChecked: true, MappingCheckPassed: true })
        {
            return adminOfficerReview.CBWChecked switch
            {
                true => InternalReviewStepStatus.Completed,
                false => InternalReviewStepStatus.Completed,
                null => InternalReviewStepStatus.NotStarted
            };
        }

        return InternalReviewStepStatus.CannotStartYet;
    }

    private static InternalReviewStepStatus CalculateEiaStatus(
        bool isEiaApplication,
        AdminOfficerReview? adminOfficerReview)
    {
        if (!isEiaApplication)
            return InternalReviewStepStatus.NotRequired;

        if (adminOfficerReview is { ConstraintsChecked: true, MappingCheckPassed: true })
        {
            return adminOfficerReview.EiaChecked switch
            {
                true or false => InternalReviewStepStatus.Completed,
                null => InternalReviewStepStatus.NotStarted
            };
        }

        return InternalReviewStepStatus.CannotStartYet;
    }

}