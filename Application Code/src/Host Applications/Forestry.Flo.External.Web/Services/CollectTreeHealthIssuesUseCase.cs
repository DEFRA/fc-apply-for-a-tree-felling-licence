using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.External.Web.Models.FellingLicenceApplication;
using Forestry.Flo.External.Web.Models.FellingLicenceApplication.PawsDesignations;
using Forestry.Flo.External.Web.Models.FellingLicenceApplication.TreeHealth;
using Forestry.Flo.External.Web.Services.Interfaces;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Configuration;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Extensions;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.PropertyProfiles.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Forestry.Flo.External.Web.Services;

public class CollectTreeHealthIssuesUseCase(
    IRetrieveUserAccountsService retrieveUserAccountsService,
    IRetrieveWoodlandOwners retrieveWoodlandOwnersService,
    IGetFellingLicenceApplicationForExternalUsers getFellingLicenceApplicationServiceForExternalUsers,
    IGetPropertyProfiles getPropertyProfilesService,
    IGetCompartments getCompartmentsService,
    IAgentAuthorityService agentAuthorityService,
    IUpdateFellingLicenceApplicationForExternalUsers updateFellingLicenceApplicationService,
    IAuditService<CollectTreeHealthIssuesUseCase> auditService,
    IOptions<TreeHealthOptions> treeHealthOptions,
    IAddDocumentService addDocumentService,
    RequestContext requestContext,
    ILogger<CollectTreeHealthIssuesUseCase> logger)
    : ApplicationUseCaseCommon(
            retrieveUserAccountsService,
            retrieveWoodlandOwnersService,
            getFellingLicenceApplicationServiceForExternalUsers,
            getPropertyProfilesService,
            getCompartmentsService,
            agentAuthorityService,
            logger
        ),
        ICollectTreeHealthIssuesUseCase
{
    private readonly ILogger<CollectTreeHealthIssuesUseCase> _logger = logger ?? new NullLogger<CollectTreeHealthIssuesUseCase>();
    private readonly IUpdateFellingLicenceApplicationForExternalUsers _updateFellingLicenceApplicationService = Guard.Against.Null(updateFellingLicenceApplicationService);
    private readonly IAuditService<CollectTreeHealthIssuesUseCase> _auditService = Guard.Against.Null(auditService);
    private readonly TreeHealthOptions _treeHealthOptions = Guard.Against.Null(treeHealthOptions)?.Value;
    private readonly RequestContext _requestContext = Guard.Against.Null(requestContext);
    private readonly IAddDocumentService _addDocumentService = Guard.Against.Null(addDocumentService);

    public async Task<Result<TreeHealthIssuesViewModel>> GetTreeHealthIssuesViewModelAsync(
        Guid applicationId, 
        ExternalApplicant user, 
        CancellationToken cancellationToken)
    {
        // get user access model
        var userAccess = await GetUserAccessModelAsync(user, cancellationToken);
        if (userAccess.IsFailure)
        {
            _logger.LogError("Unable to retrieve user access for user with id {UserId}", user.UserAccountId.Value);
            return userAccess.ConvertFailure<TreeHealthIssuesViewModel>();
        }

        // get application
        var applicationResult = await GetFellingLicenceApplicationServiceForExternalUsers
            .GetApplicationByIdAsync(applicationId, userAccess.Value, cancellationToken);

        if (applicationResult.IsFailure)
        {
            _logger.LogError("Unable to retrieve application with id {ApplicationId}", applicationId);
            return applicationResult.ConvertFailure<TreeHealthIssuesViewModel>();
        }

        // get application summary
        var applicationSummary = await GetApplicationSummaryAsync(applicationResult.Value, user, cancellationToken);
        if (applicationSummary.IsFailure)
        {
            _logger.LogError("Unable to retrieve application summary for application with id {ApplicationId}", applicationId);
            return applicationSummary.ConvertFailure<TreeHealthIssuesViewModel>();
        }

        // populate model of tree health issues values
        var selectedIssues = applicationResult.Value.TreeHealthIssues.Select(x => x.ToLower());
        var selections = _treeHealthOptions.TreeHealthIssues
            .OrderBy(x => x)
            .Union(applicationResult.Value.TreeHealthIssues)
            .DistinctBy(x => x.ToLower())
            .ToDictionary(item => item, item => selectedIssues.Contains(item.ToLower()));

        var treeHealthIssues = new TreeHealthIssuesModel
        {
            NoTreeHealthIssues = applicationResult.Value.IsTreeHealthIssue is false,
            TreeHealthIssueSelections = selections,
            OtherTreeHealthIssue = applicationResult.Value.TreeHealthIssueOther is true,
            OtherTreeHealthIssueDetails = applicationResult.Value.TreeHealthIssueOtherDetails
        };

        // populate view model to return
        var result = new TreeHealthIssuesViewModel
        {
            ApplicationId = applicationId,
            ApplicationReference = applicationResult.Value.ApplicationReference,
            ApplicationSummary = applicationSummary.Value,
            FellingLicenceStatus = applicationResult.Value.GetCurrentStatus(),
            StepRequiredForApplication = true,
            StepComplete = applicationResult.Value.FellingLicenceApplicationStepStatus.TreeHealthIssuesStatus,
            TreeHealthIssues = treeHealthIssues,
            TreeHealthDocuments = ModelMapping.ToDocumentsModelForApplicantView(
                applicationResult.Value.Documents?.Where(x =>
                        x.Purpose is DocumentPurpose.TreeHealthAttachment &&
                        x.DeletionTimestamp is null)
                    .ToList()).ToArray()
        };

        return Result.Success(result);
    }

    public async Task<UnitResult<SubmitTreeHealthIssuesError>> SubmitTreeHealthIssuesAsync(
        Guid applicationId, 
        ExternalApplicant user, 
        TreeHealthIssuesViewModel model,
        FormFileCollection treeHealthFiles, 
        CancellationToken cancellationToken)
    {
        
        var uam = await GetUserAccessModelAsync(user, cancellationToken);

        if (uam.IsFailure)
        {
            _logger.LogError("Unable to retrieve user access for user with id {UserId}", user.UserAccountId.Value);

            await CreateAuditForFailure(applicationId, [uam.Error], user, cancellationToken);

            return UnitResult.Failure(SubmitTreeHealthIssuesError.StoreTreeHealthIssues);
        }

        var isEditable = await GetFellingLicenceApplicationServiceForExternalUsers.GetIsEditable(
            applicationId,
            uam.Value,
            cancellationToken);

        if (isEditable.IsFailure || !isEditable.Value)
        {
            _logger.LogError(
                "Failed to submit tree health issues: {Error}",
                isEditable.IsFailure ? isEditable.Error : "Application is not currently editable");
            await CreateAuditForFailure(
                applicationId,
                ["Application not found or user cannot access it"],
            user,
            cancellationToken);

            return UnitResult.Failure(SubmitTreeHealthIssuesError.StoreTreeHealthIssues);
        }

        // don't store the files if there's no tree health issues
        if (!model.TreeHealthIssues.NoTreeHealthIssues)
        {
            var storeDocumentsResult = await StoreTreeHealthDocuments(
                applicationId,
                user,
                uam.Value,
                treeHealthFiles,
                cancellationToken);

            if (storeDocumentsResult.IsFailure)
            {
                // already logged and audited in StoreTreeHealthDocuments
                return UnitResult.Failure(SubmitTreeHealthIssuesError.DocumentUpload);
            }
        }

        var updateApplicationResult = await _updateFellingLicenceApplicationService
            .UpdateApplicationTreeHealthIssuesDataAsync(
                applicationId, uam.Value, model.TreeHealthIssues, cancellationToken);

        if (updateApplicationResult.IsSuccess)
        {
            await CreateAuditForSuccess(
                applicationId, !model.TreeHealthIssues.NoTreeHealthIssues, user, cancellationToken);
            return UnitResult.Success<SubmitTreeHealthIssuesError>();
        }
        
        _logger.LogError(
            "Failed to update tree health issues for application {ApplicationId}: {Error}",
            applicationId,
            updateApplicationResult.Error);

        await CreateAuditForFailure(applicationId, [updateApplicationResult.Error], user, cancellationToken);
        return UnitResult.Failure(SubmitTreeHealthIssuesError.StoreTreeHealthIssues);
    }

    private async Task<Result> StoreTreeHealthDocuments(
        Guid applicationId,
        ExternalApplicant user,
        UserAccessModel userAccessModel,
        FormFileCollection treeHealthFiles,
        CancellationToken cancellationToken)
    {
        if (!treeHealthFiles.Any())
        {
            return Result.Success();
        }

        var filesModel = ModelMapping.ToFileToStoreModel(treeHealthFiles);

        var applicationResult = await GetFellingLicenceApplicationServiceForExternalUsers.GetApplicationByIdAsync(
            applicationId, userAccessModel, cancellationToken);

        if (applicationResult.IsFailure)
        {
            _logger.LogError(
                "Failed to retrieve felling licence application for tree health document upload: {Error}",
                applicationResult.Error);
            await CreateAuditForDocumentFailure(
                applicationId,
                ["Application not found or user cannot access it"],
                user,
                cancellationToken);

            return Result.Failure("Application not found or user cannot access it.");
        }

        var documentsCount = applicationResult.Value.Documents!.Count(x =>
            x.DeletionTimestamp is null
            && x.Purpose is DocumentPurpose.EiaAttachment or DocumentPurpose.Attachment or DocumentPurpose.TreeHealthAttachment);

        var addDocumentRequest = new AddDocumentsExternalRequest
        {
            ActorType = ActorType.ExternalApplicant,
            ApplicationDocumentCount = documentsCount,
            DocumentPurpose = DocumentPurpose.TreeHealthAttachment,
            FellingApplicationId = applicationId,
            FileToStoreModels = filesModel,
            ReceivedByApi = false,
            UserAccountId = user.UserAccountId,
            VisibleToApplicant = true,
            VisibleToConsultee = true,
            WoodlandOwnerId = applicationResult.Value.WoodlandOwnerId
        };

        var result = await _addDocumentService.AddDocumentsAsExternalApplicantAsync(
                addDocumentRequest,
                cancellationToken)
            .ConfigureAwait(false);

        if (result.IsSuccess)
        {
            _logger.LogDebug(
                "Successfully added {DocumentCount} tree health documents to application {ApplicationId}",
                treeHealthFiles.Count,
                applicationId);

            await CreateAuditForDocumentSuccess(applicationId, treeHealthFiles.Count, user, cancellationToken);
            return Result.Success();
        }

        _logger.LogError(
            "Failed to add tree health documents to application {ApplicationId}: {Error}",
            applicationId,
            result.Error);
        await CreateAuditForDocumentFailure(
            applicationId,
            result.Error.UserFacingFailureMessages,
            user,
            cancellationToken);

        return Result.Failure("Failed to upload provided documents");
    }


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
                AuditEvents.TreeHealthIssuesSubmittedFailure,
                applicationId,
                user.UserAccountId,
                _requestContext,
                errorDictionary),
            cancellationToken);
    }

    private Task CreateAuditForSuccess(
        Guid applicationId,
        bool isTreeHealthIssue,
        ExternalApplicant user,
        CancellationToken cancellationToken = default)
    {
        return _auditService.PublishAuditEventAsync(new AuditEvent(
                AuditEvents.TreeHealthIssuesSubmitted,
                applicationId,
                user.UserAccountId,
                _requestContext,
                new {isTreeHealthIssue}),
            cancellationToken);
    }

    private Task CreateAuditForDocumentFailure(
        Guid applicationId,
        IEnumerable<string> errors,
        ExternalApplicant user,
        CancellationToken cancellationToken = default)
    {
        var errorDictionary = errors
            .Select((msg, idx) => new { Key = $"Error{idx + 1}", Value = msg })
            .ToDictionary(x => x.Key, x => x.Value);

        return _auditService.PublishAuditEventAsync(new AuditEvent(
                AuditEvents.TreeHealthIssuesDocumentsUploadedFailure,
                applicationId,
                user.UserAccountId,
                _requestContext,
                errorDictionary),
            cancellationToken);
    }

    private Task CreateAuditForDocumentSuccess(
        Guid applicationId,
        int documentsCount,
        ExternalApplicant user,
        CancellationToken cancellationToken = default)
    {
        return _auditService.PublishAuditEventAsync(new AuditEvent(
                AuditEvents.TreeHealthIssuesDocumentsUploaded,
                applicationId,
                user.UserAccountId,
                _requestContext,
                new {documentsCount}),
            cancellationToken);
    }
}