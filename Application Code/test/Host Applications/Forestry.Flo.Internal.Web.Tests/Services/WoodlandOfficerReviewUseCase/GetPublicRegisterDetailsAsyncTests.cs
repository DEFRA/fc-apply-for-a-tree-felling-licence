using AutoFixture.Xunit2;
using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Services.FellingLicenceApplication.WoodlandOfficerReview;
using Forestry.Flo.Services.Applicants.Entities.UserAccount;
using Forestry.Flo.Services.Applicants.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Configuration;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.Notifications.Entities;
using Forestry.Flo.Services.Notifications.Models;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Forestry.Flo.Internal.Web.Tests.Services.WoodlandOfficerReviewUseCase;

public class GetPublicRegisterDetailsAsyncTests : WoodlandOfficerReviewUseCaseTestsBase<PublicRegisterUseCase>
{
    [Theory, AutoData]
    public async Task ShouldReturnFailureWhenRetrievalOfPublicRegisterDetailsFails(
        Guid applicationId)
    {
        var sut = CreateSut();
        WoodlandOfficerReviewService
            .Setup(x => x.GetPublicRegisterDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<Maybe<PublicRegisterModel>>("error"));

        var result = await sut.GetPublicRegisterDetailsAsync(applicationId, CancellationToken.None);

        Assert.True(result.IsFailure);

        WoodlandOfficerReviewService.Verify(x => x.GetPublicRegisterDetailsAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        WoodlandOfficerReviewService.VerifyNoOtherCalls();

        FlaRepository.VerifyNoOtherCalls();
        InternalUserAccountService.VerifyNoOtherCalls();
        ExternalUserAccountRepository.VerifyNoOtherCalls();
        WoodlandOwnerService.VerifyNoOtherCalls();
        NotificationHistoryService.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task ShouldReturnFailureWhenUnableToLoadApplicationSummary(Guid applicationId, PublicRegisterModel publicRegister)
    {
        var sut = CreateSut();

        WoodlandOfficerReviewService
            .Setup(x => x.GetPublicRegisterDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(Maybe.From(publicRegister)));

        FlaRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.None);

        var result = await sut.GetPublicRegisterDetailsAsync(applicationId, CancellationToken.None);

        Assert.True(result.IsFailure);

        WoodlandOfficerReviewService.Verify(x => x.GetPublicRegisterDetailsAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FlaRepository.Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        WoodlandOwnerService.VerifyNoOtherCalls();
        ExternalUserAccountRepository.VerifyNoOtherCalls();
        InternalUserAccountService.VerifyNoOtherCalls();
        NotificationHistoryService.VerifyNoOtherCalls();
    }

    //It is assumed that the various other permutations of outcome for FellingLicenceApplicationUseCaseBase.GetFellingLicenceDetailsAsync are already tested - see AssignToUserUseCaseTestsBase

    [Theory, AutoMoqData]
    public async Task ShouldReturnSuccessWhenDataIsSuccessfullyRetrieved(
    Guid applicationId,
    PublicRegisterModel publicRegister,
    FellingLicenceApplication fla,
    WoodlandOwnerModel woodlandOwner,
    UserAccount externalApplicant,
    Flo.Services.InternalUsers.Entities.UserAccount.UserAccount internalUser,
    List<NotificationHistoryModel> notifications)
    {
        var sut = CreateSut();

        fla.LinkedPropertyProfile.ProposedFellingDetails = new List<ProposedFellingDetail>();

        WoodlandOfficerReviewService
            .Setup(x => x.GetPublicRegisterDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(Maybe.From(publicRegister)));

        FlaRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.From(fla));

        WoodlandOwnerService
            .Setup(x => x.RetrieveWoodlandOwnerByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(woodlandOwner));

        ExternalUserAccountRepository
            .Setup(x => x.RetrieveUserAccountEntityByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(externalApplicant));

        InternalUserAccountService
            .Setup(x => x.GetUserAccountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe.From(internalUser));

        NotificationHistoryService
            .Setup(x => x.RetrieveNotificationHistoryAsync(It.IsAny<Guid>(), It.IsAny<NotificationType[]?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(notifications));

        var result = await sut.GetPublicRegisterDetailsAsync(applicationId, CancellationToken.None);

        Assert.True(result.IsSuccess);

        Assert.NotNull(result.Value.FellingLicenceApplicationSummary);
        Assert.Equal(publicRegister, result.Value.PublicRegister);

        WoodlandOfficerReviewService.Verify(x => x.GetPublicRegisterDetailsAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        NotificationHistoryService.Verify(x => x.RetrieveNotificationHistoryAsync(applicationId, new [] {  NotificationType.PublicRegisterComment  }, It.IsAny<CancellationToken>()), Times.Once);
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