using System.Text.Json;
using AutoFixture.Xunit2;
using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Models.WoodlandOfficerReview;
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

public class RemoveFromConsultationPublicRegisterAsyncTests : WoodlandOfficerReviewUseCaseTestsBase<PublicRegisterUseCase>
{
    [Theory, AutoData]
    public async Task OnSuccessfulUpdate(
        Guid applicationId,
        RemoveFromPublicRegisterModel model)
    {
        var userPrincipal = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(localAccountId: RequestContextUserId);
        var user = new InternalUser(userPrincipal);
        var sut = CreateSut();

        PublicRegisterService
            .Setup(x => x.RemoveCaseFromConsultationRegisterAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success);

        UpdateWoodlandOfficerReviewService
            .Setup(x => x.RemovedFromPublicRegisterAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var result = await sut.RemoveFromPublicRegisterAsync(applicationId, user, model.EsriId.Value, model.ApplicationReference, CancellationToken.None);

        Assert.True(result.IsSuccess);

        PublicRegisterService.Verify(x => x.RemoveCaseFromConsultationRegisterAsync(model.EsriId.Value, model.ApplicationReference, Now.ToDateTimeUtc(), It.IsAny<CancellationToken>()), Times.Once);
        PublicRegisterService.VerifyNoOtherCalls();

        UpdateWoodlandOfficerReviewService.Verify(x => x.RemovedFromPublicRegisterAsync(applicationId, RequestContextUserId, Now.ToDateTimeUtc(), It.IsAny<CancellationToken>()), Times.Once);
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
                    section = "Public Register"
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
                    removalDate = Now.ToDateTimeUtc(),
                    esriId = model.EsriId
                }, SerializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);
        AuditingService.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task OnUnsuccessfulRemovalFromPublicRegister(
        Guid applicationId,
        RemoveFromPublicRegisterModel model,
        string error)
    {
        var userPrincipal = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(localAccountId: RequestContextUserId);
        var user = new InternalUser(userPrincipal);
        var sut = CreateSut();

        PublicRegisterService
            .Setup(x => x.RemoveCaseFromConsultationRegisterAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(error));

        var result = await sut.RemoveFromPublicRegisterAsync(applicationId, user, model.EsriId.Value, model.ApplicationReference, CancellationToken.None);

        Assert.True(result.IsFailure);

        PublicRegisterService.Verify(x => x.RemoveCaseFromConsultationRegisterAsync(model.EsriId.Value, model.ApplicationReference, Now.ToDateTimeUtc(), It.IsAny<CancellationToken>()), Times.Once);
        PublicRegisterService.VerifyNoOtherCalls();

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

    [Theory, AutoData]
    public async Task OnUnsuccessfulDatabaseUpdate(
        Guid applicationId,
        RemoveFromPublicRegisterModel model,
        string error)
    {
        var userPrincipal = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(localAccountId: RequestContextUserId);
        var user = new InternalUser(userPrincipal);
        var sut = CreateSut();

        PublicRegisterService
            .Setup(x => x.RemoveCaseFromConsultationRegisterAsync(It.IsAny<int>(), It.IsAny<string>(),  It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success);

        UpdateWoodlandOfficerReviewService
            .Setup(x => x.RemovedFromPublicRegisterAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(error));

        var result = await sut.RemoveFromPublicRegisterAsync(applicationId, user, model.EsriId.Value, model.ApplicationReference, CancellationToken.None);

        Assert.True(result.IsFailure);

        PublicRegisterService.Verify(x => x.RemoveCaseFromConsultationRegisterAsync(model.EsriId.Value, model.ApplicationReference, Now.ToDateTimeUtc(), It.IsAny<CancellationToken>()), Times.Once);
        PublicRegisterService.VerifyNoOtherCalls();

        UpdateWoodlandOfficerReviewService.Verify(x => x.RemovedFromPublicRegisterAsync(applicationId, RequestContextUserId, Now.ToDateTimeUtc(), It.IsAny<CancellationToken>()), Times.Once);
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
            WoodlandOfficerReviewSubStatusService.Object,
            new NullLogger<PublicRegisterUseCase>());
    }
}