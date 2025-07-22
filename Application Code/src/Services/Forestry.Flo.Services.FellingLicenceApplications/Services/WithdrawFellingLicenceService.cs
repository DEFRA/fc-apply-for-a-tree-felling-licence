using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common.Extensions;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Microsoft.Extensions.Logging;
using NodaTime;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Extensions;

namespace Forestry.Flo.Services.FellingLicenceApplications.Services;

public class WithdrawFellingLicenceService(
    ILogger<WithdrawFellingLicenceService> logger,
    IFellingLicenceApplicationInternalRepository fellingLicenceApplicationInternalRepository,
    IFellingLicenceApplicationExternalRepository fellingLicenceApplicationExternalRepository,
    IGetFellingLicenceApplicationForExternalUsers getFellingLicenceApplicationForExternalUsersService,
    IClock clock)
    : IWithdrawFellingLicenceService
{
    private readonly IGetFellingLicenceApplicationForExternalUsers
        _getFellingLicenceApplicationForExternalUsersService =
            Guard.Against.Null(getFellingLicenceApplicationForExternalUsersService);

    private readonly IFellingLicenceApplicationInternalRepository _fellingLicenceApplicationInternalRepository =
        Guard.Against.Null(fellingLicenceApplicationInternalRepository);

    private readonly IFellingLicenceApplicationExternalRepository _fellingLicenceApplicationExternalRepository =
        Guard.Against.Null(fellingLicenceApplicationExternalRepository);

    private readonly IClock _clock = Guard.Against.Null(clock);
    private readonly ILogger<WithdrawFellingLicenceService> _logger = Guard.Against.Null(logger);

    /// <inheritdoc />
    public async Task<Result<IList<Guid>>> WithdrawApplication(
        Guid applicationId,
        UserAccessModel userAccessModel,
        CancellationToken cancellationToken)
    {
        var applicationResult =
            await _getFellingLicenceApplicationForExternalUsersService.GetApplicationByIdAsync(
                applicationId,
                userAccessModel,
                cancellationToken);

        if (applicationResult.IsFailure || applicationResult.Value.LinkedPropertyProfile is null)
        {
            _logger.LogError("Failed to get felling application with id {applicationId}, (error if present is {error})",
                applicationId, applicationResult.Error);
            return Result.Failure<IList<Guid>>($"Failed to get {nameof(FellingLicenceApplication)}");
        }

        var applicationStatus = applicationResult.Value.GetCurrentStatus();

        if (applicationStatus is FellingLicenceStatus.Withdrawn)
        {
            _logger.LogError("{EntityName} with ID {applicationId} is already withdrawn",
                nameof(FellingLicenceApplication), applicationId);
            return Result.Failure<IList<Guid>>($"{nameof(FellingLicenceApplication)} is already withdrawn");
        }

        if (FellingLicenceStatusConstants.WithdrawalStatuses.Contains(applicationStatus) is false)
        {
            _logger.LogError(
                "{EntityName} with ID {applicationId} cannot be withdrawn due to its status of {Status}",
                nameof(FellingLicenceApplication),
                applicationId,
                applicationStatus.GetDisplayNameByActorType(ActorType.InternalUser));
            return Result.Failure<IList<Guid>>(
                $"{nameof(FellingLicenceApplication)} cannot be withdrawn due to its status");
        }

        await _fellingLicenceApplicationExternalRepository.AddStatusHistory(userAccessModel.UserAccountId,
            applicationId,
            FellingLicenceStatus.Withdrawn,
            cancellationToken);

        IList<Guid> internalUsersAssigned = applicationResult.Value.AssigneeHistories
            .Where(x =>
                x.TimestampUnassigned is null &&
                x.Role is not (AssignedUserRole.Author or AssignedUserRole.Applicant))
            .Select(x => x.AssignedUserId).ToList();
        return Result.Success(internalUsersAssigned);
    }

    public async Task<Result> RemoveAssignedWoodlandOfficerAsync(
        Guid applicationId,
        IList<Guid> internalUsers,
        CancellationToken cancellationToken)
    {
        var unassignedAll = true;
        internalUsers = internalUsers
            .Distinct()
            .ToList();

        foreach (var userId in internalUsers)
        {
            var result = await _fellingLicenceApplicationInternalRepository
                .RemoveAssignedFellingLicenceApplicationStaffMemberAsync(
                    applicationId,
                    userId,
                    _clock.GetCurrentInstant().ToDateTimeUtc(),
                    cancellationToken);
            if (result.IsFailure)
            {
                _logger.LogError(
                    $"Could not remove the assignment of the internal user with user ID {userId} from the {nameof(FellingLicenceApplication)} with ID {applicationId}");

                unassignedAll = false;
            }
        }

        return unassignedAll != true
            ? Result.Failure("Could not remove the assignment of at least one of the internal users!")
            : Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> UpdatePublicRegisterEntityToRemovedAsync(
        Guid applicationId,
        Guid? userId,
        DateTime removedDateTime,
        CancellationToken cancellationToken)
    {
        if (userId.HasValue)
        {
            _logger.LogDebug(
                "Attempting to update the public register information for {ApplicationId} on behalf of user with ID {UserId}",
                applicationId,
                userId);
        }
        else
        {
            _logger.LogDebug(
                "Attempting to update the public register information for {ApplicationId} as a result of automatic withdrawal",
                applicationId);
        }

        try
        {
            var maybeExistingPr =
                await _fellingLicenceApplicationInternalRepository.GetPublicRegisterAsync(applicationId,
                    cancellationToken);

            if (maybeExistingPr.HasNoValue ||
                maybeExistingPr.Value.ConsultationPublicRegisterPublicationTimestamp.HasValue is false)
            {
                _logger.LogWarning(
                    "Attempt to set removed from public register date but no prior publication date exists, returning failure");
                return Result.Failure("Public register does not have a publication date.");
            }

            maybeExistingPr.Value.ConsultationPublicRegisterRemovedTimestamp = removedDateTime;

            var saveResult =
                await _fellingLicenceApplicationInternalRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
            if (saveResult.IsFailure)
            {
                _logger.LogError("Could not save changes to public register, error: {Error}", saveResult.Error);
                return Result.Failure(saveResult.Error.ToString());
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception caught in PublishedToPublicRegisterAsync");
            return Result.Failure(ex.Message);
        }
    }
}