using System;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.InternalUsers.Entities.UserAccount;
using Forestry.Flo.Services.InternalUsers.Repositories;
using Forestry.Flo.Tests.Common;
using Moq;
using Xunit;

namespace Forestry.Flo.Services.FellingLicenceApplications.Tests.Services;

public class ActivityFeedApproverCommentServiceTests
{
    private readonly Mock<IUserAccountRepository> _userAccountRepository = new();
    private readonly Mock<IFellingLicenceApplicationInternalRepository> _fellingLicenceApplicationRepository = new();

    [Theory, AutoMoqData]
    public async Task RetrieveActivityFeedItemsAsync_ReturnsEmptyList_WhenApproverReviewDoesNotExistYet(
        ActivityFeedItemProviderModel providerModel)
    {
        // Arrange
        var sut = CreateSut();

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetApproverReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe.None);

        // Act
        var result = await sut.RetrieveActivityFeedItemsAsync(providerModel, ActorType.InternalUser, CancellationToken.None);
        
        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);

        _userAccountRepository.VerifyNoOtherCalls();
        
        _fellingLicenceApplicationRepository
            .Verify(x => x.GetApproverReviewAsync(providerModel.FellingLicenceId, It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task RetrieveActivityFeedItemsAsync_ReturnsEmptyList_WhenApproverReviewHasNoComments(
        ActivityFeedItemProviderModel providerModel,
        ApproverReview approverReview)
    {
        approverReview.ApplicationRefusedReason = null;
        approverReview.DurationChangeReason = null;
        approverReview.ReferToLocalAuthorityReason = null;

        // Arrange
        var sut = CreateSut();

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetApproverReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe.From(approverReview));

        // Act
        var result = await sut.RetrieveActivityFeedItemsAsync(providerModel, ActorType.InternalUser, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);

        _userAccountRepository.VerifyNoOtherCalls();

        _fellingLicenceApplicationRepository
            .Verify(x => x.GetApproverReviewAsync(providerModel.FellingLicenceId, It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task RetrieveActivityFeedItemsAsync_ReturnsEmptyList_WhenApproverReviewHasCommentsButCannotLoadUser(
        ActivityFeedItemProviderModel providerModel,
        ApproverReview approverReview)
    {
        // leave duration change reason with a value to ensure comments are present, but set the other comment
        // fields to null to ensure only one comment is returned
        approverReview.ApplicationRefusedReason = null;
        approverReview.ReferToLocalAuthorityReason = null;

        var expectedText  = $"Approved licence duration changed from woodland officer recommendation:\n{approverReview.DurationChangeReason}";

        // Arrange
        var sut = CreateSut();

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetApproverReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe.From(approverReview));

        _userAccountRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<UserAccount, UserDbErrorReason>(UserDbErrorReason.NotFound));

        // Act
        var result = await sut.RetrieveActivityFeedItemsAsync(providerModel, ActorType.InternalUser, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);

        var item = result.Value[0];

        Assert.Equal(providerModel.FellingLicenceId, item.FellingLicenceApplicationId);
        Assert.Equal(providerModel.FellingLicenceId, item.AssociatedId);
        Assert.Equal(ActivityFeedItemType.ApproverReviewComment, item.ActivityFeedItemType);
        Assert.Equal(expectedText, item.Text);
        Assert.False(item.VisibleToApplicant);
        Assert.False(item.VisibleToConsultee);
        Assert.Equal(approverReview.LastUpdatedDate, item.CreatedTimestamp);
        Assert.Null(item.CreatedByUser);
        Assert.Null(item.Source);
        Assert.Null(item.Attachments);
        Assert.Null(item.Recipients);

        _userAccountRepository
            .Verify(x => x.GetAsync(approverReview.LastUpdatedById, It.IsAny<CancellationToken>()), Times.Once);
        _userAccountRepository.VerifyNoOtherCalls();

        _fellingLicenceApplicationRepository
            .Verify(x => x.GetApproverReviewAsync(providerModel.FellingLicenceId, It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task RetrieveActivityFeedItemsAsync_ReturnsEmptyList_WhenApproverReviewHasDurationChangeComment(
        ActivityFeedItemProviderModel providerModel,
        ApproverReview approverReview,
        UserAccount approverAccount)
    {
        // leave duration change reason with a value to ensure comments are present, but set the other comment
        // fields to null to ensure only one comment is returned
        approverReview.ApplicationRefusedReason = null;
        approverReview.ReferToLocalAuthorityReason = null;

        var expectedText = $"Approved licence duration changed from woodland officer recommendation:\n{approverReview.DurationChangeReason}";

        // Arrange
        var sut = CreateSut();

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetApproverReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe.From(approverReview));

        _userAccountRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<UserAccount, UserDbErrorReason>(approverAccount));

        // Act
        var result = await sut.RetrieveActivityFeedItemsAsync(providerModel, ActorType.InternalUser, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);

        var item = result.Value[0];

        Assert.Equal(providerModel.FellingLicenceId, item.FellingLicenceApplicationId);
        Assert.Equal(providerModel.FellingLicenceId, item.AssociatedId);
        Assert.Equal(ActivityFeedItemType.ApproverReviewComment, item.ActivityFeedItemType);
        Assert.Equal(expectedText, item.Text);
        Assert.False(item.VisibleToApplicant);
        Assert.False(item.VisibleToConsultee);
        Assert.Equal(approverReview.LastUpdatedDate, item.CreatedTimestamp);
        Assert.Equal(approverAccount.AccountType, item.CreatedByUser.AccountType);
        Assert.Equal(approverAccount.FirstName, item.CreatedByUser.FirstName);
        Assert.Equal(approverAccount.LastName, item.CreatedByUser.LastName);
        Assert.Equal(approverAccount.Id, item.CreatedByUser.Id);
        Assert.Equal(approverAccount.Status == Status.Confirmed, item.CreatedByUser.IsActiveUser);
        Assert.Null(item.Source);
        Assert.Null(item.Attachments);
        Assert.Null(item.Recipients);

        _userAccountRepository
            .Verify(x => x.GetAsync(approverReview.LastUpdatedById, It.IsAny<CancellationToken>()), Times.Once);
        _userAccountRepository.VerifyNoOtherCalls();

        _fellingLicenceApplicationRepository
            .Verify(x => x.GetApproverReviewAsync(providerModel.FellingLicenceId, It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task RetrieveActivityFeedItemsAsync_ReturnsEmptyList_WhenApproverReviewHasRefusalComment(
        ActivityFeedItemProviderModel providerModel,
        ApproverReview approverReview,
        UserAccount approverAccount)
    {
        // leave refusal reason with a value to ensure comments are present, but set the other comment
        // fields to null to ensure only one comment is returned
        approverReview.DurationChangeReason= null;
        approverReview.ReferToLocalAuthorityReason = null;

        var expectedText = $"Reason for application refusal:\n{approverReview.ApplicationRefusedReason}";

        // Arrange
        var sut = CreateSut();

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetApproverReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe.From(approverReview));

        _userAccountRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<UserAccount, UserDbErrorReason>(approverAccount));

        // Act
        var result = await sut.RetrieveActivityFeedItemsAsync(providerModel, ActorType.InternalUser, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);

        var item = result.Value[0];

        Assert.Equal(providerModel.FellingLicenceId, item.FellingLicenceApplicationId);
        Assert.Equal(providerModel.FellingLicenceId, item.AssociatedId);
        Assert.Equal(ActivityFeedItemType.ApproverReviewComment, item.ActivityFeedItemType);
        Assert.Equal(expectedText, item.Text);
        Assert.True(item.VisibleToApplicant);
        Assert.False(item.VisibleToConsultee);
        Assert.Equal(approverReview.LastUpdatedDate, item.CreatedTimestamp);
        Assert.Equal(approverAccount.AccountType, item.CreatedByUser.AccountType);
        Assert.Equal(approverAccount.FirstName, item.CreatedByUser.FirstName);
        Assert.Equal(approverAccount.LastName, item.CreatedByUser.LastName);
        Assert.Equal(approverAccount.Id, item.CreatedByUser.Id);
        Assert.Equal(approverAccount.Status == Status.Confirmed, item.CreatedByUser.IsActiveUser);
        Assert.Null(item.Source);
        Assert.Null(item.Attachments);
        Assert.Null(item.Recipients);

        _userAccountRepository
            .Verify(x => x.GetAsync(approverReview.LastUpdatedById, It.IsAny<CancellationToken>()), Times.Once);
        _userAccountRepository.VerifyNoOtherCalls();

        _fellingLicenceApplicationRepository
            .Verify(x => x.GetApproverReviewAsync(providerModel.FellingLicenceId, It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task RetrieveActivityFeedItemsAsync_ReturnsEmptyList_WhenApproverReviewHasReferredToLAComment(
        ActivityFeedItemProviderModel providerModel,
        ApproverReview approverReview,
        UserAccount approverAccount)
    {
        // leave referral reason with a value to ensure comments are present, but set the other comment
        // fields to null to ensure only one comment is returned
        approverReview.DurationChangeReason = null;
        approverReview.ApplicationRefusedReason = null;

        var expectedText = $"Reason for referral to local authority:\n{approverReview.ReferToLocalAuthorityReason}";

        // Arrange
        var sut = CreateSut();

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetApproverReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe.From(approverReview));

        _userAccountRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<UserAccount, UserDbErrorReason>(approverAccount));

        // Act
        var result = await sut.RetrieveActivityFeedItemsAsync(providerModel, ActorType.InternalUser, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);

        var item = result.Value[0];

        Assert.Equal(providerModel.FellingLicenceId, item.FellingLicenceApplicationId);
        Assert.Equal(providerModel.FellingLicenceId, item.AssociatedId);
        Assert.Equal(ActivityFeedItemType.ApproverReviewComment, item.ActivityFeedItemType);
        Assert.Equal(expectedText, item.Text);
        Assert.True(item.VisibleToApplicant);
        Assert.False(item.VisibleToConsultee);
        Assert.Equal(approverReview.LastUpdatedDate, item.CreatedTimestamp);
        Assert.Equal(approverAccount.AccountType, item.CreatedByUser.AccountType);
        Assert.Equal(approverAccount.FirstName, item.CreatedByUser.FirstName);
        Assert.Equal(approverAccount.LastName, item.CreatedByUser.LastName);
        Assert.Equal(approverAccount.Id, item.CreatedByUser.Id);
        Assert.Equal(approverAccount.Status == Status.Confirmed, item.CreatedByUser.IsActiveUser);
        Assert.Null(item.Source);
        Assert.Null(item.Attachments);
        Assert.Null(item.Recipients);

        _userAccountRepository
            .Verify(x => x.GetAsync(approverReview.LastUpdatedById, It.IsAny<CancellationToken>()), Times.Once);
        _userAccountRepository.VerifyNoOtherCalls();

        _fellingLicenceApplicationRepository
            .Verify(x => x.GetApproverReviewAsync(providerModel.FellingLicenceId, It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.VerifyNoOtherCalls();
    }


    private ActivityFeedApproverCommentService CreateSut()
    {
        _userAccountRepository.Reset();
        _fellingLicenceApplicationRepository.Reset();

        return new ActivityFeedApproverCommentService(
            _userAccountRepository.Object,
            _fellingLicenceApplicationRepository.Object,
            null!);
    }
}