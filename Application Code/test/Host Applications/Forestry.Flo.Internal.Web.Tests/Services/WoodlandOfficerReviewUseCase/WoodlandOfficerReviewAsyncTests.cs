using AutoFixture.Xunit2;
using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Services;
using Forestry.Flo.Services.Applicants.Entities.UserAccount;
using Forestry.Flo.Services.Applicants.Models;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Forestry.Flo.Internal.Web.Tests.Services.WoodlandOfficerReviewUseCase;

public class WoodlandOfficerReviewAsyncTests: WoodlandOfficerReviewUseCaseTestsBase<Web.Services.FellingLicenceApplication.WoodlandOfficerReview.WoodlandOfficerReviewUseCase>
{
    [Theory, AutoData]
    public async Task ShouldReturnFailureWhenCannotGetReviewStatus(Guid applicationId, string hostingPage)
    {
        var user = new InternalUser(UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal());
        var sut = CreateSut();

        WoodlandOfficerReviewService
            .Setup(x => x.GetWoodlandOfficerReviewStatusAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<WoodlandOfficerReviewStatusModel>("error"));

        var result = await sut.WoodlandOfficerReviewAsync(applicationId, user, hostingPage, CancellationToken.None);

        Assert.True(result.IsFailure);

        WoodlandOfficerReviewService.Verify(x => x.GetWoodlandOfficerReviewStatusAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);

        FlaRepository.VerifyNoOtherCalls();
        WoodlandOwnerService.VerifyNoOtherCalls();
        ExternalUserAccountRepository.VerifyNoOtherCalls();
        InternalUserAccountService.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task ShouldReturnFailureWhenUnableToLoadApplicationSummary(Guid applicationId, string hostingPage, WoodlandOfficerReviewStatusModel review)
    {
        var user = new InternalUser(UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal());
        var sut = CreateSut();

        WoodlandOfficerReviewService
            .Setup(x => x.GetWoodlandOfficerReviewStatusAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(review));

        FlaRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.None);

        var result = await sut.WoodlandOfficerReviewAsync(applicationId, user, hostingPage, CancellationToken.None);

        Assert.True(result.IsFailure);

        WoodlandOfficerReviewService.Verify(x => x.GetWoodlandOfficerReviewStatusAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FlaRepository.Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        WoodlandOwnerService.VerifyNoOtherCalls();
        ExternalUserAccountRepository.VerifyNoOtherCalls();
        InternalUserAccountService.VerifyNoOtherCalls();
    }

    //It is assumed that the various other permutations of outcome for FellingLicenceApplicationUseCaseBase.GetFellingLicenceDetailsAsync are already tested - see AssignToUserUseCaseTestsBase

    [Theory, AutoMoqData]
    public async Task ShouldReturnSuccessWhenDataIsSuccessfullyRetrieved(
        Guid applicationId,
        string hostingPage,
        WoodlandOfficerReviewStatusModel review,
        FellingLicenceApplication fla,
        WoodlandOwnerModel woodlandOwner,
        UserAccount externalApplicant,
        Flo.Services.InternalUsers.Entities.UserAccount.UserAccount internalUser)
    {
        var user = new InternalUser(UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal());
        var sut = CreateSut();

        fla.LinkedPropertyProfile.ProposedFellingDetails = new List<ProposedFellingDetail>();

        WoodlandOfficerReviewService
            .Setup(x => x.GetWoodlandOfficerReviewStatusAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(review));

        FlaRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.From(fla));

        WoodlandOwnerService
            .Setup(x => x.RetrieveWoodlandOwnerByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(woodlandOwner));

        ExternalUserAccountRepository
            .Setup(x => x.RetrieveUserAccountEntityByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(externalApplicant));

        InternalUserAccountService
            .Setup(x => x.GetUserAccountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe.From(internalUser));

        var result = await sut.WoodlandOfficerReviewAsync(applicationId, user, hostingPage, CancellationToken.None);

        Assert.True(result.IsSuccess);

        Assert.NotNull(result.Value.FellingLicenceApplicationSummary);
        Assert.Equal(review.WoodlandOfficerReviewTaskListStates, result.Value.WoodlandOfficerReviewTaskListStates);
        Assert.Equal(review.RecommendedLicenceDuration, result.Value.RecommendedLicenceDuration);
        Assert.Equal(review.RecommendationForDecisionPublicRegister, result.Value.RecommendationForDecisionPublicRegister);
        Assert.Equal(review.RecommendationForDecisionPublicRegisterReason, result.Value.RecommendationForDecisionPublicRegisterReason);
        Assert.Equal(applicationId, result.Value.WoodlandOfficerReviewCommentsFeed.ApplicationId);
        Assert.Equal(hostingPage, result.Value.WoodlandOfficerReviewCommentsFeed.HostingPage);
        Assert.Equal(CaseNoteType.WoodlandOfficerReviewComment, result.Value.WoodlandOfficerReviewCommentsFeed.NewCaseNoteType);
        Assert.Equal(CaseNoteType.WoodlandOfficerReviewComment, result.Value.WoodlandOfficerReviewCommentsFeed.DefaultCaseNoteFilter);
        Assert.False(result.Value.WoodlandOfficerReviewCommentsFeed.ShowFilters);

        WoodlandOfficerReviewService.Verify(x => x.GetWoodlandOfficerReviewStatusAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task ShouldReturnSuccessWhenDataIsSuccessfullyRetrievedWithDefaultDurationFromConfig(
        Guid applicationId,
        string hostingPage,
        WoodlandOfficerReviewStatusModel review,
        FellingLicenceApplication fla,
        WoodlandOwnerModel woodlandOwner,
        UserAccount externalApplicant,
        Flo.Services.InternalUsers.Entities.UserAccount.UserAccount internalUser)
    {
        var user = new InternalUser(UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal());
        var sut = CreateSut();

        fla.IsForTenYearLicence = false;
        review.RecommendedLicenceDuration = null;

        fla.LinkedPropertyProfile.ProposedFellingDetails = new List<ProposedFellingDetail>();

        WoodlandOfficerReviewService
            .Setup(x => x.GetWoodlandOfficerReviewStatusAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(review));

        FlaRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.From(fla));

        WoodlandOwnerService
            .Setup(x => x.RetrieveWoodlandOwnerByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(woodlandOwner));

        ExternalUserAccountRepository
            .Setup(x => x.RetrieveUserAccountEntityByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(externalApplicant));

        InternalUserAccountService
            .Setup(x => x.GetUserAccountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe.From(internalUser));

        var result = await sut.WoodlandOfficerReviewAsync(applicationId, user, hostingPage, CancellationToken.None);

        Assert.True(result.IsSuccess);

        Assert.NotNull(result.Value.FellingLicenceApplicationSummary);
        Assert.Equal(review.WoodlandOfficerReviewTaskListStates, result.Value.WoodlandOfficerReviewTaskListStates);
        Assert.Equal(RecommendedLicenceDuration.FiveYear, result.Value.RecommendedLicenceDuration);
        Assert.Equal(review.RecommendationForDecisionPublicRegister, result.Value.RecommendationForDecisionPublicRegister);
        Assert.Equal(review.RecommendationForDecisionPublicRegisterReason, result.Value.RecommendationForDecisionPublicRegisterReason);
        Assert.Equal(applicationId, result.Value.WoodlandOfficerReviewCommentsFeed.ApplicationId);
        Assert.Equal(hostingPage, result.Value.WoodlandOfficerReviewCommentsFeed.HostingPage);
        Assert.Equal(CaseNoteType.WoodlandOfficerReviewComment, result.Value.WoodlandOfficerReviewCommentsFeed.NewCaseNoteType);
        Assert.Equal(CaseNoteType.WoodlandOfficerReviewComment, result.Value.WoodlandOfficerReviewCommentsFeed.DefaultCaseNoteFilter);
        Assert.False(result.Value.WoodlandOfficerReviewCommentsFeed.ShowFilters);

        WoodlandOfficerReviewService.Verify(x => x.GetWoodlandOfficerReviewStatusAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task ShouldReturnSuccessWhenDataIsSuccessfullyRetrievedWithTenYearLicenceDefaultDuration(
        Guid applicationId,
        string hostingPage,
        WoodlandOfficerReviewStatusModel review,
        FellingLicenceApplication fla,
        WoodlandOwnerModel woodlandOwner,
        UserAccount externalApplicant,
        Flo.Services.InternalUsers.Entities.UserAccount.UserAccount internalUser)
    {
        var user = new InternalUser(UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal());
        var sut = CreateSut();

        fla.IsForTenYearLicence = true;
        review.RecommendedLicenceDuration = null;

        fla.LinkedPropertyProfile.ProposedFellingDetails = new List<ProposedFellingDetail>();

        WoodlandOfficerReviewService
            .Setup(x => x.GetWoodlandOfficerReviewStatusAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(review));

        FlaRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.From(fla));

        WoodlandOwnerService
            .Setup(x => x.RetrieveWoodlandOwnerByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(woodlandOwner));

        ExternalUserAccountRepository
            .Setup(x => x.RetrieveUserAccountEntityByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(externalApplicant));

        InternalUserAccountService
            .Setup(x => x.GetUserAccountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe.From(internalUser));

        var result = await sut.WoodlandOfficerReviewAsync(applicationId, user, hostingPage, CancellationToken.None);

        Assert.True(result.IsSuccess);

        Assert.NotNull(result.Value.FellingLicenceApplicationSummary);
        Assert.Equal(review.WoodlandOfficerReviewTaskListStates, result.Value.WoodlandOfficerReviewTaskListStates);
        Assert.Equal(RecommendedLicenceDuration.TenYear, result.Value.RecommendedLicenceDuration);
        Assert.Equal(review.RecommendationForDecisionPublicRegister, result.Value.RecommendationForDecisionPublicRegister);
        Assert.Equal(review.RecommendationForDecisionPublicRegisterReason, result.Value.RecommendationForDecisionPublicRegisterReason);
        Assert.Equal(applicationId, result.Value.WoodlandOfficerReviewCommentsFeed.ApplicationId);
        Assert.Equal(hostingPage, result.Value.WoodlandOfficerReviewCommentsFeed.HostingPage);
        Assert.Equal(CaseNoteType.WoodlandOfficerReviewComment, result.Value.WoodlandOfficerReviewCommentsFeed.NewCaseNoteType);
        Assert.Equal(CaseNoteType.WoodlandOfficerReviewComment, result.Value.WoodlandOfficerReviewCommentsFeed.DefaultCaseNoteFilter);
        Assert.False(result.Value.WoodlandOfficerReviewCommentsFeed.ShowFilters);

        WoodlandOfficerReviewService.Verify(x => x.GetWoodlandOfficerReviewStatusAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
    }

    private Web.Services.FellingLicenceApplication.WoodlandOfficerReview.WoodlandOfficerReviewUseCase CreateSut()
    {
        ResetMocks();

        return new Web.Services.FellingLicenceApplication.WoodlandOfficerReview.WoodlandOfficerReviewUseCase(
            InternalUserAccountService.Object,
            ExternalUserAccountRepository.Object,
            FlaRepository.Object,
            WoodlandOwnerService.Object,
            WoodlandOfficerReviewService.Object,
            UpdateWoodlandOfficerReviewService.Object,
            ActivityFeedItemProvider.Object,
            AuditingService.Object,
            NotificationService.Object,
            MockAgentAuthorityService.Object,
            GetConfiguredFcAreas.Object,
            Clock.Object,
            WoodlandOfficerReviewSubStatusService.Object,
            RequestContext,
            MockBus.Object,
            FellingLicenceApplicationOptions,
            new NullLogger<Web.Services.FellingLicenceApplication.WoodlandOfficerReview.WoodlandOfficerReviewUseCase>());
    }
}