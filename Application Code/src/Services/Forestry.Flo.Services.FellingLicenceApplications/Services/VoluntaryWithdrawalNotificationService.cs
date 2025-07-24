using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Microsoft.Extensions.Logging;
using NodaTime;

namespace Forestry.Flo.Services.FellingLicenceApplications.Services;

public class VoluntaryWithdrawalNotificationService : IVoluntaryWithdrawalNotificationService
{
    private readonly ILogger<VoluntaryWithdrawalNotificationService> _logger;
    private readonly IFellingLicenceApplicationInternalRepository _applicationInternalRepository;
    private readonly IClock _clock;

    public VoluntaryWithdrawalNotificationService(
        IClock clock,
        IFellingLicenceApplicationInternalRepository applicationInternalRepository,
        ILogger<VoluntaryWithdrawalNotificationService> logger)
    {
        _logger = logger;
        _applicationInternalRepository = Guard.Against.Null(applicationInternalRepository);
        _clock = Guard.Against.Null(clock);
    }

    ///<inheritdoc />

    public async Task<Result<IList<VoluntaryWithdrawalNotificationModel>>> GetApplicationsAfterThresholdForWithdrawalAsync(
        TimeSpan ThresholdAfterStatusCreatedDate,
        CancellationToken cancellationToken)
    {
        var currentTime = _clock.GetCurrentInstant().ToDateTimeUtc();

        var relevantApplications = 
            await _applicationInternalRepository.GetApplicationsThatAreWithinThresholdOfWithdrawalNotificationDateAsync(
                currentTime,
                ThresholdAfterStatusCreatedDate,
                cancellationToken);

        var voluntaryWithdrawalApplications = new List<VoluntaryWithdrawalNotificationModel>();

        foreach (var application in relevantApplications)
        {
            var tempVoluntaryWithdrawalApplication = new VoluntaryWithdrawalNotificationModel
            {
                ApplicationId = application.Id,
                ApplicationReference = application.ApplicationReference,
                PropertyName = application.SubmittedFlaPropertyDetail?.Name,
                CreatedById = application.CreatedById,
                WithApplicantDate = application.StatusHistories
                    .Where(x => x.Status is FellingLicenceStatus.WithApplicant or FellingLicenceStatus.ReturnedToApplicant)
                    .MaxBy(y => y.Created)!.Created,
                WoodlandOwnerId = application.WoodlandOwnerId,
                NotificationDateSent = currentTime,
                AdministrativeRegion = application.AdministrativeRegion
            };

            voluntaryWithdrawalApplications.Add(tempVoluntaryWithdrawalApplication);

            application.VoluntaryWithdrawalNotificationTimeStamp = currentTime;

            _logger.LogDebug("Voluntary withdrawal date set to {VoluntaryWithdrawalNotificationTimeStamp} for application {appId}", application.VoluntaryWithdrawalNotificationTimeStamp, application.Id);
        }

        var saveResult = await _applicationInternalRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);


        return saveResult.IsFailure
            ? Result.Failure<IList<VoluntaryWithdrawalNotificationModel>>($"Unable to update Voluntary Withdrawal Notification Date for the applications, error: {saveResult.Error}")
            : Result.Success<IList<VoluntaryWithdrawalNotificationModel>>(voluntaryWithdrawalApplications);
    }
}