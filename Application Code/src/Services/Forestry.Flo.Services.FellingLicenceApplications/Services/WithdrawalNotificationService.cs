using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Microsoft.Extensions.Logging;
using NodaTime;

namespace Forestry.Flo.Services.FellingLicenceApplications.Services;

public class WithdrawalNotificationService : IWithdrawalNotificationService
{
    private readonly ILogger<WithdrawalNotificationService> _logger;
    private readonly IFellingLicenceApplicationInternalRepository _applicationInternalRepository;
    private readonly IClock _clock;

    public WithdrawalNotificationService(
        IClock clock,
        IFellingLicenceApplicationInternalRepository applicationInternalRepository,
        ILogger<WithdrawalNotificationService> logger)
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
            await _applicationInternalRepository.GetApplicationsThatAreWithinThresholdAutomaticWithdrawalDateAsync(
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

            _logger.LogDebug("Application picked up for automatic withdrawal WithApplicantDate:{WithApplicantDate} appId:{appId}", tempVoluntaryWithdrawalApplication.WithApplicantDate, application.Id);
        }

        return voluntaryWithdrawalApplications;
    }
}