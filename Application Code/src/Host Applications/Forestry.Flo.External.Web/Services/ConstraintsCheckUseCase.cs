using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.External.Web.Models.FellingLicenceApplication;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.PropertyProfiles.Services;
using NodaTime;
using RequestContext = Forestry.Flo.Services.Common.RequestContext;

namespace Forestry.Flo.External.Web.Services;

public class ConstraintsCheckUseCase(
    IFellingLicenceApplicationExternalRepository fellingLicenceApplicationExternalRepository,
    IRetrieveUserAccountsService retrieveUserAccountsService,
    IRetrieveWoodlandOwners retrieveWoodlandOwnersService,
    IGetFellingLicenceApplicationForExternalUsers getFellingLicenceApplicationServiceForExternalUsers,
    IGetPropertyProfiles getPropertyProfilesService,
    IGetCompartments getCompartmentsService,
    IAgentAuthorityService agentAuthorityService,
    RequestContext requestContext,
    IAuditService<ConstraintsCheckUseCase> auditService,
    IClock clock,
    ILogger<ConstraintsCheckUseCase> logger) 
    : ApplicationUseCaseCommon(
        retrieveUserAccountsService, 
        retrieveWoodlandOwnersService, 
        getFellingLicenceApplicationServiceForExternalUsers, 
        getPropertyProfilesService, 
        getCompartmentsService, 
        agentAuthorityService, 
        logger)
{
    private readonly ILogger<ConstraintsCheckUseCase> _logger = Guard.Against.Null(logger);

    private readonly IClock _clock = Guard.Against.Null(clock);

    private readonly IFellingLicenceApplicationExternalRepository _fellingLicenceApplicationRepository =
        Guard.Against.Null(fellingLicenceApplicationExternalRepository);

    private readonly RequestContext _requestContext = Guard.Against.Null(requestContext);

    private readonly IAuditService<ConstraintsCheckUseCase> _auditService = Guard.Against.Null(auditService);

    /// <summary>
    /// Sets Application Constraint Check Status
    /// </summary>
    /// <param name="user">An application user</param>
    /// <param name="constraintCheckModel">An Constraints check details model</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns></returns>
    public async Task<Result<Guid, UserDbErrorReason>> SetApplicationConstraintCheckAsync(
        ExternalApplicant user,
        ConstraintCheckModel constraintCheckModel,
        CancellationToken cancellationToken)
    {
        var isApplicationEditable = await base
            .EnsureApplicationIsEditable(constraintCheckModel.ApplicationId, user, cancellationToken)
            .ConfigureAwait(false);
        if (isApplicationEditable.IsFailure)
        {
            _logger.LogError("Application with id {ApplicationId} is not in editable state, error: {Error}",
                constraintCheckModel.ApplicationId,
                isApplicationEditable.Error);

            return Result.Failure<Guid, UserDbErrorReason>(UserDbErrorReason.General);
        }

        var applicationResult =
            await GetFellingLicenceApplicationAsync(constraintCheckModel.ApplicationId,
                user,
                cancellationToken);

        if (applicationResult.IsFailure || applicationResult.Value.LinkedPropertyProfile is null)
        {
            return Result.Failure<Guid, UserDbErrorReason>(UserDbErrorReason.NotFound);
        }

        var application = applicationResult.Value;

        application.NotRunningExternalLisReport = constraintCheckModel.NotRunningExternalLisReport.Value;
        application.FellingLicenceApplicationStepStatus.ConstraintCheckStatus = constraintCheckModel.StepComplete;
        if (constraintCheckModel.ExternalLisReportRun != null && constraintCheckModel.ExternalLisReportRun.Value)
        {
            application.ExternalLisAccessedTimestamp = _clock.GetCurrentInstant().ToDateTimeUtc();
        }

        _fellingLicenceApplicationRepository.Update(application);
        return await _fellingLicenceApplicationRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken)
            .Map(() => application.Id)
            .Tap(async appId =>
            {
                await _auditService.PublishAuditEventAsync(new AuditEvent(
                    AuditEvents.UpdateFellingLicenceApplication, appId, user.UserAccountId, _requestContext,
                    new { application.WoodlandOwnerId, Section = "Constraint Details" }), cancellationToken);
            })
            .OnFailure(async r =>
            {
                await _auditService.PublishAuditEventAsync(new AuditEvent(
                        AuditEvents.UpdateFellingLicenceApplicationFailure, constraintCheckModel.ApplicationId, user.UserAccountId, _requestContext,
                        new { application.WoodlandOwnerId, Section = "Constraint Details", Error = r.GetDescription() }),
                    cancellationToken);
                _logger.LogError(
                    "The Constraint details have not been updated due to reason {ErrorReason} for application id: {ApplicationId}",
                    r.GetDescription(), application.Id);
            });
    }

    /// <summary>
    /// Sets Application Constraint Check Status
    /// </summary>
    /// <param name="applicationId">The id of the application that a LIS report has been received for.</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns></returns>
    public async Task<Result> RecordReceivedLisReportAsync(
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        var applicationResult =
            await GetFellingLicenceApplicationServiceForExternalUsers.GetApplicationByIdAsync(
                applicationId,
                UserAccessModel.SystemUserAccessModel,
                cancellationToken);

        if (applicationResult.IsFailure || applicationResult.Value.LinkedPropertyProfile is null)
        {
            return Result.Failure("Could not find application to update");
        }

        var application = applicationResult.Value;

        application.NotRunningExternalLisReport = false;
        application.FellingLicenceApplicationStepStatus.ConstraintCheckStatus = true;

        _fellingLicenceApplicationRepository.Update(application);
        var result = await _fellingLicenceApplicationRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        if (result.IsFailure)
        {
            await _auditService.PublishAuditEventAsync(new AuditEvent(
                    AuditEvents.UpdateFellingLicenceApplicationFailure, applicationId, null, _requestContext,
                    new { application.WoodlandOwnerId, Section = "Constraint Details", Error = result.Error.GetDescription() }),
                cancellationToken);

            _logger.LogError(
                "The Constraint details have not been updated due to reason {ErrorReason} for application id: {ApplicationId}",
                result.Error.GetDescription(), application.Id);

            return Result.Failure("Failed to update application");
        }

        await _auditService.PublishAuditEventAsync(new AuditEvent(
            AuditEvents.UpdateFellingLicenceApplication, applicationId, null, _requestContext,
            new { application.WoodlandOwnerId, Section = "Constraint Details" }), cancellationToken);
        return Result.Success();
    }
}