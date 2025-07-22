using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Microsoft.Extensions.Logging;
using NodaTime;

namespace Forestry.Flo.Services.FellingLicenceApplications.Services;

public class ExtendApplicationsService : IExtendApplications
{
    private readonly ILogger<ExtendApplicationsService> _logger;
    private readonly IFellingLicenceApplicationInternalRepository _applicationInternalRepository;
    private readonly IClock _clock;

    public ExtendApplicationsService(
        IClock clock,
        IFellingLicenceApplicationInternalRepository applicationInternalRepository,
        ILogger<ExtendApplicationsService> logger)
    {
        _logger = logger;
        _applicationInternalRepository = Guard.Against.Null(applicationInternalRepository);
        _clock = Guard.Against.Null(clock);
    }

    ///<inheritdoc />

    public async Task<Result<IList<ApplicationExtensionModel>>> ApplyApplicationExtensionsAsync(
        TimeSpan extensionLength,
        TimeSpan periodBeforeThreshold,
        CancellationToken cancellationToken)
    {
        var currentTime = _clock.GetCurrentInstant().ToDateTimeUtc();

        var relevantApplications = 
            await _applicationInternalRepository.GetApplicationsThatAreWithinThresholdOfFinalActionDateAsync(
                currentTime,
                periodBeforeThreshold,
                cancellationToken);

        var extensionModels = new List<ApplicationExtensionModel>();

        foreach (var application in relevantApplications)
        {
            var applicationExtensionModel = new ApplicationExtensionModel
            {
                ApplicationId = application.Id,
                ApplicationReference = application.ApplicationReference,
                AssignedFCUserIds = application.AssigneeHistories.Where(x => x.Role is not AssignedUserRole.Applicant).Select(x => x.AssignedUserId).ToList(),
                CreatedById = application.CreatedById,
                SubmissionDate = application.StatusHistories.MaxBy(y => y.Status is FellingLicenceStatus.Submitted)!.Created,
                WoodlandOwnerId = application.WoodlandOwnerId,
                AdminHubName = application.AdministrativeRegion
            };

            application.FinalActionDate = application.FinalActionDate!.Value.Add(extensionLength);
            application.FinalActionDateExtended = true;

            applicationExtensionModel.ExtensionLength = extensionLength;
            applicationExtensionModel.FinalActionDate = application.FinalActionDate.Value;

            extensionModels.Add(applicationExtensionModel);

            _logger.LogDebug("Final action date extended to {newFinalActionDate} for application {appId}", application.FinalActionDate, application.Id);
        }

        var saveResult = await _applicationInternalRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);


        return saveResult.IsFailure
            ? Result.Failure<IList<ApplicationExtensionModel>>($"Unable to update final action dates for applications, error: {saveResult.Error}")
            : Result.Success<IList<ApplicationExtensionModel>>(extensionModels);
    }
}