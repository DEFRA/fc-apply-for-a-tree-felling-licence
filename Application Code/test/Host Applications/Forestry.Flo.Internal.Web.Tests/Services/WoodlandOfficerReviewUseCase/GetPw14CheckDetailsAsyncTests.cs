using AutoFixture.Xunit2;
using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Services.FellingLicenceApplication.WoodlandOfficerReview;
using Forestry.Flo.Services.Applicants.Entities.UserAccount;
using Forestry.Flo.Services.Applicants.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Forestry.Flo.Internal.Web.Tests.Services.WoodlandOfficerReviewUseCase;

public class GetPw14CheckDetailsAsyncTests : WoodlandOfficerReviewUseCaseTestsBase<Pw14UseCase>
{
    [Theory, AutoData]
    public async Task ShouldReturnFailureWhenRetrievalOfPw14ChecksDetailsFails(Guid applicationId)
    {
        var sut = CreateSut();
        WoodlandOfficerReviewService
            .Setup(x => x.GetPw14ChecksAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<Maybe<Pw14ChecksModel>>("error"));

        var result = await sut.GetPw14CheckDetailsAsync(applicationId, CancellationToken.None);

        Assert.True(result.IsFailure);

        WoodlandOfficerReviewService.Verify(x => x.GetPw14ChecksAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        WoodlandOfficerReviewService.VerifyNoOtherCalls();

        FlaRepository.VerifyNoOtherCalls();
        InternalUserAccountService.VerifyNoOtherCalls();
        ExternalUserAccountRepository.VerifyNoOtherCalls();
        WoodlandOwnerService.VerifyNoOtherCalls();
        NotificationHistoryService.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task ShouldReturnFailureWhenUnableToLoadApplicationSummary(
        Guid applicationId,
        Pw14ChecksModel pw14Checks)
    {
        var sut = CreateSut();

        WoodlandOfficerReviewService
            .Setup(x => x.GetPw14ChecksAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(Maybe.From(pw14Checks)));

        FlaRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.None);

        var result = await sut.GetPw14CheckDetailsAsync(applicationId, CancellationToken.None);

        Assert.True(result.IsFailure);

        WoodlandOfficerReviewService.Verify(x => x.GetPw14ChecksAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
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
        Pw14ChecksModel pw14Checks,
        FellingLicenceApplication fla,
        WoodlandOwnerModel woodlandOwner,
        UserAccount externalApplicant,
        Flo.Services.InternalUsers.Entities.UserAccount.UserAccount internalUser)
    {
        var sut = CreateSut();

        fla.LinkedPropertyProfile.ProposedFellingDetails = new List<ProposedFellingDetail>();

        WoodlandOfficerReviewService
            .Setup(x => x.GetPw14ChecksAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(Maybe.From(pw14Checks)));

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

        var result = await sut.GetPw14CheckDetailsAsync(applicationId, CancellationToken.None);

        Assert.True(result.IsSuccess);

        Assert.NotNull(result.Value.FellingLicenceApplicationSummary);
        Assert.Equal(pw14Checks, result.Value.Pw14Checks);
        Assert.Equal(applicationId, result.Value.ApplicationId);

        WoodlandOfficerReviewService.Verify(
            x => x.GetPw14ChecksAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
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
            new NullLogger<Pw14UseCase>());
    }
}