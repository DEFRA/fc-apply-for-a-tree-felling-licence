using AutoFixture.Xunit2;
using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Services;
using Forestry.Flo.Internal.Web.Services.FellingLicenceApplication.WoodlandOfficerReview;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.Gis.Models.Esri.Responses.Layers;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NodaTime;
using System.Text.Json;

namespace Forestry.Flo.Internal.Web.Tests.Services.WoodlandOfficerReviewUseCase;

public class RetrieveSiteVisitNotesAsyncTests : WoodlandOfficerReviewUseCaseTestsBase<SiteVisitUseCase>
{
    [Theory, AutoData]
    public async Task WhenGetSiteVisitNotesFails(
        Guid applicationId,
        string applicationReference,
        string error)
    {
        var userPrincipal = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(localAccountId: RequestContextUserId);
        var user = new InternalUser(userPrincipal);
        var sut = CreateSut();

        _forestryServices
            .Setup(x => x.GetVisitNotesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<List<SiteVisitNotes<Guid>>>(error));

        var result = await sut.RetrieveSiteVisitNotesAsync(applicationId, applicationReference, user, CancellationToken.None);

        Assert.True(result.IsFailure);

        _forestryServices.Verify(x => x.GetVisitNotesAsync(applicationReference, It.IsAny<CancellationToken>()), Times.Once);
        _forestryServices.VerifyNoOtherCalls();
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

    [Theory, AutoData]
    public async Task WhenUpdateWoodlandOfficerReviewFails(
        Guid applicationId,
        string applicationReference,
        List<SiteVisitNotes<Guid>> notes,
        string error)
    {
        var userPrincipal = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(localAccountId: RequestContextUserId);
        var user = new InternalUser(userPrincipal);
        var sut = CreateSut();

        var now = new Instant();

        _forestryServices
            .Setup(x => x.GetVisitNotesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(notes));

        UpdateWoodlandOfficerReviewService
            .Setup(x => x.SiteVisitNotesRetrievedAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<List<SiteVisitNotes<Guid>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(error));

        Clock.Setup(x => x.GetCurrentInstant()).Returns(now);

        var result = await sut.RetrieveSiteVisitNotesAsync(applicationId, applicationReference, user, CancellationToken.None);

        Assert.True(result.IsFailure);

        _forestryServices.Verify(x => x.GetVisitNotesAsync(applicationReference, It.IsAny<CancellationToken>()), Times.Once);
        _forestryServices.VerifyNoOtherCalls();
        UpdateWoodlandOfficerReviewService.Verify(x => x.SiteVisitNotesRetrievedAsync(applicationId, user.UserAccountId.Value, now.ToDateTimeUtc(), notes, It.IsAny<CancellationToken>()), Times.Once);
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

    [Theory, AutoData]
    public async Task WhenUpdateSuccessful(
        Guid applicationId,
        string applicationReference,
        List<SiteVisitNotes<Guid>> notes)
    {
        var userPrincipal = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(localAccountId: RequestContextUserId);
        var user = new InternalUser(userPrincipal);
        var sut = CreateSut();

        var now = new Instant();

        _forestryServices
            .Setup(x => x.GetVisitNotesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(notes));

        UpdateWoodlandOfficerReviewService
            .Setup(x => x.SiteVisitNotesRetrievedAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<List<SiteVisitNotes<Guid>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        Clock.Setup(x => x.GetCurrentInstant()).Returns(now);

        var result = await sut.RetrieveSiteVisitNotesAsync(applicationId, applicationReference, user, CancellationToken.None);

        Assert.True(result.IsSuccess);

        _forestryServices.Verify(x => x.GetVisitNotesAsync(applicationReference, It.IsAny<CancellationToken>()), Times.Once);
        _forestryServices.VerifyNoOtherCalls();
        UpdateWoodlandOfficerReviewService.Verify(x => x.SiteVisitNotesRetrievedAsync(applicationId, user.UserAccountId.Value, now.ToDateTimeUtc(), notes, It.IsAny<CancellationToken>()), Times.Once);
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
                    section = "Site Visit"
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
                    processCompletedDate = now.ToDateTimeUtc()
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