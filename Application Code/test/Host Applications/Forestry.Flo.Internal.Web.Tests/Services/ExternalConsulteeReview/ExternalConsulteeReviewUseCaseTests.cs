using System.Text.Json;
using AutoFixture.Xunit2;
using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Models.ExternalConsulteeReview;
using Forestry.Flo.Internal.Web.Services.ExternalConsulteeReview;
using Forestry.Flo.Services.Applicants.Repositories;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Models.ExternalConsultee;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.InternalUsers.Services;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NodaTime;

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
    private readonly Mock<IClock> _mockClock = new();
    private readonly Mock<IGetConfiguredFcAreas> _getConfiguredFcAreas = new();
    private const string AdminHubAddress = "admin hub address";
    private readonly string _requestContextCorrelationId = Guid.NewGuid().ToString();
    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

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
    public async Task AddConsulteeComment_WhenSuccessful(
        AddConsulteeCommentModel model)
    {
        var sut = CreateSut();

        var createdTimestamp = new Instant();
        _mockClock.Setup(x => x.GetCurrentInstant()).Returns(createdTimestamp);

        _mockExternalConsulteeReviewService
            .Setup(x => x.AddCommentAsync(It.IsAny<ConsulteeCommentModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var result = await sut.AddConsulteeCommentAsync(model, CancellationToken.None);

        Assert.True(result.IsSuccess);
        _mockExternalConsulteeReviewService.Verify(x => x.AddCommentAsync(It.Is<ConsulteeCommentModel>(c =>
                c.CreatedTimestamp == createdTimestamp.ToDateTimeUtc()
                && c.FellingLicenceApplicationId == model.ApplicationId
                && c.AuthorName == model.AuthorName
                && c.AuthorContactEmail == model.AuthorContactEmail
                && c.ApplicableToSection == model.ApplicableToSection
                && c.Comment == model.Comment), It.IsAny<CancellationToken>()), Times.Once);
        _mockExternalConsulteeReviewService.VerifyNoOtherCalls();
        _mockClock.Verify(x => x.GetCurrentInstant(), Times.Once);
        _mockClock.VerifyNoOtherCalls();

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
    public async Task AddConsulteeComment_WhenUnsuccessful(
        AddConsulteeCommentModel model,
        string error)
    {
        var sut = CreateSut();

        var createdTimestamp = new Instant();
        _mockClock.Setup(x => x.GetCurrentInstant()).Returns(createdTimestamp);

        _mockExternalConsulteeReviewService
            .Setup(x => x.AddCommentAsync(It.IsAny<ConsulteeCommentModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(error));

        var result = await sut.AddConsulteeCommentAsync(model, CancellationToken.None);

        Assert.True(result.IsFailure);
        _mockExternalConsulteeReviewService.Verify(x => x.AddCommentAsync(It.Is<ConsulteeCommentModel>(c =>
                c.CreatedTimestamp == createdTimestamp.ToDateTimeUtc()
                && c.FellingLicenceApplicationId == model.ApplicationId
                && c.AuthorName == model.AuthorName
                && c.AuthorContactEmail == model.AuthorContactEmail
                && c.ApplicableToSection == model.ApplicableToSection
                && c.Comment == model.Comment), It.IsAny<CancellationToken>()), Times.Once);
        _mockExternalConsulteeReviewService.VerifyNoOtherCalls();
        _mockClock.Verify(x => x.GetCurrentInstant(), Times.Once);
        _mockClock.VerifyNoOtherCalls();

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
                    error,
                    authorName = model.AuthorName,
                    authorContactEmail = model.AuthorContactEmail
                }, _serializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);
        _mockAuditService.VerifyNoOtherCalls();
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

        return new ExternalConsulteeReviewUseCase(
            _mockInternalUserAccountService.Object,
            _mockExternalUserAccountService.Object,
            _mockFellingLicenceApplicationInternalRepository.Object,
            _mockWoodlandOwnerService.Object,
            _mockAuditService.Object,
            _mockAgentAuthorityService.Object,
            _mockExternalConsulteeReviewService.Object,
            _getConfiguredFcAreas.Object,
            new NullLogger<ExternalConsulteeReviewUseCase>(),
            new RequestContext(_requestContextCorrelationId, new RequestUserModel(UserFactory.CreateUnauthenticatedUser())),
            _mockClock.Object);
    }
}