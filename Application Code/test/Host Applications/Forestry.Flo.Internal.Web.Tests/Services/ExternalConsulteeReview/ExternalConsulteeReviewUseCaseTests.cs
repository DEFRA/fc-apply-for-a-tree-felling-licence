using System.Reflection;
using System.Text;
using AutoFixture;
using AutoFixture.Xunit2;
using CSharpFunctionalExtensions;
using FluentEmail.Core;
using Forestry.Flo.Internal.Web.Models.ExternalConsulteeInvite;
using Forestry.Flo.Internal.Web.Models.ExternalConsulteeReview;
using Forestry.Flo.Internal.Web.Services.ExternalConsulteeReview;
using Forestry.Flo.Services.Applicants.Entities.UserAccount;
using Forestry.Flo.Services.Applicants.Models;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Models.ExternalConsultee;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.InternalUsers.Services;
using Forestry.Flo.Tests.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NodaTime;
using System.Text.Json;
using Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;
using Forestry.Flo.Services.FileStorage.ResultModels;

namespace Forestry.Flo.Internal.Web.Tests.Services.ExternalConsulteeReview;

public class ExternalConsulteeReviewUseCaseTests
{
    private readonly Mock<IUserAccountService> _mockInternalUserAccountService = new();
    private readonly Mock<IRetrieveUserAccountsService> _mockExternalUserAccountService = new();
    private readonly Mock<IFellingLicenceApplicationInternalRepository> _mockFellingLicenceApplicationInternalRepository = new();
    private readonly Mock<IRetrieveWoodlandOwners> _mockWoodlandOwnerService = new();
    private readonly Mock<IAuditService<ExternalConsulteeReviewUseCase>> _mockAuditService = new();
    private readonly Mock<IExternalConsulteeReviewService> _mockExternalConsulteeReviewService = new();
    private readonly Mock<IAgentAuthorityService> _mockAgentAuthorityService = new();
    private readonly Mock<IGetDocumentServiceInternal> _mockGetDocumentService = new();
    private readonly Mock<IAddDocumentService> _mockAddDocumentService = new();
    private readonly Mock<IRemoveDocumentService> _mockRemoveDocumentService = new();
    private readonly Mock<IClock> _mockClock = new();
    private readonly Mock<IGetConfiguredFcAreas> _getConfiguredFcAreas = new();
    private const string AdminHubAddress = "admin hub address";
    private readonly string _requestContextCorrelationId = Guid.NewGuid().ToString();
    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
    private FormFileCollection _formFileCollection = new();

    private static readonly Fixture Fixture = new Fixture();

    [Theory, AutoData]
    public async Task ValidateAccessCode_WhenAccessCodeNotValid(
        Guid applicationId,
        Guid accessCode,
        string emailAddress)
    {
        var sut = CreateSut();
        _mockExternalConsulteeReviewService
            .Setup(x => x.VerifyAccessCodeAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<ExternalAccessLinkModel>.None);

        var result = await sut.ValidateAccessCodeAsync(applicationId, accessCode, emailAddress, CancellationToken.None);

        Assert.True(result.IsFailure);

        _mockExternalConsulteeReviewService.Verify(x => x.VerifyAccessCodeAsync(applicationId, accessCode, emailAddress, It.IsAny<CancellationToken>()), Times.Once);
        _mockExternalConsulteeReviewService.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task ValidateAccessCode_WhenAccessCodeIsValid(
        Guid accessCode,
        ExternalAccessLinkModel validAccessLink)
    {
        var sut = CreateSut();
        _mockExternalConsulteeReviewService
            .Setup(x => x.VerifyAccessCodeAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<ExternalAccessLinkModel>.From(validAccessLink));

        var result = await sut.ValidateAccessCodeAsync(validAccessLink.ApplicationId, accessCode, validAccessLink.ContactEmail, CancellationToken.None);

        Assert.True(result.IsSuccess);

        Assert.Equal(validAccessLink.ContactEmail, result.Value.ContactEmail);
        Assert.Equal(validAccessLink.ExpiresTimeStamp, result.Value.ExpiresTimeStamp);
        Assert.Equal(validAccessLink.ContactName, result.Value.Name);
        Assert.Equal(validAccessLink.Purpose, result.Value.Purpose);

        _mockExternalConsulteeReviewService.Verify(x => x.VerifyAccessCodeAsync(validAccessLink.ApplicationId, accessCode, validAccessLink.ContactEmail, It.IsAny<CancellationToken>()), Times.Once);
        _mockExternalConsulteeReviewService.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task AddConsulteeComment_WhenUnableToAddAttachments(
        AddConsulteeCommentModel model,
        string error)
    {
        var sut = CreateSut();
        var addDocumentsResult = new AddDocumentsFailureResult([error]);

        AddFileToFormCollection();

        var createdTimestamp = new Instant();
        _mockClock.Setup(x => x.GetCurrentInstant()).Returns(createdTimestamp);

        _mockAddDocumentService
            .Setup(x => x.AddDocumentsAsInternalUserAsync(It.IsAny<AddDocumentsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(addDocumentsResult);

        var result = await sut.AddConsulteeCommentAsync(model, _formFileCollection, CancellationToken.None);

        Assert.True(result.IsFailure);
        _mockExternalConsulteeReviewService.VerifyNoOtherCalls();
        _mockClock.Verify(x => x.GetCurrentInstant(), Times.Once);
        _mockClock.VerifyNoOtherCalls();

        _mockAddDocumentService.Verify(x => x.AddDocumentsAsInternalUserAsync(It.Is<AddDocumentsRequest>(a =>
            a.ActorType == ActorType.ExternalConsultee
            && a.DocumentPurpose == DocumentPurpose.ConsultationAttachment
            && a.FellingApplicationId == model.ApplicationId
            && a.FileToStoreModels.Count == 1), It.IsAny<CancellationToken>()), Times.Once);
        _mockAddDocumentService.VerifyNoOtherCalls();

        _mockRemoveDocumentService.VerifyNoOtherCalls();

        _mockAuditService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.AddConsulteeCommentFailure
                && a.ActorType == ActorType.System
                && a.UserId == null
                && a.SourceEntityId == model.ApplicationId
                && a.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && a.CorrelationId == _requestContextCorrelationId
                && JsonSerializer.Serialize(a.AuditData, _serializerOptions) ==
                JsonSerializer.Serialize(new
                {
                    Error = "Failed to attach consultee documents: " + error,
                    authorName = model.AuthorName,
                    authorContactEmail = model.AuthorContactEmail
                }, _serializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);
        _mockAuditService.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task AddConsulteeComment_WhenSuccessful_WithoutDocuments(
        AddConsulteeCommentModel model)
    {
        var sut = CreateSut();

        var createdTimestamp = new Instant();
        _mockClock.Setup(x => x.GetCurrentInstant()).Returns(createdTimestamp);

        _mockExternalConsulteeReviewService
            .Setup(x => x.AddCommentAsync(It.IsAny<ConsulteeCommentModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var result = await sut.AddConsulteeCommentAsync(model, _formFileCollection, CancellationToken.None);

        Assert.True(result.IsSuccess);
        _mockExternalConsulteeReviewService.Verify(x => x.AddCommentAsync(It.Is<ConsulteeCommentModel>(c =>
                c.CreatedTimestamp == createdTimestamp.ToDateTimeUtc()
                && c.FellingLicenceApplicationId == model.ApplicationId
                && c.AuthorName == model.AuthorName
                && c.AuthorContactEmail == model.AuthorContactEmail
                && c.Comment == model.Comment
                && !c.ConsulteeAttachmentIds.Any()
                && c.AccessCode == model.AccessCode),
            It.IsAny<CancellationToken>()), Times.Once);
        _mockExternalConsulteeReviewService.VerifyNoOtherCalls();
        _mockClock.Verify(x => x.GetCurrentInstant(), Times.Once);
        _mockClock.VerifyNoOtherCalls();

        _mockAddDocumentService.VerifyNoOtherCalls();

        _mockRemoveDocumentService.VerifyNoOtherCalls();

        _mockAuditService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.AddConsulteeComment
                && a.ActorType == ActorType.System
                && a.UserId == null
                && a.SourceEntityId == model.ApplicationId
                && a.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && a.CorrelationId == _requestContextCorrelationId
                && JsonSerializer.Serialize(a.AuditData, _serializerOptions) ==
                JsonSerializer.Serialize(new
                {
                    authorName = model.AuthorName,
                    authorContactEmail = model.AuthorContactEmail
                }, _serializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);
        _mockAuditService.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task AddConsulteeComment_WhenSuccessful_WithDocuments(
        AddConsulteeCommentModel model,
        AddDocumentsSuccessResult addDocumentsResult)
    {
        var sut = CreateSut();

        AddFileToFormCollection();

        var createdTimestamp = new Instant();
        _mockClock.Setup(x => x.GetCurrentInstant()).Returns(createdTimestamp);

        _mockExternalConsulteeReviewService
            .Setup(x => x.AddCommentAsync(It.IsAny<ConsulteeCommentModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        _mockAddDocumentService
            .Setup(x => x.AddDocumentsAsInternalUserAsync(It.IsAny<AddDocumentsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(addDocumentsResult);

        var result = await sut.AddConsulteeCommentAsync(model, _formFileCollection, CancellationToken.None);

        Assert.True(result.IsSuccess);
        _mockExternalConsulteeReviewService.Verify(x => x.AddCommentAsync(It.Is<ConsulteeCommentModel>(c =>
                c.CreatedTimestamp == createdTimestamp.ToDateTimeUtc()
                && c.FellingLicenceApplicationId == model.ApplicationId
                && c.AuthorName == model.AuthorName
                && c.AuthorContactEmail == model.AuthorContactEmail
                && c.Comment == model.Comment
                && c.ConsulteeAttachmentIds.Count() == addDocumentsResult.DocumentIds.Count()
                && c.ConsulteeAttachmentIds.All(a => addDocumentsResult.DocumentIds.Any(z => z == a))
                && c.AccessCode == model.AccessCode), 
            It.IsAny<CancellationToken>()), Times.Once);
        _mockExternalConsulteeReviewService.VerifyNoOtherCalls();
        _mockClock.Verify(x => x.GetCurrentInstant(), Times.Once);
        _mockClock.VerifyNoOtherCalls();

        _mockAddDocumentService.Verify(x => x.AddDocumentsAsInternalUserAsync(It.Is<AddDocumentsRequest>(a => 
            a.ActorType == ActorType.ExternalConsultee
            && a.DocumentPurpose == DocumentPurpose.ConsultationAttachment
            && a.FellingApplicationId == model.ApplicationId
            && a.FileToStoreModels.Count == _formFileCollection.Count), It.IsAny<CancellationToken>()), Times.Once);
        _mockAddDocumentService.VerifyNoOtherCalls();

        _mockRemoveDocumentService.VerifyNoOtherCalls();

        _mockAuditService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.AddConsulteeComment
                && a.ActorType == ActorType.System
                && a.UserId == null
                && a.SourceEntityId == model.ApplicationId
                && a.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && a.CorrelationId == _requestContextCorrelationId
                && JsonSerializer.Serialize(a.AuditData, _serializerOptions) ==
                JsonSerializer.Serialize(new
                {
                    authorName = model.AuthorName,
                    authorContactEmail = model.AuthorContactEmail
                }, _serializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);
        _mockAuditService.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task AddConsulteeComment_WhenUnsuccessful_RemovesAttachmentsAgain(
        AddConsulteeCommentModel model,
        string error)
    {
        var sut = CreateSut();
        var addDocumentsResult = new AddDocumentsSuccessResult([Guid.NewGuid()], []);

        AddFileToFormCollection();

        var createdTimestamp = new Instant();
        _mockClock.Setup(x => x.GetCurrentInstant()).Returns(createdTimestamp);

        _mockExternalConsulteeReviewService
            .Setup(x => x.AddCommentAsync(It.IsAny<ConsulteeCommentModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(error));

        _mockAddDocumentService
            .Setup(x => x.AddDocumentsAsInternalUserAsync(It.IsAny<AddDocumentsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(addDocumentsResult);

        _mockRemoveDocumentService
            .Setup(x => x.PermanentlyRemoveDocumentAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var result = await sut.AddConsulteeCommentAsync(model, _formFileCollection, CancellationToken.None);

        Assert.True(result.IsFailure);
        _mockExternalConsulteeReviewService.Verify(x => x.AddCommentAsync(It.Is<ConsulteeCommentModel>(c =>
                c.CreatedTimestamp == createdTimestamp.ToDateTimeUtc()
                && c.FellingLicenceApplicationId == model.ApplicationId
                && c.AuthorName == model.AuthorName
                && c.AuthorContactEmail == model.AuthorContactEmail
                && c.Comment == model.Comment
                && c.ConsulteeAttachmentIds.Single() == addDocumentsResult.DocumentIds.Single()
                && c.AccessCode == model.AccessCode), It.IsAny<CancellationToken>()), Times.Once);
        _mockExternalConsulteeReviewService.VerifyNoOtherCalls();
        _mockClock.Verify(x => x.GetCurrentInstant(), Times.Once);
        _mockClock.VerifyNoOtherCalls();

        _mockAddDocumentService.Verify(x => x.AddDocumentsAsInternalUserAsync(It.Is<AddDocumentsRequest>(a =>
            a.ActorType == ActorType.ExternalConsultee
            && a.DocumentPurpose == DocumentPurpose.ConsultationAttachment
            && a.FellingApplicationId == model.ApplicationId
            && a.FileToStoreModels.Count == 1), It.IsAny<CancellationToken>()), Times.Once);
        _mockAddDocumentService.VerifyNoOtherCalls();

        _mockRemoveDocumentService.Verify(x => 
            x.PermanentlyRemoveDocumentAsync(model.ApplicationId, addDocumentsResult.DocumentIds.Single(), It.IsAny<CancellationToken>()), 
            Times.Once);
        _mockRemoveDocumentService.VerifyNoOtherCalls();

        _mockAuditService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.AddConsulteeCommentFailure
                && a.ActorType == ActorType.System
                && a.UserId == null
                && a.SourceEntityId == model.ApplicationId
                && a.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && a.CorrelationId == _requestContextCorrelationId
                && JsonSerializer.Serialize(a.AuditData, _serializerOptions) ==
                JsonSerializer.Serialize(new
                {
                    Error = "Failed to save consultee comment: " + error,
                    authorName = model.AuthorName,
                    authorContactEmail = model.AuthorContactEmail
                }, _serializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);
        _mockAuditService.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task GetApplicationSummaryForConsulteeReviewWhenApplicationIsNotFound(
        Guid applicationId,
        Guid accessCode,
        ExternalInviteLink externalInviteLink)
    {
        var sut = CreateSut();

        _mockFellingLicenceApplicationInternalRepository.Setup(x =>
                x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.None);

        var result = await sut.GetApplicationSummaryForConsulteeReviewAsync(applicationId, externalInviteLink, accessCode, CancellationToken.None);

        Assert.False(result.IsSuccess);

        _mockAuditService.VerifyNoOtherCalls();
        
        _mockFellingLicenceApplicationInternalRepository.Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _mockFellingLicenceApplicationInternalRepository.VerifyNoOtherCalls();

        _mockWoodlandOwnerService.VerifyNoOtherCalls();

        _mockExternalUserAccountService.VerifyNoOtherCalls();
        _mockInternalUserAccountService.VerifyNoOtherCalls();

        _mockExternalConsulteeReviewService.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task GetApplicationSummaryForConsulteeReviewWhenCannotExtractApplicationSummaryDueToOwner(
        Guid applicationId,
        FellingLicenceApplication fellingLicenceApplication,
        Guid accessCode,
        ExternalInviteLink externalInviteLink)
    {
        var sut = CreateSut();

        _mockFellingLicenceApplicationInternalRepository.Setup(x =>
                x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.From(fellingLicenceApplication));

        _mockWoodlandOwnerService.Setup(x =>
                x.RetrieveWoodlandOwnerByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<WoodlandOwnerModel>("error"));

        var result = await sut.GetApplicationSummaryForConsulteeReviewAsync(applicationId, externalInviteLink, accessCode, CancellationToken.None);

        Assert.False(result.IsSuccess);

        _mockAuditService.VerifyNoOtherCalls();

        _mockFellingLicenceApplicationInternalRepository.Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _mockFellingLicenceApplicationInternalRepository.VerifyNoOtherCalls();

        _mockWoodlandOwnerService.Verify(x => x.RetrieveWoodlandOwnerByIdAsync(fellingLicenceApplication.WoodlandOwnerId, It.IsAny<CancellationToken>()), Times.Once);
        _mockWoodlandOwnerService.VerifyNoOtherCalls();

        _mockExternalUserAccountService.VerifyNoOtherCalls();
        _mockInternalUserAccountService.VerifyNoOtherCalls();

        _mockExternalConsulteeReviewService.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task GetApplicationSummaryForConsulteeReviewWhenCannotExtractApplicationSummaryDueToAssignees(
        Guid applicationId,
        FellingLicenceApplication fellingLicenceApplication,
        WoodlandOwnerModel woodlandOwnerModel,
        Guid accessCode,
        ExternalInviteLink externalInviteLink)
    {
        var sut = CreateSut();

        _mockFellingLicenceApplicationInternalRepository.Setup(x =>
                x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.From(fellingLicenceApplication));

        _mockWoodlandOwnerService.Setup(x =>
                x.RetrieveWoodlandOwnerByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(woodlandOwnerModel));

        _mockExternalUserAccountService.Setup(x =>
                x.RetrieveUserAccountEntityByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<UserAccount>("error"));
        _mockInternalUserAccountService.Setup(x =>
                x.GetUserAccountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<Flo.Services.InternalUsers.Entities.UserAccount.UserAccount>.None);

        var result = await sut.GetApplicationSummaryForConsulteeReviewAsync(applicationId, externalInviteLink, accessCode, CancellationToken.None);

        Assert.False(result.IsSuccess);

        _mockAuditService.VerifyNoOtherCalls();

        _mockFellingLicenceApplicationInternalRepository.Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _mockFellingLicenceApplicationInternalRepository.VerifyNoOtherCalls();

        _mockWoodlandOwnerService.Verify(x => x.RetrieveWoodlandOwnerByIdAsync(fellingLicenceApplication.WoodlandOwnerId, It.IsAny<CancellationToken>()), Times.Once);
        _mockWoodlandOwnerService.VerifyNoOtherCalls();

        var firstAssignee = fellingLicenceApplication.AssigneeHistories.First();
        if (firstAssignee.Role is AssignedUserRole.Author or AssignedUserRole.Applicant)
        {
            _mockExternalUserAccountService.Verify(x => x.RetrieveUserAccountEntityByIdAsync(firstAssignee.AssignedUserId, It.IsAny<CancellationToken>()), Times.Once);
        }
        else
        {
            _mockInternalUserAccountService.Verify(x => x.GetUserAccountAsync(firstAssignee.AssignedUserId, It.IsAny<CancellationToken>()), Times.Once);
        }

        _mockExternalUserAccountService.VerifyNoOtherCalls();
        _mockInternalUserAccountService.VerifyNoOtherCalls();

        _mockExternalConsulteeReviewService.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task GetApplicationSummaryForConsulteeReviewWhenNoExistingComments(
        Guid applicationId,
        FellingLicenceApplication fellingLicenceApplication,
        WoodlandOwnerModel woodlandOwnerModel,
        UserAccount externalUserAccount,
        Flo.Services.InternalUsers.Entities.UserAccount.UserAccount internalUserAccount,
        Guid accessCode)
    {
        SyncApplicationCompartmentData(fellingLicenceApplication);
        fellingLicenceApplication.Documents.ForEach(d =>
        {
            d.VisibleToConsultee = true;
            d.DeletionTimestamp = null;
            typeof(Document)
                .GetProperty("Id", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                .SetValue(d, Guid.NewGuid());
        });

        ExternalInviteLink externalInviteLink = new Fixture()
            .Build<ExternalInviteLink>()
            .With(c => c.SharedSupportingDocuments, fellingLicenceApplication.Documents.Take(2).Select(x => x.Id).ToList())
            .Create();
        
        var sut = CreateSut();

        _mockFellingLicenceApplicationInternalRepository.Setup(x =>
                x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.From(fellingLicenceApplication));

        _mockWoodlandOwnerService.Setup(x =>
                x.RetrieveWoodlandOwnerByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(woodlandOwnerModel));

        _mockExternalUserAccountService.Setup(x =>
                x.RetrieveUserAccountEntityByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<UserAccount>(externalUserAccount));
        _mockInternalUserAccountService.Setup(x =>
                x.GetUserAccountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe.From(internalUserAccount));

        _mockExternalConsulteeReviewService.Setup(x => 
                x.RetrieveConsulteeCommentsForAccessCodeAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await sut.GetApplicationSummaryForConsulteeReviewAsync(applicationId, externalInviteLink, accessCode, CancellationToken.None);

        Assert.True(result.IsSuccess);

        Assert.NotNull(result.Value.ApplicationSummary);

        Assert.Equal(fellingLicenceApplication.Id, result.Value.ApplicationSummary.Id);
        Assert.Equal(fellingLicenceApplication.ApplicationReference, result.Value.ApplicationSummary.ApplicationReference);

        Assert.Equivalent(fellingLicenceApplication.Documents.Take(2).Select(d => new DocumentModel
        {
            Id = d.Id,
            CreatedTimestamp = d.CreatedTimestamp,
            FileName = d.FileName,
            DocumentPurpose = d.Purpose,
            FileSize = d.FileSize,
            FileType = d.FileType,
            AttachedByType = d.AttachedByType,
            MimeType = d.MimeType
        }), result.Value.ConsulteeDocuments);

        Assert.NotNull(result.Value.ActivityFeed);
        Assert.Empty(result.Value.ActivityFeed.ActivityFeedItemModels);
        Assert.Equal(applicationId, result.Value.ActivityFeed.ApplicationId);
        Assert.False(result.Value.ActivityFeed.ShowAddCaseNote);
        Assert.Equal("Your added comments", result.Value.ActivityFeed.ActivityFeedTitle);
        Assert.False(result.Value.ActivityFeed.ShowFilters);

        _mockAuditService.VerifyNoOtherCalls();

        _mockFellingLicenceApplicationInternalRepository.Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _mockFellingLicenceApplicationInternalRepository.VerifyNoOtherCalls();

        _mockWoodlandOwnerService.Verify(x => x.RetrieveWoodlandOwnerByIdAsync(fellingLicenceApplication.WoodlandOwnerId, It.IsAny<CancellationToken>()), Times.Once);
        _mockWoodlandOwnerService.VerifyNoOtherCalls();

        foreach (var assigneeHistory in fellingLicenceApplication.AssigneeHistories)
        {
            if (assigneeHistory.Role is AssignedUserRole.Author or AssignedUserRole.Applicant)
            {
                _mockExternalUserAccountService.Verify(x => x.RetrieveUserAccountEntityByIdAsync(assigneeHistory.AssignedUserId, It.IsAny<CancellationToken>()), Times.Once);
            }
            else
            {
                _mockInternalUserAccountService.Verify(x => x.GetUserAccountAsync(assigneeHistory.AssignedUserId, It.IsAny<CancellationToken>()), Times.Once);
            }
        }

        _mockExternalUserAccountService.VerifyNoOtherCalls();
        _mockInternalUserAccountService.VerifyNoOtherCalls();

        _mockExternalConsulteeReviewService.Verify(x =>
                x.RetrieveConsulteeCommentsForAccessCodeAsync(applicationId, accessCode, It.IsAny<CancellationToken>()), Times.Once);
        _mockExternalConsulteeReviewService.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task GetApplicationSummaryForConsulteeReviewWithExistingComments(
        Guid applicationId,
        FellingLicenceApplication fellingLicenceApplication,
        WoodlandOwnerModel woodlandOwnerModel,
        UserAccount externalUserAccount,
        Flo.Services.InternalUsers.Entities.UserAccount.UserAccount internalUserAccount,
        Guid accessCode)
    {
        SyncApplicationCompartmentData(fellingLicenceApplication);
        fellingLicenceApplication.Documents.ForEach(d =>
        {
            d.VisibleToConsultee = true;
            d.DeletionTimestamp = null;
            typeof(Document)
                .GetProperty("Id", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                .SetValue(d, Guid.NewGuid());
        });

        ExternalInviteLink externalInviteLink = new Fixture()
            .Build<ExternalInviteLink>()
            .With(c => c.SharedSupportingDocuments, fellingLicenceApplication.Documents.Take(2).Select(x => x.Id).ToList())
            .Create();

        var authoredComments = Fixture.Build<ConsulteeCommentModel>()
            .With(x => x.AuthorContactEmail, externalInviteLink.ContactEmail)
            .With(x => x.ConsulteeAttachmentIds, [fellingLicenceApplication.Documents.First().Id])
            .CreateMany()
            .ToList();

        var expectedItemFeedItems = authoredComments
            .OrderByDescending(x => x.CreatedTimestamp)
            .Select(x => new ActivityFeedItemModel
            {
                ActivityFeedItemType = ActivityFeedItemType.ConsulteeComment,
                CreatedTimestamp = x.CreatedTimestamp,
                FellingLicenceApplicationId = x.FellingLicenceApplicationId,
                VisibleToConsultee = true,
                Text = x.Comment,
                Source = $"{x.AuthorName} ({x.AuthorContactEmail})",
                Attachments = new Dictionary<Guid, string> {{ fellingLicenceApplication.Documents.First().Id, fellingLicenceApplication.Documents.First().FileName }}
            }).ToList();

        var sut = CreateSut();

        _mockFellingLicenceApplicationInternalRepository.Setup(x =>
                x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.From(fellingLicenceApplication));

        _mockWoodlandOwnerService.Setup(x =>
                x.RetrieveWoodlandOwnerByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(woodlandOwnerModel));

        _mockExternalUserAccountService.Setup(x =>
                x.RetrieveUserAccountEntityByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<UserAccount>(externalUserAccount));
        _mockInternalUserAccountService.Setup(x =>
                x.GetUserAccountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe.From(internalUserAccount));

        _mockExternalConsulteeReviewService.Setup(x =>
                x.RetrieveConsulteeCommentsForAccessCodeAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(authoredComments);

        var result = await sut.GetApplicationSummaryForConsulteeReviewAsync(applicationId, externalInviteLink, accessCode, CancellationToken.None);

        Assert.True(result.IsSuccess);

        Assert.NotNull(result.Value.ApplicationSummary);

        Assert.Equal(fellingLicenceApplication.Id, result.Value.ApplicationSummary.Id);
        Assert.Equal(fellingLicenceApplication.ApplicationReference, result.Value.ApplicationSummary.ApplicationReference);

        Assert.Equivalent(fellingLicenceApplication.Documents.Take(2).Select(d => new DocumentModel
        {
            Id = d.Id,
            CreatedTimestamp = d.CreatedTimestamp,
            FileName = d.FileName,
            DocumentPurpose = d.Purpose,
            FileSize = d.FileSize,
            FileType = d.FileType,
            AttachedByType = d.AttachedByType,
            MimeType = d.MimeType
        }), result.Value.ConsulteeDocuments);
        //result.Value.ConsulteeDocuments.Should().BeEquivalentTo(
        //    fellingLicenceApplication.Documents.Take(2), o => o.ExcludingMissingMembers());

        Assert.NotNull(result.Value.ActivityFeed);
        Assert.Equivalent(expectedItemFeedItems, result.Value.ActivityFeed.ActivityFeedItemModels);
        Assert.Equal(applicationId, result.Value.ActivityFeed.ApplicationId);
        Assert.False(result.Value.ActivityFeed.ShowAddCaseNote);
        Assert.Equal("Your added comments", result.Value.ActivityFeed.ActivityFeedTitle);
        Assert.False(result.Value.ActivityFeed.ShowFilters);

        _mockAuditService.VerifyNoOtherCalls();

        _mockFellingLicenceApplicationInternalRepository.Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _mockFellingLicenceApplicationInternalRepository.VerifyNoOtherCalls();

        _mockWoodlandOwnerService.Verify(x => x.RetrieveWoodlandOwnerByIdAsync(fellingLicenceApplication.WoodlandOwnerId, It.IsAny<CancellationToken>()), Times.Once);
        _mockWoodlandOwnerService.VerifyNoOtherCalls();

        foreach (var assigneeHistory in fellingLicenceApplication.AssigneeHistories)
        {
            if (assigneeHistory.Role is AssignedUserRole.Author or AssignedUserRole.Applicant)
            {
                _mockExternalUserAccountService.Verify(x => x.RetrieveUserAccountEntityByIdAsync(assigneeHistory.AssignedUserId, It.IsAny<CancellationToken>()), Times.Once);
            }
            else
            {
                _mockInternalUserAccountService.Verify(x => x.GetUserAccountAsync(assigneeHistory.AssignedUserId, It.IsAny<CancellationToken>()), Times.Once);
            }
        }

        _mockExternalUserAccountService.VerifyNoOtherCalls();
        _mockInternalUserAccountService.VerifyNoOtherCalls();

        _mockExternalConsulteeReviewService.Verify(x =>
                x.RetrieveConsulteeCommentsForAccessCodeAsync(applicationId, accessCode, It.IsAny<CancellationToken>()), Times.Once);
        _mockExternalConsulteeReviewService.VerifyNoOtherCalls();
    }

    private ExternalConsulteeReviewUseCase CreateSut()
    {
        _mockInternalUserAccountService.Reset();
        _mockExternalUserAccountService.Reset();
        _mockFellingLicenceApplicationInternalRepository.Reset();
        _mockWoodlandOwnerService.Reset();
        _mockAuditService.Reset();
        _mockExternalConsulteeReviewService.Reset();
        _mockClock.Reset();
        _getConfiguredFcAreas.Reset();
        _getConfiguredFcAreas.Setup(x => x.TryGetAdminHubAddress(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AdminHubAddress);
        _mockGetDocumentService.Reset();
        _mockAddDocumentService.Reset();
        _mockRemoveDocumentService.Reset();
        _formFileCollection = new();

        return new ExternalConsulteeReviewUseCase(
            _mockInternalUserAccountService.Object,
            _mockExternalUserAccountService.Object,
            _mockFellingLicenceApplicationInternalRepository.Object,
            _mockWoodlandOwnerService.Object,
            _mockAuditService.Object,
            _mockAgentAuthorityService.Object,
            _mockExternalConsulteeReviewService.Object,
            _getConfiguredFcAreas.Object,
            _mockGetDocumentService.Object,
            _mockAddDocumentService.Object,
            _mockRemoveDocumentService.Object,
            new NullLogger<ExternalConsulteeReviewUseCase>(),
            new RequestContext(_requestContextCorrelationId, new RequestUserModel(UserFactory.CreateUnauthenticatedUser())),
            _mockClock.Object);
    }

    private static void SyncApplicationCompartmentData(FellingLicenceApplication application)
    {
        var i = 0;
        application.SubmittedFlaPropertyDetail?.SubmittedFlaPropertyCompartments.ForEach(
            c =>
            {
                application.LinkedPropertyProfile!.ProposedFellingDetails![i].PropertyProfileCompartmentId =
                    c.CompartmentId;
                var fs = 0;
                application.LinkedPropertyProfile!.ProposedFellingDetails![i].FellingSpecies.ForEach(s =>
                    s.Species = TreeSpeciesFactory.SpeciesDictionary.Values.ToArray()[fs++].Code);
                application.LinkedPropertyProfile!.ProposedFellingDetails![i].ProposedRestockingDetails.ForEach(restock =>
                {
                    restock.PropertyProfileCompartmentId = c.CompartmentId;

                    var rs = 0;
                    restock.RestockingSpecies.ForEach(s =>
                        s.Species = TreeSpeciesFactory.SpeciesDictionary.Values.ToArray()[rs++].Code);
                });
                i++;
            });
        foreach (var (fellingDetail, restockingDetail) in from fellingDetail in application.LinkedPropertyProfile!.ProposedFellingDetails!
                 from restockingDetail in fellingDetail.ProposedRestockingDetails
                 select (fellingDetail, restockingDetail))
        {
            restockingDetail.ProposedFellingDetail = fellingDetail;
        }
    }

    private void AddFileToFormCollection(string fileName = "test.csv", string expectedFileContents = "test", string contentType = "text/csv", bool isTooLarge = true)
    {
        var fileBytes = !isTooLarge ? Fixture.CreateMany<byte>(100000).ToArray() : Encoding.Default.GetBytes(expectedFileContents);
        var formFileMock = new Mock<IFormFile>();

        formFileMock.Setup(ff => ff.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .Returns<Stream, CancellationToken>((s, _) =>
            {
                var buffer = fileBytes;
                s.Write(buffer, 0, buffer.Length);
                return Task.CompletedTask;
            });

        formFileMock.Setup(ff => ff.FileName).Returns(fileName);
        formFileMock.Setup(ff => ff.Length).Returns(fileBytes.Length);
        formFileMock.Setup(ff => ff.ContentType).Returns(contentType);

        _formFileCollection.Add(formFileMock.Object);
    }
}