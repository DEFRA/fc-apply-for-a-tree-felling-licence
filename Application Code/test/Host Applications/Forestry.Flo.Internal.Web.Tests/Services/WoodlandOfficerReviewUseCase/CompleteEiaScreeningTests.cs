using System.Text.Json;
using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Services;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Forestry.Flo.Internal.Web.Tests.Services.WoodlandOfficerReviewUseCase;

public class CompleteEiaScreeningTests : WoodlandOfficerReviewUseCaseTestsBase<Web.Services.FellingLicenceApplication.WoodlandOfficerReview.WoodlandOfficerReviewUseCase>
{
    [Theory, AutoMoqData]
    public async Task CompleteEiaScreeningAsync_WhenServiceReturnsError(
        Guid applicationId,
        Guid userId,
        string error)
    {
        // Arrange
        var sut = CreateSut();
        var user = new InternalUser(UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(localAccountId: userId));

        UpdateWoodlandOfficerReviewService
            .Setup(x => x.CompleteEiaScreeningCheckAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(error));

        // Act
        var result = await sut.CompleteEiaScreeningAsync(applicationId, user, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(error, result.Error);

        UpdateWoodlandOfficerReviewService.Verify(x =>
            x.CompleteEiaScreeningCheckAsync(applicationId, userId, It.IsAny<CancellationToken>()), Times.Once);
        UpdateWoodlandOfficerReviewService.VerifyNoOtherCalls();

        AuditingService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.WoodlandOfficerReviewEiaScreeningFailure
                && a.ActorType == ActorType.InternalUser
                && a.UserId == userId
                && a.SourceEntityId == applicationId
                && a.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && JsonSerializer.Serialize(a.AuditData, SerializerOptions) ==
                JsonSerializer.Serialize(new
                {
                    Error = error,
                }, SerializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);

        AuditingService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.UpdateWoodlandOfficerReviewFailure
                && a.ActorType == ActorType.InternalUser
                && a.UserId == userId
                && a.SourceEntityId == applicationId
                && a.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && JsonSerializer.Serialize(a.AuditData, SerializerOptions) ==
                JsonSerializer.Serialize(new
                {
                    Section = "EIA screening",
                    Error = error,
                }, SerializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);

        AuditingService.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task CompleteEiaScreeningAsync_WhenServiceReturnsSuccess(
        Guid applicationId,
        Guid userId)
    {
        // Arrange
        var sut = CreateSut();
        var user = new InternalUser(UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(localAccountId: userId));

        UpdateWoodlandOfficerReviewService
            .Setup(x => x.CompleteEiaScreeningCheckAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        var result = await sut.CompleteEiaScreeningAsync(applicationId, user, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);

        UpdateWoodlandOfficerReviewService.Verify(x =>
            x.CompleteEiaScreeningCheckAsync(applicationId, userId, It.IsAny<CancellationToken>()), Times.Once);
        UpdateWoodlandOfficerReviewService.VerifyNoOtherCalls();

        AuditingService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.WoodlandOfficerReviewEiaScreening
                && a.ActorType == ActorType.InternalUser
                && a.UserId == userId
                && a.SourceEntityId == applicationId
                && a.SourceEntityType == SourceEntityType.FellingLicenceApplication),
            It.IsAny<CancellationToken>()), Times.Once);

        AuditingService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.UpdateWoodlandOfficerReview
                && a.ActorType == ActorType.InternalUser
                && a.UserId == userId
                && a.SourceEntityId == applicationId
                && a.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && JsonSerializer.Serialize(a.AuditData, SerializerOptions) ==
                JsonSerializer.Serialize(new
                {
                    Section = "EIA screening",
                }, SerializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);

        AuditingService.VerifyNoOtherCalls();
    }

    private Web.Services.FellingLicenceApplication.WoodlandOfficerReview.WoodlandOfficerReviewUseCase CreateSut()
    {
        ResetMocks();

        return new Web.Services.FellingLicenceApplication.WoodlandOfficerReview.WoodlandOfficerReviewUseCase(
            InternalUserAccountService.Object,
            ExternalUserAccountRepository.Object,
            FlaRepository.Object,
            WoodlandOwnerService.Object,
            WoodlandOfficerReviewService.Object,
            UpdateWoodlandOfficerReviewService.Object,
            ActivityFeedItemProvider.Object,
            AuditingService.Object,
            NotificationService.Object,
            MockAgentAuthorityService.Object,
            GetConfiguredFcAreas.Object,
            Clock.Object,
            WoodlandOfficerReviewSubStatusService.Object,
            RequestContext,
            MockBus.Object,
            new NullLogger<Web.Services.FellingLicenceApplication.WoodlandOfficerReview.WoodlandOfficerReviewUseCase>());
    }
}