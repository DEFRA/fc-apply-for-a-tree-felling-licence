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

public class StorePublicRegisterExemptionAsyncTests : WoodlandOfficerReviewUseCaseTestsBase<PublicRegisterUseCase>
{
    [Theory, AutoData]
    public async Task ShouldStoreCorrectAuditOnSuccessfulUpdate(
        Guid applicationId,
        bool isExempt,
        string? exemptionReason)
    {
        var userPrincipal = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(localAccountId: RequestContextUserId);
        var user = new InternalUser(userPrincipal);
        var sut = CreateSut();

        UpdateWoodlandOfficerReviewService
            .Setup(x => x.SetPublicRegisterExemptAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(true));

        var result = await sut.StorePublicRegisterExemptionAsync(applicationId, isExempt, exemptionReason, user, CancellationToken.None);

        Assert.True(result.IsSuccess);

        UpdateWoodlandOfficerReviewService.Verify(x => x.SetPublicRegisterExemptAsync(applicationId, RequestContextUserId, isExempt, exemptionReason, It.IsAny<CancellationToken>()), Times.Once);
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
                section = "Public Register",
            }, SerializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);
        AuditingService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                    a.EventName == AuditEvents.AddToConsultationPublicRegisterSuccess
                    && a.ActorType == ActorType.InternalUser
                    && a.UserId == RequestContextUserId
                    && a.SourceEntityId == applicationId
                    && a.SourceEntityType == SourceEntityType.FellingLicenceApplication
                    && a.CorrelationId == RequestContextCorrelationId
                    && JsonSerializer.Serialize(a.AuditData, SerializerOptions) ==
                    JsonSerializer.Serialize(new
                    {
                        exempt = isExempt,
                        exemptionReason
                    }, SerializerOptions)),
                It.IsAny<CancellationToken>()), Times.Once);
        AuditingService.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task ShouldNotStoreAuditOnSuccessButNoUpdate(
        Guid applicationId,
        bool isExempt,
        string? exemptionReason)
    {
        var userPrincipal = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(localAccountId: RequestContextUserId);
        var user = new InternalUser(userPrincipal);
        var sut = CreateSut();

        UpdateWoodlandOfficerReviewService
            .Setup(x => x.SetPublicRegisterExemptAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(false));

        var result = await sut.StorePublicRegisterExemptionAsync(applicationId, isExempt, exemptionReason, user, CancellationToken.None);

        Assert.True(result.IsSuccess);

        UpdateWoodlandOfficerReviewService.Verify(x => x.SetPublicRegisterExemptAsync(applicationId, RequestContextUserId, isExempt, exemptionReason, It.IsAny<CancellationToken>()), Times.Once);
        UpdateWoodlandOfficerReviewService.VerifyNoOtherCalls();

        AuditingService.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task ShouldStoreCorrectFailureAuditOnUnsuccessfulUpdate(
        Guid applicationId,
        bool isExempt,
        string? exemptionReason,
        string error)
    {
        var userPrincipal = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(localAccountId: RequestContextUserId);
        var user = new InternalUser(userPrincipal);
        var sut = CreateSut();

        UpdateWoodlandOfficerReviewService
            .Setup(x => x.SetPublicRegisterExemptAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<bool>(error));

        var result = await sut.StorePublicRegisterExemptionAsync(applicationId, isExempt, exemptionReason, user, CancellationToken.None);

        Assert.True(result.IsFailure);

        UpdateWoodlandOfficerReviewService.Verify(x => x.SetPublicRegisterExemptAsync(applicationId, RequestContextUserId, isExempt, exemptionReason, It.IsAny<CancellationToken>()), Times.Once);
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
                    section = "Public Register",
                    error = error
                }, SerializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);
        AuditingService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.AddToConsultationPublicRegisterFailure
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

    private PublicRegisterUseCase CreateSut()
    {
        ResetMocks();

        return new PublicRegisterUseCase(
            InternalUserAccountService.Object,
            ExternalUserAccountRepository.Object,
            FlaRepository.Object,
            WoodlandOwnerService.Object,
            WoodlandOfficerReviewService.Object,
            UpdateWoodlandOfficerReviewService.Object,
            PublicRegisterService.Object,
            NotificationHistoryService.Object,
            AuditingService.Object,
            MockAgentAuthorityService.Object,
            RequestContext,
            Clock.Object,
            NotificationService.Object,
            GetConfiguredFcAreas.Object,
            new OptionsWrapper<WoodlandOfficerReviewOptions>(WoodlandOfficerReviewOptions),
            new NullLogger<PublicRegisterUseCase>());
    }
}