using System.Text.Json;
using AutoFixture.Xunit2;
using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Services;
using Forestry.Flo.Internal.Web.Services.FellingLicenceApplication.WoodlandOfficerReview;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview;
using Forestry.Flo.Services.Gis.Models.Internal;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Forestry.Flo.Internal.Web.Tests.Services.WoodlandOfficerReviewUseCase;

public class GenerateSiteVisitArtefactsAsyncTests : WoodlandOfficerReviewUseCaseTestsBase<SiteVisitUseCase>
{
    [Theory, AutoData]
    public async Task WhenNoApplicationDocumentExistsAndGenerationFails(
        Guid applicationId,
        string error)
    {
        var userPrincipal = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(localAccountId: RequestContextUserId);
        var user = new InternalUser(userPrincipal);
        var sut = CreateSut();

        WoodlandOfficerReviewService
            .Setup(x => x.GetApplicationDetailsForSiteVisitMobileLayersAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<ApplicationDetailsForSiteVisitMobileLayers>(error));

        var result = await sut.GenerateSiteVisitArtefactsAsync(applicationId, user, CancellationToken.None);

        Assert.True(result.IsFailure);

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
    public async Task WhenGetApplicationDetailsForMobileAppsFails(
        Guid applicationId,
        string error)
    {
        var userPrincipal = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(localAccountId: RequestContextUserId);
        var user = new InternalUser(userPrincipal);
        var sut = CreateSut();

        WoodlandOfficerReviewService
            .Setup(x => x.GetApplicationDetailsForSiteVisitMobileLayersAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<ApplicationDetailsForSiteVisitMobileLayers>(error));

        var result = await sut.GenerateSiteVisitArtefactsAsync(applicationId, user, CancellationToken.None);

        Assert.True(result.IsFailure);

        CreateApplicationDocument.VerifyNoOtherCalls();
        WoodlandOfficerReviewService.Verify(x => x.GetApplicationDetailsForSiteVisitMobileLayersAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        WoodlandOfficerReviewService.VerifyNoOtherCalls();
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
    public async Task WhenSaveCaseToMobileLayersFails(
        Guid applicationId,
        ApplicationDetailsForSiteVisitMobileLayers model,
        string error)
    {
        var userPrincipal = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(localAccountId: RequestContextUserId);
        var user = new InternalUser(userPrincipal);
        var sut = CreateSut();

        WoodlandOfficerReviewService
            .Setup(x => x.GetApplicationDetailsForSiteVisitMobileLayersAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(model));

        _forestryServices
            .Setup(x => x.SavesCaseToMobileLayersAsync(It.IsAny<string>(), It.IsAny<List<InternalFullCompartmentDetails>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(error));

        var result = await sut.GenerateSiteVisitArtefactsAsync(applicationId, user, CancellationToken.None);

        Assert.True(result.IsFailure);

        CreateApplicationDocument.VerifyNoOtherCalls();
        WoodlandOfficerReviewService.Verify(x => x.GetApplicationDetailsForSiteVisitMobileLayersAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once); 
        WoodlandOfficerReviewService.VerifyNoOtherCalls();
        _forestryServices.Verify(x => x.SavesCaseToMobileLayersAsync(model.CaseReference, model.Compartments, It.IsAny<CancellationToken>()), Times.Once);
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
        ApplicationDetailsForSiteVisitMobileLayers model,
        string error)
    {
        var userPrincipal = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(localAccountId: RequestContextUserId);
        var user = new InternalUser(userPrincipal);
        var sut = CreateSut();

        WoodlandOfficerReviewService
            .Setup(x => x.GetApplicationDetailsForSiteVisitMobileLayersAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(model));

        _forestryServices
            .Setup(x => x.SavesCaseToMobileLayersAsync(It.IsAny<string>(), It.IsAny<List<InternalFullCompartmentDetails>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        UpdateWoodlandOfficerReviewService
            .Setup(x => x.PublishedToSiteVisitMobileLayersAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(error));

        var result = await sut.GenerateSiteVisitArtefactsAsync(applicationId, user, CancellationToken.None);

        Assert.True(result.IsFailure);

        CreateApplicationDocument.VerifyNoOtherCalls();
        WoodlandOfficerReviewService.Verify(x => x.GetApplicationDetailsForSiteVisitMobileLayersAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        WoodlandOfficerReviewService.VerifyNoOtherCalls();
        _forestryServices.Verify(x => x.SavesCaseToMobileLayersAsync(model.CaseReference, model.Compartments, It.IsAny<CancellationToken>()), Times.Once);
        _forestryServices.VerifyNoOtherCalls();
        UpdateWoodlandOfficerReviewService.Verify(x => x.PublishedToSiteVisitMobileLayersAsync(applicationId, user.UserAccountId.Value, Now.ToDateTimeUtc(), It.IsAny<CancellationToken>()), Times.Once);
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
    public async Task WhenSuccessfulWithApplicationDocumentExists(
        Guid applicationId,
        ApplicationDetailsForSiteVisitMobileLayers model)
    {
        var userPrincipal = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(localAccountId: RequestContextUserId);
        var user = new InternalUser(userPrincipal);
        var sut = CreateSut();

        WoodlandOfficerReviewService
            .Setup(x => x.GetApplicationDetailsForSiteVisitMobileLayersAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(model));

        _forestryServices
            .Setup(x => x.SavesCaseToMobileLayersAsync(It.IsAny<string>(), It.IsAny<List<InternalFullCompartmentDetails>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        UpdateWoodlandOfficerReviewService
            .Setup(x => x.PublishedToSiteVisitMobileLayersAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var result = await sut.GenerateSiteVisitArtefactsAsync(applicationId, user, CancellationToken.None);

        Assert.True(result.IsSuccess);

        CreateApplicationDocument.VerifyNoOtherCalls();
        WoodlandOfficerReviewService.Verify(x => x.GetApplicationDetailsForSiteVisitMobileLayersAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        WoodlandOfficerReviewService.VerifyNoOtherCalls();
        _forestryServices.Verify(x => x.SavesCaseToMobileLayersAsync(model.CaseReference, model.Compartments, It.IsAny<CancellationToken>()), Times.Once);
        _forestryServices.VerifyNoOtherCalls();
        UpdateWoodlandOfficerReviewService.Verify(x => x.PublishedToSiteVisitMobileLayersAsync(applicationId, user.UserAccountId.Value, Now.ToDateTimeUtc(), It.IsAny<CancellationToken>()), Times.Once);
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
                    processStartedDate = Now.ToDateTimeUtc()
                }, SerializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);
        AuditingService.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task WhenSuccessfulWithApplicationDocumentDoesNotExist(
        Guid applicationId,
        Guid newDocumentGuid,
        ApplicationDetailsForSiteVisitMobileLayers model)
    {
        var userPrincipal = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(localAccountId: RequestContextUserId);
        var user = new InternalUser(userPrincipal);
        var sut = CreateSut();

        //CreateApplicationDocument
        //    .Setup(x => x.CreateApplicationSnapshotAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
        //    .ReturnsAsync(Result.Success(newDocumentGuid));

        WoodlandOfficerReviewService
            .Setup(x => x.GetApplicationDetailsForSiteVisitMobileLayersAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(model));

        _forestryServices
            .Setup(x => x.SavesCaseToMobileLayersAsync(It.IsAny<string>(), It.IsAny<List<InternalFullCompartmentDetails>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        UpdateWoodlandOfficerReviewService
            .Setup(x => x.PublishedToSiteVisitMobileLayersAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var result = await sut.GenerateSiteVisitArtefactsAsync(applicationId, user, CancellationToken.None);

        Assert.True(result.IsSuccess);

        WoodlandOfficerReviewService.Verify(x => x.GetApplicationDetailsForSiteVisitMobileLayersAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        WoodlandOfficerReviewService.VerifyNoOtherCalls();
        _forestryServices.Verify(x => x.SavesCaseToMobileLayersAsync(model.CaseReference,model.Compartments, It.IsAny<CancellationToken>()), Times.Once);
        _forestryServices.VerifyNoOtherCalls();
        UpdateWoodlandOfficerReviewService.Verify(x => x.PublishedToSiteVisitMobileLayersAsync(applicationId, user.UserAccountId.Value, Now.ToDateTimeUtc(), It.IsAny<CancellationToken>()), Times.Once);
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
                    processStartedDate = Now.ToDateTimeUtc()
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