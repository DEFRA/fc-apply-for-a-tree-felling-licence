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
    public async Task<Result<List<Guid>>> WithdrawApplicationAsync(
        Guid applicationId,
        UserAccessModel userAccessModel,
        List<WithdrawalReason> withdrawalReasons,
        string? withdrawalReasonsOtherDetails,
        CancellationToken cancellationToken)
    {
        var applicationResult =
            await _getFellingLicenceApplicationForExternalUsersService.GetApplicationByIdAsync(
                applicationId,
                userAccessModel,
                cancellationToken).ConfigureAwait(false);

        if (applicationResult.IsFailure || applicationResult.Value.LinkedPropertyProfile is null)
        {
            _logger.LogError("Failed to get felling application with id {applicationId}, (error: {error})",
                applicationId, applicationResult.IsFailure ? applicationResult.Error : "Application has no linked property profile");
            return Result.Failure<List<Guid>>($"Failed to retrieve felling licence application");
        }

        var applicationStatus = applicationResult.Value.GetCurrentStatus();

        if (!FellingLicenceStatusConstants.WithdrawalStatuses.Contains(applicationStatus))
        {
            _logger.LogError("{EntityName} with ID {applicationId} cannot be withdrawn as it's in status {CurrentStatus}",
                nameof(FellingLicenceApplication), applicationId, applicationStatus);
            return Result.Failure<List<Guid>>($"Application cannot be withdrawn as it is in status {applicationStatus}");
        }

        var assignedUsers = applicationResult.Value.AssigneeHistories
            .Where(x =>
                x.TimestampUnassigned is null &&
                x.Role is not (AssignedUserRole.Author or AssignedUserRole.Applicant))
            .Select(x => x.AssignedUserId).ToList();

        var updateResult = await _fellingLicenceApplicationExternalRepository.WithdrawApplicationAsync(
            applicationId,
            userAccessModel.IsSystemUser ? null : userAccessModel.UserAccountId,
            _clock.GetCurrentInstant().ToDateTimeUtc(),
            withdrawalReasons,
            withdrawalReasonsOtherDetails,
            cancellationToken).ConfigureAwait(false);

        if (updateResult.IsFailure)
        {
            _logger.LogError("Unable to update application {ApplicationId} to withdrawn, error: {Error}",
                applicationId, updateResult.Error);
            return Result.Failure<List<Guid>>($"Unable to withdraw application {applicationId}");
        }

        return Result.Success(assignedUsers);
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