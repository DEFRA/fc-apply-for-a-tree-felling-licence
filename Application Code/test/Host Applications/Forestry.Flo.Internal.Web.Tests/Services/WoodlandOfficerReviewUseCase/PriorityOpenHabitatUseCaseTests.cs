using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Services.FellingLicenceApplication.WoodlandOfficerReview;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Text.Json;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Internal.Web.Services;
using Forestry.Flo.Services.Applicants.Entities.UserAccount;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.Applicants.Models;

namespace Forestry.Flo.Internal.Web.Tests.Services.WoodlandOfficerReviewUseCase;

public class PriorityOpenHabitatUseCaseTests : WoodlandOfficerReviewUseCaseTestsBase<PriorityOpenHabitatUseCase>
{
    private readonly Mock<IHabitatRestorationService> _habitatRestorationService = new();

    private PriorityOpenHabitatUseCase CreateSut()
    {
        ResetMocks();
        _habitatRestorationService.Reset();

        return new PriorityOpenHabitatUseCase(
            InternalUserAccountService.Object,
            ExternalUserAccountRepository.Object,
            FlaRepository.Object,
            WoodlandOwnerService.Object,
            _habitatRestorationService.Object,
            UpdateWoodlandOfficerReviewService.Object,
            AuditingService.Object,
            RequestContext,
            MockAgentAuthorityService.Object,
            GetConfiguredFcAreas.Object,
            WoodlandOfficerReviewSubStatusService.Object,
            new NullLogger<PriorityOpenHabitatUseCase>());
    }

    [Theory, AutoMoqData]
    public async Task CompletePriorityOpenHabitatAsync_ReturnsSuccess_WhenUpdateSucceeds(
        Guid applicationId,
        bool isComplete)
    {
        var user = new InternalUser(
            UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(localAccountId: RequestContextUserId));
        var sut = CreateSut();

        UpdateWoodlandOfficerReviewService
            .Setup(x => x.CompletePriorityOpenHabitatAsync(applicationId, user.UserAccountId!.Value, isComplete, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var result = await sut.CompletePriorityOpenHabitatAsync(applicationId, user, isComplete, CancellationToken.None);

        Assert.True(result.IsSuccess);

        AuditingService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.UpdateWoodlandOfficerReview
                && a.SourceEntityId == applicationId
                && JsonSerializer.Serialize(a.AuditData, SerializerOptions).Contains("Priority Open Habitat")),
            It.IsAny<CancellationToken>()), Times.Once);

        AuditingService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.WoodlandOfficerReviewPriorityOpenHabitat
                && a.SourceEntityId == applicationId
                && JsonSerializer.Serialize(a.AuditData, SerializerOptions).Contains($"\"isComplete\":{isComplete.ToString().ToLower()}")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task CompletePriorityOpenHabitatAsync_ReturnsFailure_WhenUpdateFails(
        Guid applicationId,
        bool isComplete,
        string error)
    {
        var user = new InternalUser(
            UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(localAccountId: RequestContextUserId));
        var sut = CreateSut();

        UpdateWoodlandOfficerReviewService
            .Setup(x => x.CompletePriorityOpenHabitatAsync(applicationId, user.UserAccountId!.Value, isComplete, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(error));

        var result = await sut.CompletePriorityOpenHabitatAsync(applicationId, user, isComplete, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(error, result.Error);

        AuditingService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.UpdateWoodlandOfficerReviewFailure
                && a.SourceEntityId == applicationId
                && JsonSerializer.Serialize(a.AuditData, SerializerOptions).Contains("Priority Open Habitat")),
            It.IsAny<CancellationToken>()), Times.Once);

        AuditingService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.WoodlandOfficerReviewPriorityOpenHabitatFailure
                && a.SourceEntityId == applicationId
                && JsonSerializer.Serialize(a.AuditData, SerializerOptions).Contains(error)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task GetPriorityOpenHabitatsAsync_ReturnsFailure_WhenApplicationNotFound(
        Guid applicationId)
    {
        var sut = CreateSut();

        FlaRepository.Setup(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.None);

        var result = await sut.GetPriorityOpenHabitatsAsync(applicationId, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Could not locate Felling Licence Application with the given id", result.Error);
    }

    [Theory, AutoMoqData]
    public async Task GetPriorityOpenHabitatsAsync_ReturnsSuccess_WhenApplicationFound(
        Guid applicationId,
        FellingLicenceApplication application,
        WoodlandOwnerModel woodlandOwnerModel,
        List<HabitatRestorationModel> restorations,
        WoodlandOfficerReview woodlandOfficerReview)
    {
        var sut = CreateSut();
        woodlandOwnerModel.Id = Guid.NewGuid();
        application.Id = applicationId;
        application.WoodlandOwnerId = woodlandOwnerModel.Id.Value;
        application.AssigneeHistories = new List<AssigneeHistory>();
        application.StatusHistories = new List<StatusHistory>();
        application.Documents = new List<Document>();
        application.SubmittedFlaPropertyDetail = new SubmittedFlaPropertyDetail
        {
            SubmittedFlaPropertyCompartments = new List<SubmittedFlaPropertyCompartment>()
        };
        application.LinkedPropertyProfile = new LinkedPropertyProfile
        {
            ProposedFellingDetails = new List<ProposedFellingDetail>()
        };

        FlaRepository.Setup(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.From(application));

        WoodlandOwnerService.Setup(x => x.RetrieveWoodlandOwnerByIdAsync(woodlandOwnerModel.Id.Value, It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(woodlandOwnerModel));

        MockAgentAuthorityService.Setup(x => x.GetAgencyForWoodlandOwnerAsync(woodlandOwnerModel.Id.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<AgencyModel>.None);

        _habitatRestorationService.Setup(x => x.GetHabitatRestorationModelsAsync(applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(restorations);

        FlaRepository.Setup(x => x.GetWoodlandOfficerReviewAsync(applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<WoodlandOfficerReview>.From(woodlandOfficerReview));

        var result = await sut.GetPriorityOpenHabitatsAsync(applicationId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(applicationId, result.Value.ApplicationId);
        Assert.Equal(restorations.Count, result.Value.Habitats.Count);
        Assert.Equal(woodlandOfficerReview.PriorityOpenHabitatComplete, result.Value.AreDetailsCorrect);
        Assert.NotNull(result.Value.Breadcrumbs);
    }
}
