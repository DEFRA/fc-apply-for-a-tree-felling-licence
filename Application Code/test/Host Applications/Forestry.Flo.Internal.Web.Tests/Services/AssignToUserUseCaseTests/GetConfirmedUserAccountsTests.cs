using System.Reflection;
using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Models.UserAccount;
using Forestry.Flo.Services.InternalUsers.Entities.UserAccount;
using Forestry.Flo.Tests.Common;
using Moq;

namespace Forestry.Flo.Internal.Web.Tests.Services.AssignToUserUseCaseTests;

public class GetConfirmedUserAccountsTests : AssignToUserUseCaseTestsBase
{
    [Theory, AutoMoqData]
    public async Task ShouldReturnConfirmedUsers(
        List<UserAccount> users)
    {
        // Arrange
        var sut = CreateSut();

        foreach (var user in users)
        {
            TestUtils.SetProtectedProperty(user, "Id", Guid.NewGuid());
        }

        MockInternalUserAccountService.Setup(x => x.ListConfirmedUserAccountsAsync(It.IsAny<CancellationToken>(), null))
            .ReturnsAsync(users);
        var methodInfo = sut.GetType().GetMethod("GetConfirmedUserAccountsAsync", BindingFlags.NonPublic | BindingFlags.Instance);

        // Act
        var result = await (Task<Result<IEnumerable<UserAccountModel>>>)methodInfo!.Invoke(sut, new object[] { CancellationToken.None })!;

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(users.Count, result.Value.Count());
        foreach (var resultUser in result.Value)
        {
            Assert.Contains(resultUser.Id, users.Select(x => x.Id));
        }

        MockInternalUserAccountService.Verify(x => x.ListConfirmedUserAccountsAsync(It.IsAny<CancellationToken>(), null), Times.Once());
    }

    [Theory, AutoMoqData]
    public async Task ShouldReturnConfirmedUsers_WhenConfirmedUsersIsEmpty(
        List<UserAccount> users)
    {
        // Arrange
        var sut = CreateSut();

        users = null;

        MockInternalUserAccountService.Setup(x => x.ListConfirmedUserAccountsAsync(It.IsAny<CancellationToken>(), null))!
            .ReturnsAsync(users);
        var methodInfo = sut.GetType().GetMethod("GetConfirmedUserAccountsAsync", BindingFlags.NonPublic | BindingFlags.Instance);

        // Act
        var result = await (Task<Result<IEnumerable<UserAccountModel>>>)methodInfo!.Invoke(sut, new object[] { CancellationToken.None })!;

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Empty(result.Value);

        MockInternalUserAccountService.Verify(x => x.ListConfirmedUserAccountsAsync(It.IsAny<CancellationToken>(), null), Times.Once());
    }

    [Theory, AutoMoqData]
    public async Task ShouldReturnConfirmedUsers_WithMapppingCorrect(
        List<UserAccount> users)
    {
        // Arrange
        var sut = CreateSut();

        foreach (var user in users)
        {
            TestUtils.SetProtectedProperty(user, "Id", Guid.NewGuid());
        }

        MockInternalUserAccountService.Setup(x => x.ListConfirmedUserAccountsAsync(It.IsAny<CancellationToken>(), null))!
            .ReturnsAsync(users);
        var methodInfo = sut.GetType().GetMethod("GetConfirmedUserAccountsAsync", BindingFlags.NonPublic | BindingFlags.Instance);

        // Act
        var result = await (Task<Result<IEnumerable<UserAccountModel>>>)methodInfo!.Invoke(sut, new object[] { CancellationToken.None })!;

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(users.Count, result.Value.Count());
        Assert.Equal(users.Count, result.Value.DistinctBy(z => z.Id).Count());
        foreach (var resultUser in result.Value)
        {
            var user = users.FirstOrDefault(x => x.Id == resultUser.Id);
            Assert.NotNull(user);
            Assert.Equal(user.AccountType, resultUser.AccountType);
            Assert.Equal(user.CanApproveApplications, resultUser.CanApproveApplications);
            Assert.Equal(user.Status, resultUser.Status);
        }

        MockInternalUserAccountService.Verify(x => x.ListConfirmedUserAccountsAsync(It.IsAny<CancellationToken>(), null), Times.Once());
    }

}