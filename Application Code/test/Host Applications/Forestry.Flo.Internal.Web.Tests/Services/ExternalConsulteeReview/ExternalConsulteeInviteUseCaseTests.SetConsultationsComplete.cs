using AutoFixture.Xunit2;
using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Services;
using Forestry.Flo.Tests.Common;
using Moq;

namespace Forestry.Flo.Internal.Web.Tests.Services.ExternalConsulteeReview;

public partial class ExternalConsulteeInviteUseCaseTests
{
    [Theory, AutoData]
    public async Task WhenCompleteUpdateFails(Guid applicationId, Guid userId, string error)
    {
        var userPrincipal = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(localAccountId: userId);
        var user = new InternalUser(userPrincipal);

        var sut = CreateSut();

        _mockUpdateWoodlandOfficerReviewService
            .Setup(x => x.UpdateConsultationsStatusAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<bool?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(error));

        var result = await sut.SetConsultationsCompleteAsync(applicationId, user, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(error, result.Error);

        _mockUpdateWoodlandOfficerReviewService
            .Verify(x => x.UpdateConsultationsStatusAsync(applicationId, userId, true, true, It.IsAny<CancellationToken>()),
                Times.Once);
        _mockUpdateWoodlandOfficerReviewService.VerifyNoOtherCalls();

    }

    [Theory, AutoData]
    public async Task WhenCompleteUpdateSucceeds(Guid applicationId, Guid userId)
    {
        var userPrincipal = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(localAccountId: userId);
        var user = new InternalUser(userPrincipal);

        var sut = CreateSut();

        _mockUpdateWoodlandOfficerReviewService
            .Setup(x => x.UpdateConsultationsStatusAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<bool?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var result = await sut.SetConsultationsCompleteAsync(applicationId, user, CancellationToken.None);

        Assert.True(result.IsSuccess);

        _mockUpdateWoodlandOfficerReviewService
            .Verify(x => x.UpdateConsultationsStatusAsync(applicationId, userId, true, true, It.IsAny<CancellationToken>()),
                Times.Once);
        _mockUpdateWoodlandOfficerReviewService.VerifyNoOtherCalls();

    }
}