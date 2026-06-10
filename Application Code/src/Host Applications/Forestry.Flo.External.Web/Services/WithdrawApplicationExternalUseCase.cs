using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.External.Web.Infrastructure;
using Forestry.Flo.External.Web.Services.Interfaces;
using Forestry.Flo.HostApplicationsCommon.Infrastructure;
using Forestry.Flo.HostApplicationsCommon.Services;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.Gis.Interfaces;
using Forestry.Flo.Services.InternalUsers.Services;
using Forestry.Flo.Services.Notifications.Services;
using Forestry.Flo.Services.PropertyProfiles.Services;
using Microsoft.Extensions.Options;
using NodaTime;

namespace Forestry.Flo.External.Web.Services;

/// <summary>
/// Implementation of the <see cref="IWithdrawApplicationExternalUseCase"/> for handling the withdrawal of felling licence applications
/// from the external applicant interface. This class inherits from <see cref="WithdrawApplicationUseCaseBase"/> to reuse the common logic
/// for withdrawing applications, while implementing the specific contract defined in <see cref="IWithdrawApplicationExternalUseCase"/>
/// for the external context.
/// </summary>
public class WithdrawApplicationExternalUseCase(
    IGetFellingLicenceApplicationForExternalUsers getFellingLicenceApplicationServiceForExternalUsers, 
    IFellingLicenceApplicationExternalRepository fellingLicenceApplicationExternalRepository, 
    IWithdrawFellingLicenceService withdrawFellingLicenceService, 
    IAuditService<WithdrawApplicationUseCaseBase> auditService, 
    IClock clock, 
    IPublicRegister publicRegisterService, 
    IGetPropertyProfiles getPropertyProfilesService, 
    IGetConfiguredFcAreas getConfiguredFcAreasService, 
    IRetrieveWoodlandOwners woodlandOwnerService, 
    IRetrieveUserAccountsService retrieveExternalAccountsService, 
    IUserAccountService internalUserAccountService, 
    ISendNotifications sendNotifications,
    IOptions<InternalUserSiteOptions> internalUserSiteOptions,
    RequestContext requestContext, 
    ILogger<WithdrawApplicationUseCaseBase> logger) 
    : WithdrawApplicationUseCaseBase(
        getFellingLicenceApplicationServiceForExternalUsers, 
        fellingLicenceApplicationExternalRepository, 
        withdrawFellingLicenceService, 
        auditService, 
        clock, 
        publicRegisterService, 
        getPropertyProfilesService, 
        getConfiguredFcAreasService, 
        woodlandOwnerService, 
        retrieveExternalAccountsService, 
        internalUserAccountService, 
        sendNotifications, 
        requestContext, 
        logger), IWithdrawApplicationExternalUseCase
{
    private readonly IRetrieveUserAccountsService _retrieveExternalAccountsService
        = Guard.Against.Null(retrieveExternalAccountsService);

    private readonly ILogger<WithdrawApplicationUseCaseBase> _logger = logger;

    private readonly InternalUserSiteOptions _internalUserSiteOptions = Guard.Against.Null(internalUserSiteOptions.Value);

    public async Task<Result> WithdrawApplicationAsync(
        Guid applicationId, 
        ExternalApplicant user, 
        List<WithdrawalReason> withdrawalReasons,
        string? withdrawalReasonsOtherDetails, 
        string linkToApplication, 
        CancellationToken cancellationToken)
    {
        var userAccess = await _retrieveExternalAccountsService
            .RetrieveUserAccessAsync(user.UserAccountId!.Value, cancellationToken)
            .ConfigureAwait(false);

        if (userAccess.IsFailure)
        {
            _logger.LogError(
                "Could not Withdraw the Felling Licence Application with ID {ApplicationId} when requested by user with ID {UserAccountId}, user access to this application was denied",
                applicationId,
                user.UserAccountId);
            return Result.Failure(
                $"Attempt to access Felling Licence Application with id: {applicationId} by user with Id of {user.UserAccountId} resulted in access being denied");
        }

        var internalLinkToApplication =
            $"{_internalUserSiteOptions.BaseUrl}FellingLicenceApplication/ApplicationSummary/{applicationId}";

        return await WithdrawApplicationAsync(
            applicationId,
            userAccess.Value,
            withdrawalReasons,
            withdrawalReasonsOtherDetails,
            linkToApplication,
            internalLinkToApplication,
            cancellationToken)
            .ConfigureAwait(false);
    }
}