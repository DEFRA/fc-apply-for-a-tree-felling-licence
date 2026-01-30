using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common; // for UserDbErrorReason
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.Common.User;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using Forestry.Flo.Tests.Common; // AutoMoqData

namespace Forestry.Flo.Services.FellingLicenceApplications.Tests.Services;

public class ActivityFeedAmendmentReviewServiceTests
{
    private readonly Mock<InternalUsers.Repositories.IUserAccountRepository> _internalAccountsRepository = new();
    private readonly Mock<Applicants.Repositories.IUserAccountRepository> _externalAccountsRepository = new();
    private readonly Mock<IFellingLicenceApplicationInternalRepository> _fellingLicenceApplicationRepository = new();

    [Fact]
    public void ShouldSupportCorrectItemTypes()
    {
        var sut = CreateSut();
        var result = sut.SupportedItemTypes();
        Assert.Equal(new[] { ActivityFeedItemType.AmendmentApplicantReason, ActivityFeedItemType.AmendmentOfficerReason }, result);
    }

    [Theory, AutoMoqData]
    public async Task ShouldReturnExpectedItems(
        Guid applicationId,
        List<FellingAndRestockingAmendmentReview> amendmentReviews,
        List<InternalUsers.Entities.UserAccount.UserAccount> internalUsers,
        List<Applicants.Entities.UserAccount.UserAccount> externalApplicants)
    {
        var sut = CreateSut();
        var now = DateTime.UtcNow;
        if (amendmentReviews.Count == 0)
        {
            amendmentReviews.Add(new FellingAndRestockingAmendmentReview());
        }
        var officerId = internalUsers.First().Id;
        var applicantId = externalApplicants.First().Id;

        amendmentReviews[0].AmendingWoodlandOfficerId = officerId;
        amendmentReviews[0].AmendmentsSentDate = now;
        amendmentReviews[0].AmendmentsReason = "Reason 1";
        amendmentReviews[0].RespondingApplicantId = applicantId;
        amendmentReviews[0].ResponseReceivedDate = now.AddHours(1);
        amendmentReviews[0].ApplicantDisagreementReason = "Disagree reason";

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetFellingAndRestockingAmendmentReviewsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IEnumerable<FellingAndRestockingAmendmentReview>>(amendmentReviews));
        _internalAccountsRepository
            .Setup(x => x.GetUsersWithIdsInAsync(It.IsAny<IList<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IList<InternalUsers.Entities.UserAccount.UserAccount>, UserDbErrorReason>(internalUsers));
        _externalAccountsRepository
            .Setup(x => x.GetUsersWithIdsInAsync(It.IsAny<IList<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IList<Applicants.Entities.UserAccount.UserAccount>, UserDbErrorReason>(externalApplicants));

        var input = new ActivityFeedItemProviderModel
        {
            FellingLicenceId = applicationId,
            ItemTypes = new[] { ActivityFeedItemType.AmendmentOfficerReason, ActivityFeedItemType.AmendmentApplicantReason }
        };

        var result = await sut.RetrieveActivityFeedItemsAsync(input, ActorType.InternalUser, CancellationToken.None);

        Assert.True(result.IsSuccess);

        amendmentReviews.ForEach(review =>
        {
            Assert.Contains(result.Value, x =>
                x.ActivityFeedItemType == ActivityFeedItemType.AmendmentOfficerReason
                && x.Text == review.AmendmentsReason);

            switch (review.ApplicantAgreed)
            {
                case true:
                    Assert.Contains(result.Value, x =>
                        x.ActivityFeedItemType == ActivityFeedItemType.AmendmentApplicantReason
                        && x.Text.Contains("Applicant agreed to amendments"));
                    break;
                case false:
                    Assert.Contains(result.Value, x =>
                        x.ActivityFeedItemType == ActivityFeedItemType.AmendmentApplicantReason
                        && x.Text.Contains($"Applicant disagreed with amendments: {review.ApplicantDisagreementReason}"));
                    break;
            }
        });


        _internalAccountsRepository.Verify(x => x.GetUsersWithIdsInAsync(It.Is<IList<Guid>>(ids => ids.Contains(officerId)), It.IsAny<CancellationToken>()), Times.Once);
        _externalAccountsRepository.Verify(x => x.GetUsersWithIdsInAsync(It.Is<IList<Guid>>(ids => ids.Contains(applicantId)), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task ShouldReturnFailureWhenCannotRetrieveAmendmentReviews(
        Guid applicationId)
    {
        var sut = CreateSut();

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetFellingAndRestockingAmendmentReviewsAsync(applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<IEnumerable<FellingAndRestockingAmendmentReview>>("error"));

        var input = new ActivityFeedItemProviderModel
        {
            FellingLicenceId = applicationId,
            ItemTypes = [ ActivityFeedItemType.AmendmentOfficerReason ]
        };
        var result = await sut.RetrieveActivityFeedItemsAsync(input, ActorType.InternalUser, CancellationToken.None);

        Assert.True(result.IsFailure);

        _fellingLicenceApplicationRepository
            .Verify(x => x.GetFellingAndRestockingAmendmentReviewsAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.VerifyNoOtherCalls();

        _internalAccountsRepository.VerifyNoOtherCalls();
        _externalAccountsRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task ShouldReturnFailureWhenCannotRetrieveInternalAccounts(
        Guid applicationId,
        List<FellingAndRestockingAmendmentReview> amendmentReviews)
    {
        var sut = CreateSut();
        if (amendmentReviews.Count == 0)
        {
            amendmentReviews.Add(new FellingAndRestockingAmendmentReview());
        }
        amendmentReviews[0].AmendingWoodlandOfficerId = Guid.NewGuid();
        amendmentReviews[0].AmendmentsSentDate = DateTime.UtcNow;
        amendmentReviews[0].AmendmentsReason = "Reason 1";

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetFellingAndRestockingAmendmentReviewsAsync(applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IEnumerable<FellingAndRestockingAmendmentReview>>(amendmentReviews));
        _internalAccountsRepository
            .Setup(x => x.GetUsersWithIdsInAsync(It.IsAny<IList<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<IList<InternalUsers.Entities.UserAccount.UserAccount>, UserDbErrorReason>(UserDbErrorReason.General));

        var input = new ActivityFeedItemProviderModel
        {
            FellingLicenceId = applicationId,
            ItemTypes = new[] { ActivityFeedItemType.AmendmentOfficerReason }
        };
        var result = await sut.RetrieveActivityFeedItemsAsync(input, ActorType.InternalUser, CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Theory, AutoMoqData]
    public async Task ShouldReturnFailureWhenCannotRetrieveExternalAccounts(
        Guid applicationId,
        List<FellingAndRestockingAmendmentReview> amendmentReviews,
        List<InternalUsers.Entities.UserAccount.UserAccount> internalUsers)
    {
        var sut = CreateSut();
        if (amendmentReviews.Count == 0)
        {
            amendmentReviews.Add(new FellingAndRestockingAmendmentReview());
        }
        amendmentReviews[0].AmendingWoodlandOfficerId = internalUsers.First().Id;
        amendmentReviews[0].AmendmentsSentDate = DateTime.UtcNow;
        amendmentReviews[0].AmendmentsReason = "Reason 1";
        amendmentReviews[0].RespondingApplicantId = Guid.NewGuid();
        amendmentReviews[0].ResponseReceivedDate = DateTime.UtcNow.AddHours(1);
        amendmentReviews[0].ApplicantDisagreementReason = "Disagree reason";

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetFellingAndRestockingAmendmentReviewsAsync(applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IEnumerable<FellingAndRestockingAmendmentReview>>(amendmentReviews));
        _internalAccountsRepository
            .Setup(x => x.GetUsersWithIdsInAsync(It.IsAny<IList<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IList<InternalUsers.Entities.UserAccount.UserAccount>, UserDbErrorReason>(internalUsers));
        _externalAccountsRepository
            .Setup(x => x.GetUsersWithIdsInAsync(It.IsAny<IList<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<IList<Applicants.Entities.UserAccount.UserAccount>, UserDbErrorReason>(UserDbErrorReason.General));

        var input = new ActivityFeedItemProviderModel
        {
            FellingLicenceId = applicationId,
            ItemTypes = new[] { ActivityFeedItemType.AmendmentOfficerReason, ActivityFeedItemType.AmendmentApplicantReason }
        };
        var result = await sut.RetrieveActivityFeedItemsAsync(input, ActorType.InternalUser, CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    private ActivityFeedAmendmentReviewService CreateSut()
    {
        _internalAccountsRepository.Reset();
        _externalAccountsRepository.Reset();
        _fellingLicenceApplicationRepository.Reset();

        return new ActivityFeedAmendmentReviewService(
            _fellingLicenceApplicationRepository.Object,
            _internalAccountsRepository.Object,
            _externalAccountsRepository.Object,
            new NullLogger<ActivityFeedAmendmentReviewService>());
    }
}
