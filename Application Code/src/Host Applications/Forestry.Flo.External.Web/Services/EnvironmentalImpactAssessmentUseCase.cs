using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.External.Web.Infrastructure;
using Forestry.Flo.External.Web.Models.FellingLicenceApplication.EnvironmentalImpactAssessment;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Extensions;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.FileStorage.ResultModels;
using Forestry.Flo.Services.PropertyProfiles.Services;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;
using NodaTime;

namespace Forestry.Flo.External.Web.Services;

public class EnvironmentalImpactAssessmentUseCase(
    IAddDocumentService service,
    IUpdateFellingLicenceApplication updateFellingLicenceApplication,
    IAuditService<EnvironmentalImpactAssessmentUseCase> auditService,
    IRetrieveUserAccountsService retrieveUserAccountsService,
    IRetrieveWoodlandOwners retrieveWoodlandOwnersService,
    IGetFellingLicenceApplicationForExternalUsers getFellingLicenceApplicationServiceForExternalUsers,
    IGetPropertyProfiles getPropertyProfilesService,
    IOptions<EiaOptions> eiaOptions,
    IGetCompartments getCompartmentsService,
    IAgentAuthorityService agentAuthorityService,
    RequestContext requestContext,
    IClock clock,
    ILogger<EnvironmentalImpactAssessmentUseCase> logger)
    : ApplicationUseCaseCommon(
        retrieveUserAccountsService,
        retrieveWoodlandOwnersService,
        getFellingLicenceApplicationServiceForExternalUsers,
        getPropertyProfilesService,
        getCompartmentsService,
        agentAuthorityService,
        logger)
{
    private readonly IAuditService<EnvironmentalImpactAssessmentUseCase> _auditService = Guard.Against.Null(auditService);
    private readonly RequestContext _requestContext = Guard.Against.Null(requestContext);
    private readonly ILogger<EnvironmentalImpactAssessmentUseCase> _logger = Guard.Against.Null(logger);
    private readonly IAddDocumentService _service = Guard.Against.Null(service);

    /// <summary>
    /// Adds Environmental Impact Assessment (EIA) documents to a felling licence application for an external applicant.
    /// </summary>
    /// <param name="user">The external applicant user performing the upload.</param>
    /// <param name="fellingLicenceApplicationId">The unique identifier of the felling licence application.</param>
    /// <param name="eiaDocuments">The collection of EIA documents to be added.</param>
    /// <param name="modelState">The model state dictionary to which any user-facing errors will be added.</param>
    /// <param name="cancellationToken">A cancellation token for the async operation.</param>
    /// <returns>
    /// A <see cref="Result"/> indicating success or failure of the document upload operation.
    /// On failure, user-facing error messages are added to the <paramref name="modelState"/>.
    /// </returns>
    public async Task<Result> AddEiaDocumentsToApplicationAsync(
        ExternalApplicant user,
        Guid fellingLicenceApplicationId,
        FormFileCollection eiaDocuments,
        ModelStateDictionary modelState,
        CancellationToken cancellationToken)
    {
        if (!eiaDocuments.Any()) return Result.Success();

        _logger.LogInformation(
            "User {UserId} is adding {DocumentCount} EIA documents to felling licence application {ApplicationId}",
            user.UserAccountId,
            eiaDocuments.Count,
            fellingLicenceApplicationId);

        var editable = await base.EnsureApplicationIsEditable(fellingLicenceApplicationId, user, cancellationToken)
            .ConfigureAwait(false);

        if (editable.IsFailure)
        {
            _logger.LogError(
                "Failed to add EIA documents: {Error}",
                editable.Error);
            await CreateAuditForFailure( 
                fellingLicenceApplicationId, 
                ["Application not found or user cannot access it"],
                user,
                cancellationToken);
            return Result.Failure("Application not found or user cannot access it.");
        }

        var filesModel = ModelMapping.ToFileToStoreModel(eiaDocuments);

        var applicationResult = await GetFellingLicenceApplicationAsync(fellingLicenceApplicationId, user, cancellationToken);

        if (applicationResult.IsFailure)
        {
            _logger.LogError(
                "Failed to retrieve felling licence application for EIA document upload: {Error}",
                applicationResult.Error);
            await CreateAuditForFailure( 
                fellingLicenceApplicationId, 
                ["Application not found or user cannot access it"],
                user,
                cancellationToken);

            return Result.Failure("Application not found or user cannot access it.");
        }

        if (applicationResult.Value.FellingLicenceApplicationStepStatus.EnvironmentalImpactAssessmentStatus is null)
        {
            // set the step status to InProgress if it was previously NotStarted (null)
            var updateStepStatusesResult =
                await updateFellingLicenceApplication.UpdateEnvironmentalImpactAssessmentStatusAsync(
                    fellingLicenceApplicationId, 
                    false,
                    cancellationToken);

            if (updateStepStatusesResult.IsFailure)
            {
                _logger.LogError(
                    "Failed to update application step status for felling licence application {ApplicationId}: {Error}",
                    fellingLicenceApplicationId,
                    updateStepStatusesResult.Error);
                await CreateAuditForFailure(
                    fellingLicenceApplicationId, 
                    ["Could not update application step status."],
                    user,
                    cancellationToken);
                return Result.Failure("Could not update application step status.");
            }
        }

        var addDocumentRequest = new AddDocumentsExternalRequest
        {
            ActorType = ActorType.ExternalApplicant,
            ApplicationDocumentCount = applicationResult.Value.Documents!.Count(x => x.DeletionTimestamp is null),
            DocumentPurpose = DocumentPurpose.EiaAttachment,
            FellingApplicationId = fellingLicenceApplicationId,
            FileToStoreModels = filesModel,
            ReceivedByApi = false,
            UserAccountId = user.UserAccountId,
            VisibleToApplicant = true,
            VisibleToConsultee = true,
            WoodlandOwnerId = applicationResult.Value.WoodlandOwnerId
        };

        var result = await _service.AddDocumentsAsExternalApplicantAsync(
                addDocumentRequest,
                cancellationToken)
            .ConfigureAwait(false);

        if (result.IsFailure)
        {

            AddErrorsToModelState(modelState, result.Error.UserFacingFailureMessages);
            _logger.LogError(
                "Failed to add EIA documents to felling licence application {ApplicationId} for user {UserId}: {Errors}",
                fellingLicenceApplicationId,
                user.UserAccountId,
                string.Join("; ", result.Error.UserFacingFailureMessages));

            await CreateAuditForFailure(
                fellingLicenceApplicationId,
                result.Error.UserFacingFailureMessages,
                user,
                cancellationToken);
            return Result.Failure("Failed to add EIA documents to application.");
        }

        _logger.LogInformation(
            "Successfully added {DocumentCount} EIA documents to felling licence application {ApplicationId} for user {UserId}",
            result.Value.DocumentIds.Count(),
            fellingLicenceApplicationId,
            user.UserAccountId);

        await CreateAuditForSuccess(
            fellingLicenceApplicationId,
            result.Value,
            user,
            cancellationToken);
        return Result.Success();
    }

    /// <summary>
    /// Marks the Environmental Impact Assessment (EIA) as completed for a specified felling licence application.
    /// </summary>
    /// <param name="applicationId">The unique identifier of the felling licence application.</param>
    /// <param name="eiaRecord">The EIA record containing completion details.</param>
    /// <param name="user">The external applicant user performing the operation.</param>
    /// <param name="cancellationToken">A cancellation token for the async operation.</param>
    /// <returns>
    /// A <see cref="Result"/> indicating success or failure of the operation.
    /// On failure, a user-facing error message is returned.
    /// </returns>
    public async Task<Result> MarkEiaAsCompletedAsync(
        Guid applicationId,
        EnvironmentalImpactAssessmentRecord eiaRecord,
        ExternalApplicant user,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "User {UserId} is marking EIA as completed for felling licence application {ApplicationId}",
            user.UserAccountId,
            applicationId);

        var editable = await base.EnsureApplicationIsEditable(applicationId, user, cancellationToken);

        if (editable.IsFailure)
        {
            _logger.LogError(
                "Failed to mark EIA as completed: {Error}",
                editable.Error);
            await CreateAuditForCompletionFailure(
                applicationId, 
                editable.Error, 
                user,
                cancellationToken);
            return Result.Failure("Application not found or user cannot access it.");
        }

        var updateResult =
            await updateFellingLicenceApplication.UpdateEnvironmentalImpactAssessmentAsync(
                applicationId,
                eiaRecord,
                cancellationToken);

        if (updateResult.IsFailure)
        {
            _logger.LogError(
                "Failed to update EIA record for felling licence application {ApplicationId}: {Error}",
                applicationId,
                updateResult.Error);
            await CreateAuditForCompletionFailure(
                applicationId, 
                updateResult.Error, 
                user,
                cancellationToken);
            return Result.Failure("Could not update EIA record.");
        }

        _logger.LogInformation(
            "Successfully marked EIA as completed for felling licence application {ApplicationId} by user {UserId}",
            applicationId,
            user.UserAccountId);
        await CreateAuditForCompletionSuccess(
            applicationId, 
            user,
            cancellationToken);
        return Result.Success();
    }

    private static void AddErrorsToModelState(ModelStateDictionary modelState, IEnumerable<string> userFacingFailureMessage)
    {
        foreach (var message in userFacingFailureMessage)
        {
            modelState.AddModelError("eia-file-upload", message);
        }
    }

    private Task CreateAuditForCompletionSuccess(
        Guid applicationId,
        ExternalApplicant user,
        CancellationToken cancellationToken = default) =>
        _auditService.PublishAuditEventAsync(new AuditEvent(
                AuditEvents.ApplicantCompleteEiaSection,
                applicationId,
                user.UserAccountId,
                _requestContext,
                new { }),
            cancellationToken);

    private Task CreateAuditForCompletionFailure(
        Guid applicationId,
        string error,
        ExternalApplicant user,
        CancellationToken cancellationToken = default) =>
        _auditService.PublishAuditEventAsync(new AuditEvent(
                AuditEvents.ApplicantCompleteEiaSectionFailure,
                applicationId,
                user.UserAccountId,
                _requestContext,
                new
                {
                    error
                }),
            cancellationToken);

    private Task CreateAuditForSuccess(
        Guid applicationId,
        AddDocumentsSuccessResult result,
        ExternalApplicant user,
        CancellationToken cancellationToken = default) =>
        _auditService.PublishAuditEventAsync(new AuditEvent(
                AuditEvents.ApplicantUploadEiaDocumentsSuccess,
                applicationId,
                user.UserAccountId,
                _requestContext,
                new
                {
                    result.DocumentIds
                }),
            cancellationToken);

    private Task CreateAuditForFailure(
        Guid applicationId,
        IEnumerable<string> errors,
        ExternalApplicant user,
        CancellationToken cancellationToken = default)
    {
        var errorDictionary = errors
            .Select((msg, idx) => new { Key = $"Error{idx + 1}", Value = msg })
            .ToDictionary(x => x.Key, x => x.Value);

        return _auditService.PublishAuditEventAsync(new AuditEvent(
                AuditEvents.ApplicantUploadEiaDocumentsFailure,
                applicationId,
                user.UserAccountId,
                _requestContext,
                errorDictionary),
            cancellationToken);
    }
}
