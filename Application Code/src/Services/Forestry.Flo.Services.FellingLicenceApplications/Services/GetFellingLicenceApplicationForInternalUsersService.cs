using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using NodaTime;

namespace Forestry.Flo.Services.FellingLicenceApplications.Services;

/// <summary>
/// Service class for retrieving <see cref="FellingLicenceApplication"/>. 
/// </summary>
public class GetFellingLicenceApplicationForInternalUsersService : IGetFellingLicenceApplicationForInternalUsers
{
    private readonly IFellingLicenceApplicationInternalRepository _fellingLicenceApplicationInternalRepository;
    private readonly IClock _clock;

    public GetFellingLicenceApplicationForInternalUsersService(
        IFellingLicenceApplicationInternalRepository fellingLicenceApplicationInternalRepository,
        IClock clock)
    {
        _fellingLicenceApplicationInternalRepository = Guard.Against.Null(fellingLicenceApplicationInternalRepository);
        _clock = Guard.Against.Null(clock);
    }

    /// <inheritdoc />>
    public async Task<Result<FellingLicenceApplication>> GetApplicationByIdAsync(
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        var application = await _fellingLicenceApplicationInternalRepository.GetAsync(applicationId, cancellationToken);

        return application.HasValue
            ? Result.Success(application.Value)
            : Result.Failure<FellingLicenceApplication>($"Unable to retrieve application with id {applicationId}");
    }

    ///<inheritdoc />
    public async Task<IList<PublicRegisterPeriodEndModel>> RetrieveApplicationsHavingExpiredOnTheConsultationPublicRegisterAsync(
        CancellationToken cancellationToken)
    {
        var currentTime = _clock.GetCurrentInstant().ToDateTimeUtc();

        var applicationsRequiringNotifications =
            await _fellingLicenceApplicationInternalRepository.GetApplicationsWithExpiredConsultationPublicRegisterPeriodsAsync(
                currentTime,
                cancellationToken);

        return applicationsRequiringNotifications.Select(x => new PublicRegisterPeriodEndModel
        {
            PublicRegister = x.PublicRegister,
            AssignedUserIds = x.AssigneeHistories
                .Where(y => y.TimestampUnassigned is null)
                .Select(y => y.AssignedUserId).ToList(),
            ApplicationReference = x.ApplicationReference,
            PropertyName = x.SubmittedFlaPropertyDetail?.Name,
            AdminHubName = x.AdministrativeRegion
        }).ToList();
    }

    ///<inheritdoc />
    public async Task<IList<PublicRegisterPeriodEndModel>> RetrieveFinalisedApplicationsHavingExpiredOnTheDecisionPublicRegisterAsync(
        CancellationToken cancellationToken)
    {
        var currentTime = _clock.GetCurrentInstant().ToDateTimeUtc();

        var applicationsRequiringNotifications =
            await _fellingLicenceApplicationInternalRepository.GetFinalisedApplicationsWithExpiredDecisionPublicRegisterPeriodsAsync(
                currentTime,
                cancellationToken);

        // https://harrishealthalliance.atlassian.net/browse/FLOV2-1596
        // AC4, As an Approver, I need to know that the published application has been taken off the register

        return applicationsRequiringNotifications.Select(x => new PublicRegisterPeriodEndModel
        {
            PublicRegister = x.PublicRegister,
            AssignedUserIds = x.AssigneeHistories
                .Where(y => y.TimestampUnassigned is null && y.Role is AssignedUserRole.FieldManager) //only the Approver (internally the FM) needs to know
                .Select(y => y.AssignedUserId).ToList(),
            ApplicationReference = x.ApplicationReference,
            PropertyName = x.SubmittedFlaPropertyDetail?.Name,
            AdminHubName = x.AdministrativeRegion
        }).ToList();
    }

    ///<inheritdoc />
    public async Task<Maybe<PublicRegisterPeriodEndModel>> RetrievePublicRegisterForRemoval(Guid applicationId, CancellationToken cancellationToken)
    {
        var application = await _fellingLicenceApplicationInternalRepository
            .GetAsync(applicationId, cancellationToken)
            .ConfigureAwait(false);

        if (application.HasNoValue || application.Value.PublicRegister == null)
        {
            return Maybe<PublicRegisterPeriodEndModel>.None;
        }

        return Maybe<PublicRegisterPeriodEndModel>.From(new PublicRegisterPeriodEndModel
        {
            ApplicationReference = application.Value.ApplicationReference,
            PropertyName = application.Value.SubmittedFlaPropertyDetail?.Name,
            PublicRegister = application.Value.PublicRegister,
            AdminHubName = application.Value.AdministrativeRegion,
            AssignedUserIds = application.Value.AssigneeHistories
                .Where(y => y.TimestampUnassigned is null)
                .Select(y => y.AssignedUserId).ToList()
        });
    }

    ///<inheritdoc />
    public async Task<Result<ApplicationNotificationDetails>> RetrieveApplicationNotificationDetailsAsync(
        Guid applicationId, 
        UserAccessModel userAccessModel,
        CancellationToken cancellationToken)
    {
        var checkAccessResult = await _fellingLicenceApplicationInternalRepository
            .CheckUserCanAccessApplicationAsync(applicationId, userAccessModel, cancellationToken)
            .ConfigureAwait(false);

        if (checkAccessResult.IsFailure || checkAccessResult.Value is false)
        {
            return Result.Failure<ApplicationNotificationDetails>("Could not access application to retrieve details");
        }

        var getApplicationResult = await _fellingLicenceApplicationInternalRepository
            .GetAsync(applicationId, cancellationToken)
            .ConfigureAwait(false);

        if (getApplicationResult.HasNoValue)
        {
            return Result.Failure<ApplicationNotificationDetails>("Could not access application to retrieve details");
        }

        return Result.Success(new ApplicationNotificationDetails
        {
            ApplicationReference = getApplicationResult.Value.ApplicationReference,
            PropertyName = getApplicationResult.Value.SubmittedFlaPropertyDetail?.Name,
            AdminHubName = getApplicationResult.Value.AdministrativeRegion,
        });
    }

    ///<inheritdoc />
    public async Task<Result<List<ApplicationAssigneeModel>>> GetApplicationAssignedUsers(
        Guid applicationId, 
        UserAccessModel userAccessModel,
        CancellationToken cancellationToken)
    {
        var checkAccessResult = await _fellingLicenceApplicationInternalRepository
            .CheckUserCanAccessApplicationAsync(applicationId, userAccessModel, cancellationToken)
            .ConfigureAwait(false);

        if (checkAccessResult.IsFailure || checkAccessResult.Value is false)
        {
            return Result.Failure<List<ApplicationAssigneeModel>>("Could not access application assigned users");
        }

        var assignments = await _fellingLicenceApplicationInternalRepository
            .GetAssigneeHistoryForApplicationAsync(applicationId, cancellationToken)
            .ConfigureAwait(false);

        var result = assignments.Select(x => new ApplicationAssigneeModel(
                x.Id, x.FellingLicenceApplicationId, x.AssignedUserId, x.TimestampAssigned, x.TimestampUnassigned, x.Role))
            .ToList();
        return Result.Success(result);
    }

    ///<inheritdoc />
    public async Task<Result<List<StatusHistoryModel>>> GetApplicationStatusHistory(
        Guid applicationId, 
        UserAccessModel userAccessModel,
        CancellationToken cancellationToken)
    {
        var checkAccessResult = await _fellingLicenceApplicationInternalRepository
            .CheckUserCanAccessApplicationAsync(applicationId, userAccessModel, cancellationToken)
            .ConfigureAwait(false);

        if (checkAccessResult.IsFailure || checkAccessResult.Value is false)
        {
            return Result.Failure<List<StatusHistoryModel>>("Could not access application status history");
        }

        var statusHistoryEntities = await _fellingLicenceApplicationInternalRepository
            .GetStatusHistoryForApplicationAsync(applicationId, cancellationToken)
            .ConfigureAwait(false);

        var models = statusHistoryEntities
            .Select(x => new StatusHistoryModel(x.Created, x.CreatedById, x.Status))
            .ToList();

        return Result.Success(models);
    }

    ///<inheritdoc />
    public async Task<Result<List<SubmittedFlaPropertyCompartment>>> GetSubmittedFlaPropertyCompartmentsByApplicationIdAsync(
        Guid applicationId,
        CancellationToken cancellationToken) =>  
        await _fellingLicenceApplicationInternalRepository
            .GetSubmittedFlaPropertyCompartmentsByApplicationIdAsync(applicationId, cancellationToken)
            .ConfigureAwait(false);

    ///<inheritdoc />
    public Task<Result<EnvironmentalImpactAssessment>> GetEnvironmentalImpactAssessmentAsync(
        Guid applicationId,
        CancellationToken cancellationToken) 
        => _fellingLicenceApplicationInternalRepository.GetEnvironmentalImpactAssessmentAsync(
            applicationId,
            cancellationToken);

    ///<inheritdoc />
    public async Task<IList<PublicRegisterPeriodEndModel>> RetrieveApplicationsOnTheConsultationPublicRegisterAsync(
        CancellationToken cancellationToken)
    {
        var applications = await _fellingLicenceApplicationInternalRepository.GetApplicationsOnConsultationPublicRegisterPeriodsAsync(cancellationToken);

        var filtered = applications
            .Where(x => x.PublicRegister != null
                && x.PublicRegister.ConsultationPublicRegisterRemovedTimestamp == null
                && x.PublicRegister.ConsultationPublicRegisterPublicationTimestamp != null)
            .Select(x => new PublicRegisterPeriodEndModel
            {
                PublicRegister = x.PublicRegister,
                AssignedUserIds = x.AssigneeHistories
                    .Where(y => y.TimestampUnassigned is null)
                    .Select(y => y.AssignedUserId).ToList(),
                ApplicationReference = x.ApplicationReference,
                PropertyName = x.SubmittedFlaPropertyDetail?.Name,
                AdminHubName = x.AdministrativeRegion
            })
            .ToList();

        return filtered;
    }
}