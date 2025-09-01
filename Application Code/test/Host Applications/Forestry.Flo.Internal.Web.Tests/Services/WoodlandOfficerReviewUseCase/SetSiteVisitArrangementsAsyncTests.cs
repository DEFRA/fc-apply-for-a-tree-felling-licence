using System.Text.Json;
using AutoFixture.Xunit2;
using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Services;
using Forestry.Flo.Internal.Web.Services.FellingLicenceApplication.WoodlandOfficerReview;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Forestry.Flo.Internal.Web.Tests.Services.WoodlandOfficerReviewUseCase;

public class SetSiteVisitArrangementsAsyncTests : WoodlandOfficerReviewUseCaseTestsBase<SiteVisitUseCase>
{
    [Theory, AutoData]
    public async Task ShouldStoreCorrectAuditOnSuccessfulUpdate(
        Guid applicationId,
        bool? arrangementsMade,
        FormLevelCaseNote arrangements)
    {
        var userPrincipal = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(localAccountId: RequestContextUserId);
        var user = new InternalUser(userPrincipal);
        var sut = CreateSut();

        UpdateWoodlandOfficerReviewService
            .Setup(x => x.SaveSiteVisitArrangementsAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<bool?>(), It.IsAny<FormLevelCaseNote>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var result = await sut.SetSiteVisitArrangementsAsync(applicationId, user, arrangementsMade, arrangements, CancellationToken.None);

        Assert.True(result.IsSuccess);

        UpdateWoodlandOfficerReviewService.Verify(x => x.SaveSiteVisitArrangementsAsync(applicationId, RequestContextUserId, arrangementsMade, arrangements, It.IsAny<CancellationToken>()), Times.Once);
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
                        arrangementsMade,
                        arrangementsNotes = arrangements.CaseNote
                    }, SerializerOptions)),

                It.IsAny<CancellationToken>()), Times.Once);
        AuditingService.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task ShouldStoreCorrectFailureAuditOnUnsuccessfulUpdate(
        Guid applicationId,
        bool? arrangementsMade,
        FormLevelCaseNote arrangements,
        string error)
    {
        var userPrincipal = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(localAccountId: RequestContextUserId);
        var user = new InternalUser(userPrincipal);
        var sut = CreateSut();

        UpdateWoodlandOfficerReviewService
            .Setup(x => x.SaveSiteVisitArrangementsAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<bool?>(), It.IsAny<FormLevelCaseNote>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(error));

        var result = await sut.SetSiteVisitArrangementsAsync(applicationId, user, arrangementsMade, arrangements, CancellationToken.None);

        Assert.True(result.IsFailure);

        UpdateWoodlandOfficerReviewService.Verify(x => x.SaveSiteVisitArrangementsAsync(applicationId, RequestContextUserId, arrangementsMade, arrangements, It.IsAny<CancellationToken>()), Times.Once);
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
            AuditingService.Object,
            MockAgentAuthorityService.Object,
            GetConfiguredFcAreas.Object,
            RequestContext,
            new NullLogger<SiteVisitUseCase>());
    }
}