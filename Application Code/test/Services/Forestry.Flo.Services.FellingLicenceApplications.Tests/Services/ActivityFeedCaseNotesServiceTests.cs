using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.InternalUsers.Entities.UserAccount;
using Forestry.Flo.Services.InternalUsers.Repositories;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Forestry.Flo.Services.FellingLicenceApplications.Tests.Services;

public class ActivityFeedCaseNotesServiceTests
{
    private readonly Mock<IViewCaseNotesService> _viewCaseNotes = new();
    private readonly Mock<IUserAccountRepository> _userAccountRepository = new();


    [Theory, AutoMoqData]

    public async Task ShouldRetrieveAllActivityFeedItems_WhenAllDetailsPresent(
        List<CaseNoteModel> caseNoteModels, 
        UserAccount userAccount)
    {
        var _sut = CreateSut();

        caseNoteModels.ForEach(x => x.CreatedByUserId = userAccount.Id);

        userAccount.AccountType = AccountTypeInternal.AdminOfficer;

        ActivityFeedItemType[] activityFeedItemTypes =
        {
                ActivityFeedItemType.CaseNote,
                ActivityFeedItemType.AdminOfficerReviewComment,
                ActivityFeedItemType.WoodlandOfficerReviewComment,
                ActivityFeedItemType.SiteVisitComment,
                ActivityFeedItemType.ReturnToApplicantComment
        };
        var expectedCaseNoteTypes = new CaseNoteType[]
        {
            CaseNoteType.CaseNote,
            CaseNoteType.AdminOfficerReviewComment,
            CaseNoteType.WoodlandOfficerReviewComment,
            CaseNoteType.SiteVisitComment,
            CaseNoteType.ReturnToApplicantComment
        };

        _viewCaseNotes.Setup(r => r.GetSpecificCaseNotesAsync(It.IsAny<Guid>(), It.IsAny<CaseNoteType[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(caseNoteModels);

        _userAccountRepository
            .Setup(r => r.GetUsersWithIdsInAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IList<UserAccount>, UserDbErrorReason>(new List<UserAccount>() { userAccount }));

        var providerModel = new ActivityFeedItemProviderModel()
        {
            FellingLicenceId = Guid.NewGuid(),
            ItemTypes = activityFeedItemTypes
        };

        var result = await _sut.RetrieveActivityFeedItemsAsync(
            providerModel,
            ActorType.InternalUser,
            CancellationToken.None);

        // verify
        _viewCaseNotes.Verify(x => x.GetSpecificCaseNotesAsync(providerModel.FellingLicenceId, expectedCaseNoteTypes, It.IsAny<CancellationToken>()), Times.Once);
        _userAccountRepository.Verify(x => x.GetUsersWithIdsInAsync(new List<Guid> { userAccount.Id }, It.IsAny<CancellationToken>()), Times.Once);

        // assert

        Assert.True(result.IsSuccess);
        Assert.Equal(caseNoteModels.Count, result.Value.Count);
        foreach (var expected in caseNoteModels)
        {
            Assert.Contains(result.Value, x =>
                x.ActivityFeedItemType == (ActivityFeedItemType)expected.Type
                && x.AssociatedId == expected.Id
                && x.VisibleToApplicant == expected.VisibleToApplicant
                && x.VisibleToConsultee == expected.VisibleToConsultee
                && x.FellingLicenceApplicationId == expected.FellingLicenceApplicationId
                && x.CreatedTimestamp == expected.CreatedTimestamp
                && x.Text == expected.Text
                && x.CreatedByUser.Id == userAccount.Id
                && x.CreatedByUser.AccountType == userAccount.AccountType
                && x.CreatedByUser.FirstName == userAccount.FirstName
                && x.CreatedByUser.LastName == userAccount.LastName);
        }
    }

    [Theory, AutoMoqData]

    public async Task ShouldReturnFailure_WhenUserQueryFails(
        List<CaseNoteModel> caseNoteModels,
        UserAccount userAccount)
    {
        var _sut = CreateSut();

        caseNoteModels.ForEach(x => x.CreatedByUserId = userAccount.Id);

        userAccount.AccountType = AccountTypeInternal.AdminOfficer;

        ActivityFeedItemType[] activityFeedItemTypes =
        {
                ActivityFeedItemType.CaseNote,
                ActivityFeedItemType.AdminOfficerReviewComment,
                ActivityFeedItemType.WoodlandOfficerReviewComment,
                ActivityFeedItemType.SiteVisitComment,
                ActivityFeedItemType.ReturnToApplicantComment,
                ActivityFeedItemType.CBWCheckComment,
                ActivityFeedItemType.LarchCheckComment,
                ActivityFeedItemType.ApproverReviewComment
        };
        var expectedCaseNoteTypes = new CaseNoteType[]
        {
            CaseNoteType.CaseNote,
            CaseNoteType.AdminOfficerReviewComment,
            CaseNoteType.WoodlandOfficerReviewComment,
            CaseNoteType.SiteVisitComment,
            CaseNoteType.ReturnToApplicantComment,
            CaseNoteType.CBWCheckComment,
            CaseNoteType.LarchCheckComment,
            CaseNoteType.ApproverReviewComment
        };

        _viewCaseNotes.Setup(r => r.GetSpecificCaseNotesAsync(It.IsAny<Guid>(), It.IsAny<CaseNoteType[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(caseNoteModels);

        _userAccountRepository
            .Setup(r => r.GetUsersWithIdsInAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<IList<UserAccount>, UserDbErrorReason>(UserDbErrorReason.General));

        var providerModel = new ActivityFeedItemProviderModel()
        {
            FellingLicenceId = Guid.NewGuid(),
            ItemTypes = activityFeedItemTypes
        };

        var result = await _sut.RetrieveActivityFeedItemsAsync(
            providerModel,
            ActorType.InternalUser,
            CancellationToken.None);

        // verify
        _viewCaseNotes.Verify(x => x.GetSpecificCaseNotesAsync(providerModel.FellingLicenceId, expectedCaseNoteTypes, It.IsAny<CancellationToken>()), Times.Once);
        _userAccountRepository.Verify(x => x.GetUsersWithIdsInAsync(new List<Guid> { userAccount.Id }, It.IsAny<CancellationToken>()), Times.Once);

        // assert

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void ShouldSupportCorrectItemTypes()
    {
        var expectedItemTypes = new[]
        {
            ActivityFeedItemType.CaseNote,
            ActivityFeedItemType.AdminOfficerReviewComment,
            ActivityFeedItemType.WoodlandOfficerReviewComment,
            ActivityFeedItemType.SiteVisitComment,
            ActivityFeedItemType.ReturnToApplicantComment,
            ActivityFeedItemType.LarchCheckComment,
            ActivityFeedItemType.CBWCheckComment,
            ActivityFeedItemType.ApproverReviewComment
        };

        var sut = CreateSut();
        var result = sut.SupportedItemTypes();

        Assert.Equal(expectedItemTypes, result);
    }

    private ActivityFeedCaseNotesService CreateSut()
    {
        _viewCaseNotes.Reset();
        _userAccountRepository.Reset();

        return new ActivityFeedCaseNotesService(
            _viewCaseNotes.Object,
            _userAccountRepository.Object,
            new NullLogger<ActivityFeedCaseNotesService>());
    }
}