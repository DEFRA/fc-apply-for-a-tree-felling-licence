using System.Reflection;
using CSharpFunctionalExtensions;
using FluentAssertions;
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
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Count().Should().Be(users.Count);
        foreach (var resultUser in result.Value) 
        {
            users.Any(x=>x.Id == resultUser.Id).Should().BeTrue();
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
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
        result.Value.Should().NotBeNull();

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
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Count().Should().Be(users.Count);
        result.Value.DistinctBy(x=>x.Id).Count().Should().Be(users.Count);
        foreach (var resultUser in result.Value)
        {
            users.Any(x => x.Id == resultUser.Id).Should().BeTrue();
            (users.First(x => x.Id == resultUser.Id).AccountType == resultUser.AccountType).Should().BeTrue();
            (users.First(x => x.Id == resultUser.Id).CanApproveApplications == resultUser.CanApproveApplications).Should().BeTrue();
            (users.First(x => x.Id == resultUser.Id).Status == resultUser.Status).Should().BeTrue();
        }

        MockInternalUserAccountService.Verify(x => x.ListConfirmedUserAccountsAsync(It.IsAny<CancellationToken>(), null), Times.Once());
    }

}