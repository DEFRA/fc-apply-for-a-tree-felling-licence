using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Models.ExternalConsulteeInvite;
using Forestry.Flo.Internal.Web.Models.ExternalConsulteeReview;
using Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;
using Forestry.Flo.Internal.Web.Services.FellingLicenceApplication;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.Extensions;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Models.ExternalConsultee;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.InternalUsers.Services;
using NodaTime;

namespace Forestry.Flo.Internal.Web.Services.ExternalConsulteeReview;

public class ExternalConsulteeReviewUseCase: FellingLicenceApplicationUseCaseBase
{
    private readonly RequestContext _requestContext;
    private readonly IExternalConsulteeReviewService _externalConsulteeReviewService;
    private readonly IAuditService<ExternalConsulteeReviewUseCase> _auditService;
    private readonly ILogger<ExternalConsulteeReviewUseCase> _logger;
    private readonly IClock _clock;

    public ExternalConsulteeReviewUseCase(
        IUserAccountService internalUserAccountService,
        IRetrieveUserAccountsService externalUserAccountService,
        IFellingLicenceApplicationInternalRepository fellingLicenceApplicationInternalRepository,
        IRetrieveWoodlandOwners woodlandOwnerService,
        IAuditService<ExternalConsulteeReviewUseCase> auditService,
        IAgentAuthorityService agentAuthorityService,
        IExternalConsulteeReviewService externalConsulteeReviewService,
        IGetConfiguredFcAreas getConfiguredFcAreasService,
        ILogger<ExternalConsulteeReviewUseCase> logger,
        RequestContext requestContext,
        IClock clock) : base(
        internalUserAccountService,
        externalUserAccountService,
        fellingLicenceApplicationInternalRepository,
        woodlandOwnerService,
        agentAuthorityService,
        getConfiguredFcAreasService)
    {
        _requestContext = Guard.Against.Null(requestContext);
        _externalConsulteeReviewService = Guard.Against.Null(externalConsulteeReviewService);
        _auditService = Guard.Against.Null(auditService);
        _logger = Guard.Against.Null(logger);
        _clock = Guard.Against.Null(clock);
    }

    public async Task<Result<ExternalInviteLink>> ValidateAccessCodeAsync(
        Guid applicationId,
        Guid accessCode,
        string emailAddress,
        CancellationToken cancellationToken)
    {
        var externalAccessLink = await _externalConsulteeReviewService.VerifyAccessCodeAsync(
            applicationId, accessCode, emailAddress, cancellationToken);

        if (externalAccessLink.HasNoValue)
        {
            return Result.Failure<ExternalInviteLink>("Could not locate valid external access link");
        }
        
        return Result.Success(new ExternalInviteLink
        {
            ContactEmail = externalAccessLink.Value.ContactEmail,
            ExpiresTimeStamp = externalAccessLink.Value.ExpiresTimeStamp,
            Name = externalAccessLink.Value.ContactName,
            Purpose = externalAccessLink.Value.Purpose
        });
    }


    public async Task<Result<ExternalConsulteeReviewViewModel>> GetApplicationSummaryForConsulteeReviewAsync(
        Guid applicationId,
        ExternalInviteLink externalInviteLink,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "Attempting to retrieve information to display consultee comments page for application id {ApplicationId} and external access link id {ExternalAccessLinkId}",
            applicationId,
            externalInviteLink.Id);

        // TODO - might need a specific GetDetails method on an ExternalConsultee service class rather than using the basic get summary method
        var fla = await GetFellingLicenceDetailsAsync(applicationId, cancellationToken);
        if (fla.IsFailure)
        {
            _logger.LogError("Could not load application: {Error}", fla.Error);
            return Result.Failure<ExternalConsulteeReviewViewModel>(fla.Error);
        }


        var comments = await _externalConsulteeReviewService.RetrieveConsulteeCommentsForAuthorAsync(
            applicationId,
            externalInviteLink.ContactEmail,
            cancellationToken);
        var items = comments
            .OrderByDescending(x => x.CreatedTimestamp)
            .Select(x => new ActivityFeedItemModel
        {
            ActivityFeedItemType = ActivityFeedItemType.ConsulteeComment,
            CreatedTimestamp = x.CreatedTimestamp,
            FellingLicenceApplicationId = x.FellingLicenceApplicationId,
            VisibleToConsultee = true,
            Text = (x.ApplicableToSection.HasValue ? $"Regarding {x.ApplicableToSection.GetDisplayName()}:\n" : string.Empty) + x.Comment,
            Source = $"{x.AuthorName} ({x.AuthorContactEmail})"
        }).ToList();

        var feed = new ActivityFeedModel
        {
            ActivityFeedItemModels = items,
            ApplicationId = applicationId,
            ShowAddCaseNote = false,
            ActivityFeedTitle = "Your added comments",
            ShowFilters = false
        };

        var result = new ExternalConsulteeReviewViewModel
        {
            ApplicationReference = fla.Value.ApplicationReference,
            PropertyName = fla.Value.PropertyName,
            AddConsulteeComment = new AddConsulteeCommentModel
            {
                ApplicationId = applicationId,
                AuthorContactEmail = externalInviteLink.ContactEmail,
                AuthorName = externalInviteLink.Name,
                LinkExpiryDateTime = externalInviteLink.ExpiresTimeStamp
            },
            ActivityFeed = feed
        };

        return Result.Success(result);
    }

    public async Task<Result> AddConsulteeCommentAsync(
        AddConsulteeCommentModel model,
        CancellationToken cancellationToken)
    {
        Guard.Against.Null(model);
        
        _logger.LogDebug("Attempting to store new consultee comment for application with id {ApplicationId}", model.ApplicationId);

        var now = _clock.GetCurrentInstant().ToDateTimeUtc();

        var consulteeCommentModel = new ConsulteeCommentModel
        {
            FellingLicenceApplicationId = model.ApplicationId,
            AuthorContactEmail = model.AuthorContactEmail,
            CreatedTimestamp = now,
            ApplicableToSection = model.ApplicableToSection,
            AuthorName = model.AuthorName,
            Comment = model.Comment
        };
        var addCommentResult = await _externalConsulteeReviewService.AddCommentAsync(
            consulteeCommentModel, cancellationToken);

        if (addCommentResult.IsFailure)
        {
            _logger.LogError("Attempt to store new consultee comment failed");

            await _auditService.PublishAuditEventAsync(new AuditEvent(
                AuditEvents.AddConsulteeCommentFailure,
                model.ApplicationId,
                null,
                _requestContext,
                new
                {
                    addCommentResult.Error,
                    model.AuthorName,
                    model.AuthorContactEmail
                }), cancellationToken);

            return Result.Failure(addCommentResult.Error);
        }

        await _auditService.PublishAuditEventAsync(new AuditEvent(
            AuditEvents.AddConsulteeComment,
            model.ApplicationId,
            null,
            _requestContext,
            new
            {
                model.AuthorName,
                model.AuthorContactEmail
            }), cancellationToken);

        return Result.Success();
    }
}