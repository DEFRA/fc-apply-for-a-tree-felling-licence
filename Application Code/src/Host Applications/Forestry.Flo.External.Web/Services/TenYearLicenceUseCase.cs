using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.External.Web.Models.FellingLicenceApplication;
using Forestry.Flo.External.Web.Models.FellingLicenceApplication.TenYearLicenceApplications;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.Extensions;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Extensions;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.PropertyProfiles.Services;

namespace Forestry.Flo.External.Web.Services;

/// <summary>
/// Use case class for functionality relating to the ten-year licence check in the
/// felling licence application process.
/// </summary>
public class TenYearLicenceUseCase(
        IRetrieveUserAccountsService retrieveUserAccountsService, 
        IRetrieveWoodlandOwners retrieveWoodlandOwnersService, 
        IGetFellingLicenceApplicationForExternalUsers getFellingLicenceApplicationServiceForExternalUsers, 
        IGetPropertyProfiles getPropertyProfilesService, 
        IGetCompartments getCompartmentsService, 
        IAgentAuthorityService agentAuthorityService, 
        IUpdateFellingLicenceApplicationForExternalUsers updateFellingLicenceApplicationService,
        IAuditService<TenYearLicenceUseCase> auditService,
        RequestContext requestContext,
        ILogger<TenYearLicenceUseCase> logger) 
        : ApplicationUseCaseCommon(
            retrieveUserAccountsService, 
            retrieveWoodlandOwnersService, 
            getFellingLicenceApplicationServiceForExternalUsers, 
            getPropertyProfilesService, 
            getCompartmentsService, 
            agentAuthorityService, 
            logger)
{

    private readonly ILogger<TenYearLicenceUseCase> _logger = Guard.Against.Null(logger);
    private readonly IUpdateFellingLicenceApplicationForExternalUsers _updateFellingLicenceApplicationService =
        Guard.Against.Null(updateFellingLicenceApplicationService);
    private readonly IAuditService<TenYearLicenceUseCase> _auditService = Guard.Against.Null(auditService);
    private readonly RequestContext _requestContext = Guard.Against.Null(requestContext);

    /// <summary>
    /// Gets the view model for the ten-year licence check in the felling licence application process.
    /// </summary>
    /// <param name="user">The external user viewing the application.</param>
    /// <param name="applicationId">The ID of the application being viewed.</param>
    /// <param name="returnToApplicationSummary">A flag to indicate whether to return to the summary when the page is completed.</param>
    /// <param name="fromDataImport">A flag to indicate whether the application flow came from the data import process.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="TenYearLicenceApplicationViewModel"/> representing the ten-year licence check task on
    /// the application process.</returns>
    public async Task<Result<TenYearLicenceApplicationViewModel>> GetTenYearLicenceApplicationViewModel(
        ExternalApplicant user,
        Guid applicationId,
        bool returnToApplicationSummary,
        bool fromDataImport,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);

        var userAccess = await GetUserAccessModelAsync(user, cancellationToken);
        if (userAccess.IsFailure)
        {
            _logger.LogError("Unable to retrieve user access for user with id {UserId}", user.UserAccountId.Value);
            return userAccess.ConvertFailure<TenYearLicenceApplicationViewModel>();
        }

        var applicationResult = await GetFellingLicenceApplicationServiceForExternalUsers
            .GetApplicationByIdAsync(applicationId, userAccess.Value, cancellationToken);

        if (applicationResult.IsFailure)
        {
            _logger.LogError("Unable to retrieve application with id {ApplicationId}", applicationId);
            return applicationResult.ConvertFailure<TenYearLicenceApplicationViewModel>();
        }

        var applicationSummary = await GetApplicationSummaryAsync(applicationResult.Value, user, cancellationToken);
        if (applicationSummary.IsFailure)
        {
            _logger.LogError("Unable to retrieve application summary for application with id {ApplicationId}", applicationId);
            return applicationSummary.ConvertFailure<TenYearLicenceApplicationViewModel>();
        }

        var currentStatus = applicationResult.Value.GetCurrentStatus();

        var result = new TenYearLicenceApplicationViewModel
        {
            ApplicationId = applicationId,
            ApplicationReference = applicationResult.Value.ApplicationReference,
            FellingLicenceStatus = currentStatus,
            StepComplete = applicationResult.Value.FellingLicenceApplicationStepStatus.TenYearLicenceStepStatus,
            ApplicationSummary = applicationSummary.Value,
            StepRequiredForApplication = user.IsFcUser,
            ReturnToApplicationSummary = returnToApplicationSummary,
            FromDataImport = fromDataImport,
            IsForTenYearLicence = applicationResult.Value.IsForTenYearLicence,
            WoodlandManagementPlanReference = applicationResult.Value.WoodlandManagementPlanReference
        };

        return result;
    }

    /// <summary>
    /// Attempts to update the ten-year licence status for the given application.
    /// </summary>
    /// <param name="applicationId">The ID of the application to update.</param>
    /// <param name="user">The user performing the update.</param>
    /// <param name="isForTenYearLicence">A flag indicating whether the application is for a ten-year licence.</param>
    /// <param name="woodlandManagementPlanReference">The reference for the WMP related to the application, if applicable.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Result"/> struct indicating success or failure.</returns>
    public async Task<Result> UpdateTenYearLicenceStatusAsync(
        Guid applicationId,
        ExternalApplicant user,
        bool isForTenYearLicence,
        string? woodlandManagementPlanReference,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Attempting to update ten-year licence status for application id {ApplicationId}", applicationId);

        var uam = await GetUserAccessModelAsync(user, cancellationToken);

        if (uam.IsFailure)
        {
            _logger.LogError("Unable to retrieve user access for user with id {UserId}", user.UserAccountId.Value);
            return uam.ConvertFailure();
        }

        var updateResult = await _updateFellingLicenceApplicationService.UpdateTenYearLicenceStatusAsync(
            applicationId, uam.Value, isForTenYearLicence, woodlandManagementPlanReference, cancellationToken);

        if (updateResult.IsSuccess)
        {
            _logger.LogDebug("Successfully updated ten-year licence status for application with id {ApplicationId}", applicationId);
            if (isForTenYearLicence)
            {
                await CreateAuditForUpdateSuccess(applicationId, user, isForTenYearLicence, cancellationToken);
            }
            else
            {
                await CreateAuditForCompletionSuccess(applicationId, user, isForTenYearLicence, cancellationToken);
            }

            return updateResult;
        }

        _logger.LogError("Failed to update ten-year licence status for application with id {ApplicationId}. Error: {Error}",
            applicationId, updateResult.Error);

        if (isForTenYearLicence)
        {
            await CreateAuditForUpdateFailure(applicationId, user, isForTenYearLicence, updateResult.Error, cancellationToken);
        }
        else
        {
            await CreateAuditForCompletionFailure(applicationId, user, isForTenYearLicence, updateResult.Error, cancellationToken);
        }

        return updateResult;
    }

    /// <summary>
    /// Marks the ten-year licence step as complete for the given application.
    /// </summary>
    /// <param name="applicationId">The ID of the application to update.</param>
    /// <param name="user">The user performing the update.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Result"/> struct indicating success or failure.</returns>
    public async Task<Result> CompleteTenYearLicenceStepAsync(
        Guid applicationId,
        ExternalApplicant user,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Attempting to complete ten-year licence step status for application id {ApplicationId}", applicationId);

        var uam = await GetUserAccessModelAsync(user, cancellationToken);

        if (uam.IsFailure)
        {
            _logger.LogError("Unable to retrieve user access for user with id {UserId}", user.UserAccountId.Value);
            return uam.ConvertFailure();
        }

        var completeResult = await _updateFellingLicenceApplicationService.CompleteTenYearLicenceStepAsync(
            applicationId, uam.Value, cancellationToken);

        if (completeResult.IsSuccess)
        {
            _logger.LogDebug("Successfully completed ten-year licence step status for application id {ApplicationId}", applicationId);

            await CreateAuditForCompletionSuccess(applicationId, user, true, cancellationToken);

            return completeResult;
        }

        _logger.LogError("Failed to complete ten-year licence step status for application with id {ApplicationId}. Error: {Error}",
            applicationId, completeResult.Error);

        await CreateAuditForCompletionFailure(applicationId, user, true, completeResult.Error, cancellationToken);

        return completeResult;
    }

    /// <summary>
    /// Gets the view model for adding WMP documents in the felling licence application process.
    /// </summary>
    /// <param name="user">The external user viewing the application.</param>
    /// <param name="applicationId">The ID of the application being viewed.</param>
    /// <param name="returnToApplicationSummary">A flag to indicate whether to return to the summary when the page is completed.</param>
    /// <param name="fromDataImport">A flag to indicate whether the application flow came from the data import process.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="WmpDocumentsViewModel"/> representing the WMP document upload page for the
    /// application process.</returns>
    public async Task<Result<WmpDocumentsViewModel>> GetWmpDocumentsViewModel(
        ExternalApplicant user,
        Guid applicationId,
        bool returnToApplicationSummary,
        bool fromDataImport,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);

        var userAccess = await GetUserAccessModelAsync(user, cancellationToken);
        if (userAccess.IsFailure)
        {
            _logger.LogError("Unable to retrieve user access for user with id {UserId}", user.UserAccountId.Value);
            return userAccess.ConvertFailure<WmpDocumentsViewModel>();
        }

        var applicationResult = await GetFellingLicenceApplicationServiceForExternalUsers
            .GetApplicationByIdAsync(applicationId, userAccess.Value, cancellationToken);

        if (applicationResult.IsFailure)
        {
            _logger.LogError("Unable to retrieve application with id {ApplicationId}", applicationId);
            return applicationResult.ConvertFailure<WmpDocumentsViewModel>();
        }

        var applicationSummary = await GetApplicationSummaryAsync(applicationResult.Value, user, cancellationToken);
        if (applicationSummary.IsFailure)
        {
            _logger.LogError("Unable to retrieve application summary for application with id {ApplicationId}", applicationId);
            return applicationSummary.ConvertFailure<WmpDocumentsViewModel>();
        }

        var relevantDocuments = applicationResult.Value.Documents?
            .Where(d => d.Purpose == DocumentPurpose.WmpDocument && d.DeletionTimestamp.HasNoValue())
            .ToList() ?? [];

        var currentStatus = applicationResult.Value.GetCurrentStatus();

        var result = new WmpDocumentsViewModel
        {
            ApplicationId = applicationId,
            ApplicationReference = applicationResult.Value.ApplicationReference,
            Documents = ModelMapping.ToDocumentsModelForApplicantView(relevantDocuments),
            FellingLicenceStatus = currentStatus,
            StepComplete = applicationResult.Value.FellingLicenceApplicationStepStatus.TenYearLicenceStepStatus,
            ApplicationSummary = applicationSummary.Value,
            StepRequiredForApplication = user.IsFcUser,
            ReturnToApplicationSummary = returnToApplicationSummary,
            FromDataImport = fromDataImport,
            AddSupportingDocumentModel = new AddSupportingDocumentModel
            {
                ReturnToApplicationSummary = returnToApplicationSummary,
                FromDataImport = fromDataImport,
                DocumentCount = 0,  // don't limit documents for WMP upload
                FellingLicenceApplicationId = applicationId,
                Purpose = DocumentPurpose.WmpDocument
            }
        };

        return result;
    }

    /// <summary>
    /// Checks if the given application is a ten-year licence application.
    /// </summary>
    /// <param name="user">The external user viewing the application.</param>
    /// <param name="applicationId">The ID of the application being viewed.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A flag to indicate if the application is marked as being for a ten-year licence.</returns>
    public async Task<Result<bool>> IsTenYearLicenceApplicationAsync(
        ExternalApplicant user,
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);

        var userAccess = await GetUserAccessModelAsync(user, cancellationToken);
        if (userAccess.IsFailure)
        {
            _logger.LogError("Unable to retrieve user access for user with id {UserId}", user.UserAccountId.Value);
            return userAccess.ConvertFailure<bool>();
        }

        var applicationResult = await GetFellingLicenceApplicationServiceForExternalUsers
            .GetApplicationByIdAsync(applicationId, userAccess.Value, cancellationToken);

        if (applicationResult.IsFailure)
        {
            _logger.LogError("Unable to retrieve application with id {ApplicationId}", applicationId);
            return applicationResult.ConvertFailure<bool>();
        }

        return applicationResult.Value.IsForTenYearLicence is true;
    }

    private Task CreateAuditForUpdateSuccess(
        Guid applicationId,
        ExternalApplicant user,
        bool isForTenYearLicence,
        CancellationToken cancellationToken = default) =>
        _auditService.PublishAuditEventAsync(new AuditEvent(
                AuditEvents.TenYearLicenceStepUpdated,
                applicationId,
                user.UserAccountId,
                _requestContext,
                new { isForTenYearLicence }),
            cancellationToken);

    private Task CreateAuditForUpdateFailure(
        Guid applicationId,
        ExternalApplicant user,
        bool isForTenYearLicence,
        string error,
        CancellationToken cancellationToken = default) =>
        _auditService.PublishAuditEventAsync(new AuditEvent(
                AuditEvents.TenYearLicenceStepUpdatedFailure,
                applicationId,
                user.UserAccountId,
                _requestContext,
                new { isForTenYearLicence, error }),
            cancellationToken);

    private Task CreateAuditForCompletionSuccess(
        Guid applicationId,
        ExternalApplicant user,
        bool isForTenYearLicence,
        CancellationToken cancellationToken = default) =>
        _auditService.PublishAuditEventAsync(new AuditEvent(
                AuditEvents.TenYearLicenceStepCompleted,
                applicationId,
                user.UserAccountId,
                _requestContext,
                new { isForTenYearLicence }),
            cancellationToken);

    private Task CreateAuditForCompletionFailure(
        Guid applicationId,
        ExternalApplicant user,
        bool isForTenYearLicence,
        string error,
        CancellationToken cancellationToken = default) =>
        _auditService.PublishAuditEventAsync(new AuditEvent(
                AuditEvents.TenYearLicenceStepCompletedFailure,
                applicationId,
                user.UserAccountId,
                _requestContext,
                new { isForTenYearLicence, error }),
            cancellationToken);


}