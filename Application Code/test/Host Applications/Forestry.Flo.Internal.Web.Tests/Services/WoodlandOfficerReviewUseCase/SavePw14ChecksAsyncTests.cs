using System.Text.Json;
using AutoFixture.Xunit2;
using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Models.WoodlandOfficerReview;
using Forestry.Flo.Internal.Web.Services;
using Forestry.Flo.Internal.Web.Services.FellingLicenceApplication.WoodlandOfficerReview;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Forestry.Flo.Internal.Web.Tests.Services.WoodlandOfficerReviewUseCase;

public class SavePw14ChecksAsyncTests : WoodlandOfficerReviewUseCaseTestsBase<Pw14UseCase>
{
    [Theory, AutoData]
    public async Task OnSuccessfulUpdate(
        Pw14ChecksViewModel model)
    {
        var userPrincipal = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(localAccountId: RequestContextUserId);
        var user = new InternalUser(userPrincipal);
        var sut = CreateSut();

        UpdateWoodlandOfficerReviewService
            .Setup(x => x.UpdatePw14ChecksAsync(It.IsAny<Guid>(), It.IsAny<Pw14ChecksModel>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success);

        var result = await sut.SavePw14ChecksAsync(model, user, CancellationToken.None);

        Assert.True(result.IsSuccess);

        UpdateWoodlandOfficerReviewService.Verify(x => x.UpdatePw14ChecksAsync(
                model.ApplicationId, model.Pw14Checks, user.UserAccountId.Value, It.IsAny<CancellationToken>()),
            Times.Once);

        AuditingService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.UpdateWoodlandOfficerReview
                && a.ActorType == ActorType.InternalUser
                && a.UserId == RequestContextUserId
                && a.SourceEntityId == model.ApplicationId
                && a.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && a.CorrelationId == RequestContextCorrelationId
                && JsonSerializer.Serialize(a.AuditData, SerializerOptions) ==
                JsonSerializer.Serialize(new
                {
                    section = "PW14 Checks",
                }, SerializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);
        AuditingService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.UpdatePw14Checks
                && a.ActorType == ActorType.InternalUser
                && a.UserId == RequestContextUserId
                && a.SourceEntityId == model.ApplicationId
                && a.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && a.CorrelationId == RequestContextCorrelationId
                && JsonSerializer.Serialize(a.AuditData, SerializerOptions) ==
                JsonSerializer.Serialize(new
                {
                    pw14ChecksComplete = model.Pw14Checks.Pw14ChecksComplete
                }, SerializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);
        AuditingService.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task OnUnsuccessfulUpdate(
        Pw14ChecksViewModel model,
        string error)
    {
        var userPrincipal = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(localAccountId: RequestContextUserId);
        var user = new InternalUser(userPrincipal);
        var sut = CreateSut();

        UpdateWoodlandOfficerReviewService
            .Setup(x => x.UpdatePw14ChecksAsync(It.IsAny<Guid>(), It.IsAny<Pw14ChecksModel>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(error));

        var result = await sut.SavePw14ChecksAsync(model, user, CancellationToken.None);

        Assert.True(result.IsFailure);

        UpdateWoodlandOfficerReviewService.Verify(x => x.UpdatePw14ChecksAsync(
                model.ApplicationId, model.Pw14Checks, user.UserAccountId.Value, It.IsAny<CancellationToken>()),
            Times.Once);

        AuditingService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.UpdateWoodlandOfficerReviewFailure
                && a.ActorType == ActorType.InternalUser
                && a.UserId == RequestContextUserId
                && a.SourceEntityId == model.ApplicationId
                && a.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && a.CorrelationId == RequestContextCorrelationId
                && JsonSerializer.Serialize(a.AuditData, SerializerOptions) ==
                JsonSerializer.Serialize(new
                {
                    section = "PW14 Checks",
                    error = error
                }, SerializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);
        AuditingService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.UpdatePw14ChecksFailure
                && a.ActorType == ActorType.InternalUser
                && a.UserId == RequestContextUserId
                && a.SourceEntityId == model.ApplicationId
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

    private Pw14UseCase CreateSut()
    {
        ResetMocks();

        return new Pw14UseCase(
            InternalUserAccountService.Object,
            ExternalUserAccountRepository.Object,
            FlaRepository.Object,
            WoodlandOwnerService.Object,
            WoodlandOfficerReviewService.Object,
            UpdateWoodlandOfficerReviewService.Object,
            AuditingService.Object,
            MockAgentAuthorityService.Object,
            GetConfiguredFcAreas.Object,
            RequestContext,
            ActivityFeedItemProvider.Object,
            WoodlandOfficerReviewSubStatusService.Object,
            new NullLogger<Pw14UseCase>());
    }
}