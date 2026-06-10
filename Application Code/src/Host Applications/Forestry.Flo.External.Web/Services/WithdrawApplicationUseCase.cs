using CSharpFunctionalExtensions;
using Forestry.Flo.External.Web.Models;
using Forestry.Flo.External.Web.Models.FellingLicenceApplication;
using Forestry.Flo.External.Web.Services.Interfaces;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common.Extensions;
using Forestry.Flo.Services.Common.Infrastructure;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Extensions;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.PropertyProfiles.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace Forestry.Flo.External.Web.Services;

/// <summary>
/// Use case class dealing with withdrawing a felling licence application.
/// </summary>
public class WithdrawApplicationUseCase(IRetrieveUserAccountsService retrieveUserAccountsService,
    IRetrieveWoodlandOwners retrieveWoodlandOwnersService,
    IGetFellingLicenceApplicationForExternalUsers getFellingLicenceApplicationServiceForExternalUsers,
    IGetPropertyProfiles getPropertyProfilesService,
    IGetCompartments getCompartmentsService,
    IAgentAuthorityService agentAuthorityService,
    ILogger<WithdrawApplicationUseCase> logger)
    : ApplicationUseCaseCommon(
        retrieveUserAccountsService,
        retrieveWoodlandOwnersService,
        getFellingLicenceApplicationServiceForExternalUsers,
        getPropertyProfilesService,
        getCompartmentsService,
        agentAuthorityService,
        logger
    ), IWithdrawApplicationsUseCase
{
    private readonly ILogger<WithdrawApplicationUseCase> _logger = logger ?? new NullLogger<WithdrawApplicationUseCase>();
    
    /// <inheritdoc/>
    public async Task<Result<ConfirmWithdrawFellingLicenceApplicationViewModel>> GetConfirmWithdrawalViewModelAsync(
        Guid applicationId,
        ExternalApplicant user,
        CancellationToken cancellationToken)
    {
        // get user access model
        var userAccess = await GetUserAccessModelAsync(user, cancellationToken);
        if (userAccess.IsFailure)
        {
            _logger.LogError("Unable to retrieve user access for user with id {UserId}", user.UserAccountId.Value);
            return Result.Failure<ConfirmWithdrawFellingLicenceApplicationViewModel>("Unable to retrieve application");
        }

        // get application
        var applicationResult = await GetFellingLicenceApplicationServiceForExternalUsers
            .GetApplicationByIdAsync(applicationId, userAccess.Value, cancellationToken);

        if (applicationResult.IsFailure)
        {
            _logger.LogError("Unable to retrieve application with id {ApplicationId}", applicationId);
            return Result.Failure<ConfirmWithdrawFellingLicenceApplicationViewModel>("Unable to retrieve application");
        }

        // get application summary
        var applicationSummary = await GetApplicationSummaryAsync(applicationResult.Value, user, cancellationToken);
        if (applicationSummary.IsFailure)
        {
            _logger.LogError("Unable to retrieve application summary for application with id {ApplicationId}", applicationId);
            return Result.Failure<ConfirmWithdrawFellingLicenceApplicationViewModel>("Unable to retrieve application");
        }

        var currentStatus = applicationResult.Value.GetCurrentStatus();
        if (!FellingLicenceStatusConstants.WithdrawalStatuses.Contains(currentStatus))
        {
            _logger.LogError("Application {ApplicationId} is not in a valid state to be withdrawn: {CurrentState}",
                applicationId, currentStatus);
            return Result.Failure<ConfirmWithdrawFellingLicenceApplicationViewModel>($"Application cannot be withdrawn in the current state {currentStatus.GetDisplayNameByActorType(ActorType.ExternalApplicant)}");
        }

        var selections = Enum.GetValues(typeof(WithdrawalReason)).Cast<WithdrawalReason>();

        // filter selections to those with the ApplicantOption attribute
        selections = selections.Where(reason =>
        {
            var member = typeof(WithdrawalReason).GetMember(reason.ToString()).FirstOrDefault();
            return member != null && Attribute.IsDefined(member, typeof(ApplicantOptionAttribute));
        });

        var viewModel = new ConfirmWithdrawFellingLicenceApplicationViewModel
        {
            ApplicationId = applicationResult.Value.Id,
            ApplicationReference = applicationResult.Value.ApplicationReference,
            ApplicationSummary = applicationSummary.Value,
            TaskName = "Withdraw",
            WithdrawalReasonOptions = selections.ToDictionary(reason => reason, reason => false)
        };

        return Result.Success(viewModel);
    }
}