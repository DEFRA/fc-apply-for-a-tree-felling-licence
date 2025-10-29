using System.Text.Json;
using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Services;
using Forestry.Flo.Internal.Web.Services.FellingLicenceApplication.WoodlandOfficerReview;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Forestry.Flo.Internal.Web.Tests.Services.WoodlandOfficerReviewUseCase;

public class DesignationsUseCaseUpdateDesignationsTests : WoodlandOfficerReviewUseCaseTestsBase<DesignationsUseCase>
{
    [Theory, AutoMoqData]
    public async Task UpdateCompartmentDesignationsWhenServiceReturnsSuccess(
        Guid applicationId,
        SubmittedCompartmentDesignationsModel model,
        Guid userId)
    {
        var userPrincipal = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(localAccountId: userId);
        var user = new InternalUser(userPrincipal);
        var sut = CreateSut();

        UpdateWoodlandOfficerReviewService
            .Setup(x => x.UpdateCompartmentDesignationsAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<SubmittedCompartmentDesignationsModel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var result = await sut.UpdateCompartmentDesignationsAsync(applicationId, model, user, CancellationToken.None);

        Assert.True(result.IsSuccess);

        UpdateWoodlandOfficerReviewService.Verify(x => x.UpdateCompartmentDesignationsAsync(
            applicationId,
            user.UserAccountId.Value,
            model,
            It.IsAny<CancellationToken>()), Times.Once);
        UpdateWoodlandOfficerReviewService.VerifyNoOtherCalls();

        AuditingService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.UpdateDesignations
                && a.ActorType == ActorType.InternalUser
                && a.UserId == userId
                && a.SourceEntityId == applicationId
                && a.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && a.CorrelationId == RequestContextCorrelationId
                && JsonSerializer.Serialize(a.AuditData, SerializerOptions) ==
                JsonSerializer.Serialize(new
                {
                    CompartmentId = model.SubmittedFlaCompartmentId,
                }, SerializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);
        AuditingService.VerifyNoOtherCalls();
    }


    [Theory, AutoMoqData]
    public async Task UpdateCompartmentDesignationsWhenServiceReturnsFailure(
        Guid applicationId,
        SubmittedCompartmentDesignationsModel model,
        Guid userId,
        string error)
    {
        var userPrincipal = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(localAccountId: userId);
        var user = new InternalUser(userPrincipal);
        var sut = CreateSut();

        UpdateWoodlandOfficerReviewService
            .Setup(x => x.UpdateCompartmentDesignationsAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<SubmittedCompartmentDesignationsModel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(error));

        var result = await sut.UpdateCompartmentDesignationsAsync(applicationId, model, user, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(error, result.Error);

        UpdateWoodlandOfficerReviewService.Verify(x => x.UpdateCompartmentDesignationsAsync(
            applicationId,
            user.UserAccountId.Value,
            model,
            It.IsAny<CancellationToken>()), Times.Once);
        UpdateWoodlandOfficerReviewService.VerifyNoOtherCalls();

        AuditingService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.UpdateDesignationsFailure
                && a.ActorType == ActorType.InternalUser
                && a.UserId == userId
                && a.SourceEntityId == applicationId
                && a.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && a.CorrelationId == RequestContextCorrelationId
                && JsonSerializer.Serialize(a.AuditData, SerializerOptions) ==
                JsonSerializer.Serialize(new
                {
                    CompartmentId = model.SubmittedFlaCompartmentId,
                    Error = error
                }, SerializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);
        AuditingService.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task UpdateCompartmentDesignationsCompletionWhenServiceReturnsSuccess(
        Guid applicationId,
        bool isComplete,
        Guid userId)
    {
        var userPrincipal = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(localAccountId: userId);
        var user = new InternalUser(userPrincipal);
        var sut = CreateSut();

        UpdateWoodlandOfficerReviewService
            .Setup(x => x.UpdateApplicationCompartmentDesignationsCompletedAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var result = await sut.UpdateCompartmentDesignationsCompletionAsync(applicationId, user, isComplete, CancellationToken.None);

        Assert.True(result.IsSuccess);

        UpdateWoodlandOfficerReviewService.Verify(x => x.UpdateApplicationCompartmentDesignationsCompletedAsync(
            applicationId,
            user.UserAccountId.Value,
            isComplete,
            It.IsAny<CancellationToken>()), Times.Once);
        UpdateWoodlandOfficerReviewService.VerifyNoOtherCalls();

        AuditingService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.UpdateWoodlandOfficerReview
                && a.ActorType == ActorType.InternalUser
                && a.UserId == userId
                && a.SourceEntityId == applicationId
                && a.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && a.CorrelationId == RequestContextCorrelationId
                && JsonSerializer.Serialize(a.AuditData, SerializerOptions) ==
                JsonSerializer.Serialize(new
                {
                    Section = "Compartment designations",
                }, SerializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);
        AuditingService.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task UpdateCompartmentDesignationsCompletionWhenServiceReturnsFailure(
        Guid applicationId,
        bool isComplete,
        Guid userId,
        string error)
    {
        var userPrincipal = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(localAccountId: userId);
        var user = new InternalUser(userPrincipal);
        var sut = CreateSut();

        UpdateWoodlandOfficerReviewService
            .Setup(x => x.UpdateApplicationCompartmentDesignationsCompletedAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(error));

        var result = await sut.UpdateCompartmentDesignationsCompletionAsync(applicationId, user, isComplete, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(error, result.Error);

        UpdateWoodlandOfficerReviewService.Verify(x => x.UpdateApplicationCompartmentDesignationsCompletedAsync(
            applicationId,
            user.UserAccountId.Value,
            isComplete,
            It.IsAny<CancellationToken>()), Times.Once);
        UpdateWoodlandOfficerReviewService.VerifyNoOtherCalls();

        AuditingService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.UpdateWoodlandOfficerReviewFailure
                && a.ActorType == ActorType.InternalUser
                && a.UserId == userId
                && a.SourceEntityId == applicationId
                && a.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && a.CorrelationId == RequestContextCorrelationId
                && JsonSerializer.Serialize(a.AuditData, SerializerOptions) ==
                JsonSerializer.Serialize(new
                {
                    Section = "Compartment designations",
                    Error = error
                }, SerializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);
        AuditingService.VerifyNoOtherCalls();
    }

    private DesignationsUseCase CreateSut()
    {
        ResetMocks();

        return new DesignationsUseCase(
            InternalUserAccountService.Object,
            ExternalUserAccountRepository.Object,
            FlaRepository.Object,
            WoodlandOwnerService.Object,
            WoodlandOfficerReviewService.Object,
            UpdateWoodlandOfficerReviewService.Object,
            MockAgentAuthorityService.Object,
            GetConfiguredFcAreas.Object,
            AuditingService.Object,
            RequestContext,
            WoodlandOfficerReviewSubStatusService.Object,
            new NullLogger<DesignationsUseCase>());
    }
}