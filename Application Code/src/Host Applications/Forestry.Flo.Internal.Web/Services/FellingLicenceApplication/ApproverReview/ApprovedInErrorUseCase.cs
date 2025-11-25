using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;
using Forestry.Flo.Internal.Web.Services.Interfaces;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.Services;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Configuration;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.FellingLicenceApplications.Services.WoodlandOfficerReviewSubstatuses;
using Forestry.Flo.Services.InternalUsers.Services;
using Microsoft.Extensions.Options;
using NodaTime;

namespace Forestry.Flo.Internal.Web.Services.FellingLicenceApplication.ApproverReview;

/// <summary>
/// Use case for handling the Approved In Error process of felling licence applications.
/// Mirrors the patterns used in ApproverReviewUseCase.
/// </summary>
public class ApprovedInErrorUseCase : FellingLicenceApplicationUseCaseBase, IApprovedInErrorUseCase
{
    private readonly IAgentAuthorityInternalService _agentAuthorityInternalService;
    private readonly IFellingLicenceApplicationInternalRepository _fellingLicenceApplicationInternalRepository;
    private readonly ILogger<FellingLicenceApplicationUseCase> _logger;
    private readonly IActivityFeedItemProvider _activityFeedItemProvider;
    private readonly IGetWoodlandOfficerReviewService _getWoodlandOfficerReviewService;
    private readonly IApprovedInErrorService _approvedInErrorService;
    private readonly IAuditService<FellingLicenceApplicationUseCase> _auditService;
    private readonly RequestContext _requestContext;
    private readonly FellingLicenceApplicationOptions _fellingLicenceApplicationOptions;
    private readonly IClock _clock;

    public ApprovedInErrorUseCase(
    IUserAccountService internalUserAccountService,
    IRetrieveUserAccountsService externalUserAccountService,
    IFellingLicenceApplicationInternalRepository fellingLicenceApplicationInternalRepository,
    IRetrieveWoodlandOwners woodlandOwnerService,
    IAuditService<FellingLicenceApplicationUseCase> auditService,
    IActivityFeedItemProvider activityFeedItemProvider,
    IAgentAuthorityService agentAuthorityService,
    IAgentAuthorityInternalService agentAuthorityInternalService,
    IGetWoodlandOfficerReviewService getWoodlandOfficerReviewService,
    IApprovedInErrorService approvedInErrorService,
    IGetConfiguredFcAreas getConfiguredFcAreasService,
    RequestContext requestContext,
    IOptions<FellingLicenceApplicationOptions> fellingLicenceApplicationOptions,
    IWoodlandOfficerReviewSubStatusService woodlandOfficerReviewSubStatusService,
    IClock clock,
    ILogger<FellingLicenceApplicationUseCase> logger)
    : base(internalUserAccountService,
    externalUserAccountService,
    fellingLicenceApplicationInternalRepository,
    woodlandOwnerService,
    agentAuthorityService,
    getConfiguredFcAreasService,
    woodlandOfficerReviewSubStatusService)
    {
        _agentAuthorityInternalService = Guard.Against.Null(agentAuthorityInternalService);
        Guard.Against.Null(internalUserAccountService);
        _fellingLicenceApplicationInternalRepository = Guard.Against.Null(fellingLicenceApplicationInternalRepository);
        _auditService = Guard.Against.Null(auditService);
        _requestContext = Guard.Against.Null(requestContext);
        _logger = Guard.Against.Null(logger);
        _activityFeedItemProvider = Guard.Against.Null(activityFeedItemProvider);
        _getWoodlandOfficerReviewService = Guard.Against.Null(getWoodlandOfficerReviewService);
        _approvedInErrorService = Guard.Against.Null(approvedInErrorService);
        _fellingLicenceApplicationOptions = fellingLicenceApplicationOptions.Value;
        _clock = Guard.Against.Null(clock);
    }

    /// <inheritdoc />
    public async Task<Maybe<ApprovedInErrorViewModel>> RetrieveApprovedInErrorAsync(
    Guid applicationId,
    InternalUser viewingUser,
    CancellationToken cancellationToken)
    {
        var application = await _fellingLicenceApplicationInternalRepository.GetAsync(applicationId, cancellationToken);
        if (!application.HasValue)
        {
            _logger.LogError("Felling licence application not found, application id: {ApplicationId}", applicationId);
            return Maybe<ApprovedInErrorViewModel>.None;
        }

        var approvedInError = await _approvedInErrorService.GetApprovedInErrorAsync(applicationId, cancellationToken);

        var model = new ApprovedInErrorViewModel
        {
            Id = application.Value.Id,
            ViewingUser = viewingUser,
            ApplicationId = application.Value.Id,
            PreviousReference = application.Value.ApplicationReference,
            ReasonExpiryDate = approvedInError.HasValue && approvedInError.Value.ReasonExpiryDate,
            ReasonSupplementaryPoints = approvedInError.HasValue && approvedInError.Value.ReasonSupplementaryPoints,
            ReasonOther = approvedInError.HasValue && approvedInError.Value.ReasonOther,
            CaseNote = approvedInError.HasValue ? approvedInError.Value.CaseNote : null
        };

        var summary = await ExtractApplicationSummaryAsync(application.Value, cancellationToken);
        if (summary.IsFailure)
        {
            _logger.LogError("Application summary cannot be extracted, application id: {ApplicationId}, error {Error}", application.Value.Id, summary.Error);
            return Maybe<ApprovedInErrorViewModel>.None;
        }
        model.FellingLicenceApplicationSummary = summary.Value;

        var creator = await GetSubmittingUserAsync(application.Value.CreatedById, cancellationToken);
        if (creator.IsFailure)
        {
            _logger.LogError("Unable to retrieve the details of the external user who submitted the application, application id: {ApplicationId}, external user: {createdById} , error {Error}", application.Value.Id, application.Value.CreatedById, creator.Error);
            return Maybe<ApprovedInErrorViewModel>.None;
        }

        return Maybe<ApprovedInErrorViewModel>.From(model);
    }

    /// <inheritdoc />
    public async Task<Result> ConfirmApprovedInErrorAsync(
    ApprovedInErrorModel model,
    InternalUser user,
    CancellationToken cancellationToken)
    {
        if (user.AccountType is not AccountTypeInternal.AccountAdministrator)
        {
            _logger.LogError("User {UserAccountId} is not an account administrator and cannot mark an application as approved in error.", user.UserAccountId);
            return Result.Failure("You do not have permission to mark an application as approved in error.");
        }

        _logger.LogDebug("Attempting to store Approved In Error details for application with id {ApplicationId}", model.ApplicationId);

        var updateResult = await _approvedInErrorService.SetToApprovedInErrorAsync(
            model.ApplicationId,
            model,
            user.UserAccountId!.Value,
            cancellationToken);

        if (updateResult.IsSuccess)
        {
            await _auditService.PublishAuditEventAsync(new AuditEvent(
                AuditEvents.ConfirmApprovedInError,
                model.ApplicationId,
                user.UserAccountId,
                _requestContext,
                new { model.ReasonExpiryDate, model.ReasonSupplementaryPoints, model.ReasonOther, model.CaseNote, model.PreviousReference }),
                cancellationToken);

            return Result.Success();
        }

        _logger.LogError("Failed to update Approved In Error with error {Error}", updateResult.Error);

        await _auditService.PublishAuditEventAsync(new AuditEvent(
            AuditEvents.ConfirmApprovedInErrorFailure,
            model.ApplicationId,
            user.UserAccountId,
            _requestContext,
            new { updateResult.Error, model.ReasonExpiryDate, model.ReasonSupplementaryPoints, model.ReasonOther, model.CaseNote, model.PreviousReference }),
            cancellationToken);

        return Result.Failure(updateResult.Error);
    }

    private static Maybe<DocumentModel> GetMostRecentDocumentOfType(IList<Document>? documents, DocumentPurpose purpose)
    {
        if (documents == null) return Maybe<DocumentModel>.None;

        var lisDocument = documents.OrderByDescending(x => x.CreatedTimestamp)
            .FirstOrDefault(x => x.Purpose == purpose);

        if (lisDocument == null) return Maybe<DocumentModel>.None;

        var documentModel = ModelMapping.ToDocumentModel(lisDocument);
        return Maybe<DocumentModel>.From(documentModel);
    }
}
