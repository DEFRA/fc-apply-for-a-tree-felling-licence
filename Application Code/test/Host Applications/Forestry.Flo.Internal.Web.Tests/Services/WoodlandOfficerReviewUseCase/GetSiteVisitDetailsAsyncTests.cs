using AutoFixture.Xunit2;
using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Services.FellingLicenceApplication.WoodlandOfficerReview;
using Forestry.Flo.Services.Applicants.Entities.UserAccount;
using Forestry.Flo.Services.Applicants.Models;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Forestry.Flo.Internal.Web.Tests.Services.WoodlandOfficerReviewUseCase;

public class GetSiteVisitDetailsAsyncTests : WoodlandOfficerReviewUseCaseTestsBase<SiteVisitUseCase>
{
    [Theory, AutoData]
    public async Task ShouldReturnFailureWhenRetrievalOfSiteVisitDetailsFails(
        Guid applicationId,
        string hostingPage)
    {
        var sut = CreateSut();
        WoodlandOfficerReviewService
            .Setup(x => x.GetSiteVisitDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<Maybe<SiteVisitModel>>("error"));

        var result = await sut.GetSiteVisitDetailsAsync(applicationId, hostingPage, CancellationToken.None);

        Assert.True(result.IsFailure);

        WoodlandOfficerReviewService.Verify(x => x.GetSiteVisitDetailsAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        WoodlandOfficerReviewService.VerifyNoOtherCalls();

        FlaRepository.VerifyNoOtherCalls();
        InternalUserAccountService.VerifyNoOtherCalls();
        ExternalUserAccountRepository.VerifyNoOtherCalls();
        WoodlandOwnerService.VerifyNoOtherCalls();
        NotificationHistoryService.VerifyNoOtherCalls();
        ActivityFeedItemProvider.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task ShouldReturnFailureWhenUnableToLoadApplicationSummary(
        Guid applicationId, 
        string hostingPage,
        SiteVisitModel siteVisit)
    {
        var sut = CreateSut();

        WoodlandOfficerReviewService
            .Setup(x => x.GetSiteVisitDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(Maybe.From(siteVisit)));

        FlaRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.None);

        var result = await sut.GetSiteVisitDetailsAsync(applicationId, hostingPage, CancellationToken.None);

        Assert.True(result.IsFailure);

        WoodlandOfficerReviewService.Verify(x => x.GetSiteVisitDetailsAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FlaRepository.Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        WoodlandOwnerService.VerifyNoOtherCalls();
        ExternalUserAccountRepository.VerifyNoOtherCalls();
        InternalUserAccountService.VerifyNoOtherCalls();
        NotificationHistoryService.VerifyNoOtherCalls();
        ActivityFeedItemProvider.VerifyNoOtherCalls();
    }

    //It is assumed that the various other permutations of outcome for FellingLicenceApplicationUseCaseBase.GetFellingLicenceDetailsAsync are already tested - see AssignToUserUseCaseTestsBase

    [Theory, AutoMoqData]
    public async Task ShouldReturnFailureWhenUnableToLoadSiteVisitComments(
        Guid applicationId,
        string hostingPage,
        SiteVisitModel siteVisit,
        FellingLicenceApplication fla,
        WoodlandOwnerModel woodlandOwner,
        UserAccount externalApplicant,
        Flo.Services.InternalUsers.Entities.UserAccount.UserAccount internalUser)
    {
        var sut = CreateSut();

        fla.LinkedPropertyProfile.ProposedFellingDetails = new List<ProposedFellingDetail>();

        WoodlandOfficerReviewService
            .Setup(x => x.GetSiteVisitDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(Maybe.From(siteVisit)));

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

        ActivityFeedItemProvider
            .Setup(x => x.RetrieveAllRelevantActivityFeedItemsAsync(It.IsAny<ActivityFeedItemProviderModel>(), It.IsAny<ActorType>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<IList<ActivityFeedItemModel>>("error"));

        var result = await sut.GetSiteVisitDetailsAsync(applicationId, hostingPage, CancellationToken.None);

        Assert.True(result.IsFailure);

        WoodlandOfficerReviewService.Verify(x => x.GetSiteVisitDetailsAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FlaRepository.Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);

        ActivityFeedItemProvider.Verify(x => x.RetrieveAllRelevantActivityFeedItemsAsync(
            It.Is<ActivityFeedItemProviderModel>(m => m.FellingLicenceId == applicationId && m.FellingLicenceReference == fla.ApplicationReference && m.ItemTypes.Single() == ActivityFeedItemType.SiteVisitComment),
            ActorType.InternalUser,
            It.IsAny<CancellationToken>()), Times.Once);
        ActivityFeedItemProvider.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task ShouldReturnSuccessWhenDataIsSuccessfullyRetrieved(
        Guid applicationId,
        string hostingPage,
        SiteVisitModel siteVisit,
        FellingLicenceApplication fla,
        WoodlandOwnerModel woodlandOwner,
        UserAccount externalApplicant,
        Flo.Services.InternalUsers.Entities.UserAccount.UserAccount internalUser,
        List<ActivityFeedItemModel> siteVisitComments)
    {
        var sut = CreateSut();

        fla.LinkedPropertyProfile.ProposedFellingDetails = new List<ProposedFellingDetail>();

        WoodlandOfficerReviewService
            .Setup(x => x.GetSiteVisitDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(Maybe.From(siteVisit)));

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

        ActivityFeedItemProvider
            .Setup(x => x.RetrieveAllRelevantActivityFeedItemsAsync(It.IsAny<ActivityFeedItemProviderModel>(), It.IsAny<ActorType>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IList<ActivityFeedItemModel>>(siteVisitComments));

        var result = await sut.GetSiteVisitDetailsAsync(applicationId, hostingPage, CancellationToken.None);

        Assert.True(result.IsSuccess);

        Assert.NotNull(result.Value.FellingLicenceApplicationSummary);
        Assert.Equal(siteVisit.SiteVisitNeeded, result.Value.SiteVisitNeeded);
        Assert.Equal(siteVisit.SiteVisitArrangementsMade, result.Value.SiteVisitArrangementsMade);
        Assert.Equal(siteVisit.SiteVisitComplete, result.Value.SiteVisitComplete);
        Assert.Equal(CaseNoteType.SiteVisitComment, result.Value.SiteVisitComments.DefaultCaseNoteFilter);
        Assert.False(result.Value.SiteVisitComments.ShowAddCaseNote);
        Assert.Equal(hostingPage, result.Value.SiteVisitComments.HostingPage);
        Assert.Equivalent(siteVisitComments, result.Value.SiteVisitComments.ActivityFeedItemModels);

        WoodlandOfficerReviewService.Verify(x => x.GetSiteVisitDetailsAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);

        ActivityFeedItemProvider.Verify(x => x.RetrieveAllRelevantActivityFeedItemsAsync(
            It.Is<ActivityFeedItemProviderModel>(m => m.FellingLicenceId == applicationId && m.FellingLicenceReference == fla.ApplicationReference && m.ItemTypes.Single() == ActivityFeedItemType.SiteVisitComment),
            ActorType.InternalUser,
            It.IsAny<CancellationToken>()), Times.Once);
        ActivityFeedItemProvider.VerifyNoOtherCalls();
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
            MockAddDocumentService.Object,
            MockRemoveDocumentService.Object,
            _foresterServices.Object,
            RequestContext,
            WoodlandOfficerReviewSubStatusService.Object,
            new NullLogger<SiteVisitUseCase>());
    }
}