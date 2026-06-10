using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.HostApplicationsCommon.Services;
using Forestry.Flo.Internal.Web.Infrastructure;
using Forestry.Flo.Internal.Web.Services.Interfaces;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.Gis.Interfaces;
using Forestry.Flo.Services.InternalUsers.Services;
using Forestry.Flo.Services.Notifications.Services;
using Forestry.Flo.Services.PropertyProfiles.Services;
using Microsoft.Extensions.Options;
using NodaTime;

namespace Forestry.Flo.Internal.Web.Services.FellingLicenceApplication;

/// <summary>
/// Implementation of the <see cref="IWithdrawApplicationInternalUseCase"/> for handling the withdrawal of felling licence applications
/// from scheduled tasks that run in the internal FC interface. This class inherits from <see cref="WithdrawApplicationUseCaseBase"/> to
/// reuse the common logic for withdrawing applications, while implementing the specific contract defined in
/// <see cref="IWithdrawApplicationInternalUseCase"/> for the internal context.
/// </summary>
public class WithdrawApplicationInternalUseCase(
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
    IOptions<ExternalApplicantSiteOptions> externalApplicantSiteOptions,
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
        logger), IWithdrawApplicationInternalUseCase
{

    private readonly ExternalApplicantSiteOptions _externalApplicantSiteOptions = Guard.Against.Null(externalApplicantSiteOptions).Value;

    /// <inheritdoc/>
    public async Task<Result> WithdrawApplicationAsync(
        Guid applicationId, 
        WithdrawalReason withdrawalReason, 
        string linkToApplication,
        CancellationToken cancellationToken)
    {
        var userAccess = UserAccessModel.SystemUserAccessModel;

        var externalLinkToApplication =
            $"{_externalApplicantSiteOptions.BaseUrl}FellingLicenceApplication/ApplicationTaskList?applicationId={applicationId}";

        return await WithdrawApplicationAsync(
            applicationId,
            userAccess,
            [withdrawalReason],
            null,
            externalLinkToApplication,
            linkToApplication,
            cancellationToken)
            .ConfigureAwait(false);
    }
}