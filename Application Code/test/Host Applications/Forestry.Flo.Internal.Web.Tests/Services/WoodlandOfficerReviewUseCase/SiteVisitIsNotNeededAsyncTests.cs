using System.Text.Json;
using AutoFixture.Xunit2;
using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Services;
using Forestry.Flo.Internal.Web.Services.FellingLicenceApplication.WoodlandOfficerReview;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Configuration;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Forestry.Flo.Internal.Web.Tests.Services.WoodlandOfficerReviewUseCase;

public class SiteVisitIsNotNeededAsyncTests : WoodlandOfficerReviewUseCaseTestsBase<SiteVisitUseCase>
{
    [Theory, AutoData]
    public async Task ShouldStoreCorrectAuditOnSuccessfulUpdate(
    Guid applicationId,
    string reason)
    {
        var userPrincipal = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(localAccountId: RequestContextUserId);
        var user = new InternalUser(userPrincipal);
        var sut = CreateSut();

        UpdateWoodlandOfficerReviewService
            .Setup(x => x.SetSiteVisitNotNeededAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(true));

        var result = await sut.SiteVisitIsNotNeededAsync(applicationId, user, reason, CancellationToken.None);

        Assert.True(result.IsSuccess);

        UpdateWoodlandOfficerReviewService.Verify(x => x.SetSiteVisitNotNeededAsync(applicationId, RequestContextUserId, reason, It.IsAny<CancellationToken>()), Times.Once);
        UpdateWoodlandOfficerReviewService.VerifyNoOtherCalls();

        AuditingService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
            a.EventName == AuditEvents.UpdateWoodlandOfficerReview
            && a.ActorType == ActorType.InternalUser
            && a.UserId == RequestContextUserId
            && a.SourceEntityId == applicationId
            && a.SourceEntityType == SourceEntityType.FellingLicenceApplication
            && a.CorrelationId == RequestContextCorrelationId
            && JsonSerializer.Serialize(a.AuditData, SerializerOptions) ==
            JsonSerializer.Serialize(new
            {
                section = "Site Visit",
            }, SerializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);
        AuditingService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                    a.EventName == AuditEvents.UpdateSiteVisit
                    && a.ActorType == ActorType.InternalUser
                    && a.UserId == RequestContextUserId
                    && a.SourceEntityId == applicationId
                    && a.SourceEntityType == SourceEntityType.FellingLicenceApplication
                    && a.CorrelationId == RequestContextCorrelationId
                    && JsonSerializer.Serialize(a.AuditData, SerializerOptions) ==
                    JsonSerializer.Serialize(new
                    {
                        notNeededReason = reason
                    }, SerializerOptions)),
                It.IsAny<CancellationToken>()), Times.Once);
        AuditingService.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task ShouldNotStoreAuditOnSuccessButNoUpdate(
        Guid applicationId,
        string reason)
    {
        var userPrincipal = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(localAccountId: RequestContextUserId);
        var user = new InternalUser(userPrincipal);
        var sut = CreateSut();

        UpdateWoodlandOfficerReviewService
            .Setup(x => x.SetSiteVisitNotNeededAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(false));

        var result = await sut.SiteVisitIsNotNeededAsync(applicationId, user, reason, CancellationToken.None);

        Assert.True(result.IsSuccess);

        UpdateWoodlandOfficerReviewService.Verify(x => x.SetSiteVisitNotNeededAsync(applicationId, RequestContextUserId, reason, It.IsAny<CancellationToken>()), Times.Once);
        UpdateWoodlandOfficerReviewService.VerifyNoOtherCalls();

        AuditingService.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task ShouldStoreCorrectFailureAuditOnUnsuccessfulUpdate(
    Guid applicationId,
    string reason,
    string error)
    {
        var userPrincipal = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(localAccountId: RequestContextUserId);
        var user = new InternalUser(userPrincipal);
        var sut = CreateSut();

        UpdateWoodlandOfficerReviewService
            .Setup(x => x.SetSiteVisitNotNeededAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<bool>(error));

        var result = await sut.SiteVisitIsNotNeededAsync(applicationId, user, reason, CancellationToken.None);

        Assert.True(result.IsFailure);

        UpdateWoodlandOfficerReviewService.Verify(x => x.SetSiteVisitNotNeededAsync(applicationId, RequestContextUserId, reason, It.IsAny<CancellationToken>()), Times.Once);
        UpdateWoodlandOfficerReviewService.VerifyNoOtherCalls();

        AuditingService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.UpdateWoodlandOfficerReviewFailure
                && a.ActorType == ActorType.InternalUser
                && a.UserId == RequestContextUserId
                && a.SourceEntityId == applicationId
                && a.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && a.CorrelationId == RequestContextCorrelationId
                && JsonSerializer.Serialize(a.AuditData, SerializerOptions) ==
                JsonSerializer.Serialize(new
                {
                    section = "Site Visit",
                    error = error
                }, SerializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);
        AuditingService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.UpdateSiteVisitFailure
                && a.ActorType == ActorType.InternalUser
                && a.UserId == RequestContextUserId
                && a.SourceEntityId == applicationId
                && a.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && a.CorrelationId == RequestContextCorrelationId
                && JsonSerializer.Serialize(a.AuditData, SerializerOptions) ==
                JsonSerializer.Serialize(new
                {
                    error = error
                }, SerializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);
        AuditingService.VerifyNoOtherCalls();
    }

    private SiteVisitUseCase CreateSut()
    {
        ResetMocks();

        return new SiteVisitUseCase(
            InternalUserAccountService.Object,
            ExternalUserAccountRepository.Object,
            FlaRepository.Object,
            WoodlandOwnerService.Object,
            WoodlandOfficerReviewService.Object,
            UpdateWoodlandOfficerReviewService.Object,
            ActivityFeedItemProvider.Object,
            _forestryServices.Object,
            _foresterServices.Object,
            AuditingService.Object,
            MockAgentAuthorityService.Object,
            GetConfiguredFcAreas.Object,
            RequestContext,
            Clock.Object,
            new NullLogger<SiteVisitUseCase>());
    }
}