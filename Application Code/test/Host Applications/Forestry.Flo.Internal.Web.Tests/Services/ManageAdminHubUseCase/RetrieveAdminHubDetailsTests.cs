using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Services;
using Forestry.Flo.Services.AdminHubs.Model;
using Forestry.Flo.Services.AdminHubs.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.InternalUsers.Entities.UserAccount;
using Forestry.Flo.Services.InternalUsers.Services;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Forestry.Flo.Internal.Web.Tests.Services.ManageAdminHubUseCase;

public class RetrieveAdminHubDetailsTests
{
    protected readonly string RequestContextCorrelationId = Guid.NewGuid().ToString();
    protected readonly Guid RequestContextUserId = Guid.NewGuid();
    private readonly Mock<IAuditService<Web.Services.AdminHub.ManageAdminHubUseCase>> MockAuditService = new();
    private readonly Mock<IAdminHubService> MockAdminHubService = new();
    private readonly Mock<IUserAccountService> MockUserAccountService = new();

    [Theory, AutoMoqData]
    public async Task WhenCurrentUserIsManagerOfAParticularHub(
        IReadOnlyCollection<AdminHubModel> adminHubs,
        IEnumerable<UserAccount> users)
    {
        var principal = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(
            localAccountId: RequestContextUserId,
            accountTypeInternal: AccountTypeInternal.AdminHubManager);
        var user = new InternalUser(principal);

        foreach (var userAccount in users)
        {
            userAccount.AccountType = AccountTypeInternal.AdminOfficer;
        }

        adminHubs.First().AdminManagerUserAccountId = user.UserAccountId.Value;

        var sut = CreateSut();

        MockAdminHubService
            .Setup(x => x.RetrieveAdminHubDataAsync(It.IsAny<GetAdminHubsDataRequestModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IReadOnlyCollection<AdminHubModel>, ManageAdminHubOutcome>(adminHubs));

        MockUserAccountService
            .Setup(x => x.ListConfirmedUserAccountsAsync(It.IsAny<CancellationToken>(), null))
            .ReturnsAsync(users);

        var result = await sut.RetrieveAdminHubDetailsAsync(user, CancellationToken.None);

        Assert.True(result.IsSuccess);

        MockAdminHubService
            .Verify(x => x.RetrieveAdminHubDataAsync(
                It.Is<GetAdminHubsDataRequestModel>(x => x.PerformingUserAccountType == user.AccountType && x.PerformingUserId == user.UserAccountId.Value), 
                It.IsAny<CancellationToken>()), Times.Once);
        MockAdminHubService.VerifyNoOtherCalls();
        MockUserAccountService.Verify(x => x.ListConfirmedUserAccountsAsync(It.IsAny<CancellationToken>(), null), Times.Once);
        MockUserAccountService.VerifyNoOtherCalls();
        MockAuditService.VerifyNoOtherCalls();

        Assert.Equal(adminHubs.First().Id, result.Value.Id);
        Assert.Equal(adminHubs.First().Name, result.Value.Name);
        Assert.Equal(adminHubs, result.Value.AdminHubs);
        Assert.Equal(users.Count(), result.Value.AllAdminOfficers.Count);
        foreach (var userAccount in users)
        {
            Assert.Contains(result.Value.AllAdminOfficers, x =>
                x.Id == userAccount.Id
                && x.AccountType == userAccount.AccountType
                && x.Email == userAccount.Email
                && x.FirstName == userAccount.FirstName
                && x.LastName == userAccount.LastName
                && x.IsActiveAccount == userAccount.Status is Status.Confirmed);
        }
    }

    [Theory, AutoMoqData]
    public async Task WhenCurrentUserIsNotManagerOfAParticularHub(
        IReadOnlyCollection<AdminHubModel> adminHubs,
        IEnumerable<UserAccount> users)
    {
        var principal = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(
            localAccountId: RequestContextUserId,
            accountTypeInternal: AccountTypeInternal.AdminHubManager);
        var user = new InternalUser(principal);

        foreach (var userAccount in users)
        {
            userAccount.AccountType = AccountTypeInternal.AdminOfficer;
        }

        var sut = CreateSut();

        MockAdminHubService
            .Setup(x => x.RetrieveAdminHubDataAsync(It.IsAny<GetAdminHubsDataRequestModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IReadOnlyCollection<AdminHubModel>, ManageAdminHubOutcome>(adminHubs));

        MockUserAccountService
            .Setup(x => x.ListConfirmedUserAccountsAsync(It.IsAny<CancellationToken>(), null))
            .ReturnsAsync(users);

        var result = await sut.RetrieveAdminHubDetailsAsync(user, CancellationToken.None);

        Assert.True(result.IsSuccess);

        MockAdminHubService
            .Verify(x => x.RetrieveAdminHubDataAsync(
                It.Is<GetAdminHubsDataRequestModel>(x => x.PerformingUserAccountType == user.AccountType && x.PerformingUserId == user.UserAccountId.Value),
                It.IsAny<CancellationToken>()), Times.Once);
        MockAdminHubService.VerifyNoOtherCalls();
        MockUserAccountService.Verify(x => x.ListConfirmedUserAccountsAsync(It.IsAny<CancellationToken>(), null), Times.Once);
        MockUserAccountService.VerifyNoOtherCalls();
        MockAuditService.VerifyNoOtherCalls();

        Assert.Equal(Guid.Empty, result.Value.Id);
        Assert.Equal("You are not currently a manager at an admin hub", result.Value.Name);
        Assert.Equal(adminHubs, result.Value.AdminHubs);
        Assert.Equal(users.Count(), result.Value.AllAdminOfficers.Count);
        foreach (var userAccount in users)
        {
            Assert.Contains(result.Value.AllAdminOfficers, x =>
                x.Id == userAccount.Id
                && x.AccountType == userAccount.AccountType
                && x.Email == userAccount.Email
                && x.FirstName == userAccount.FirstName
                && x.LastName == userAccount.LastName
                && x.IsActiveAccount == userAccount.Status is Status.Confirmed);
        }
    }

    [Fact]
    public async Task WhenAdminHubRetrievalFails()
    {
        var principal = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(
            localAccountId: RequestContextUserId,
            accountTypeInternal: AccountTypeInternal.AdminHubManager);
        var user = new InternalUser(principal);

        var sut = CreateSut();

        MockAdminHubService
            .Setup(x => x.RetrieveAdminHubDataAsync(It.IsAny<GetAdminHubsDataRequestModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<IReadOnlyCollection<AdminHubModel>, ManageAdminHubOutcome>(ManageAdminHubOutcome.AdminHubsNotFound));

        var result = await sut.RetrieveAdminHubDetailsAsync(user, CancellationToken.None);

        Assert.True(result.IsFailure);

        MockAdminHubService
            .Verify(x => x.RetrieveAdminHubDataAsync(
                It.Is<GetAdminHubsDataRequestModel>(x => x.PerformingUserAccountType == user.AccountType && x.PerformingUserId == user.UserAccountId.Value),
                It.IsAny<CancellationToken>()), Times.Once);
        MockAdminHubService.VerifyNoOtherCalls();
        MockUserAccountService.VerifyNoOtherCalls();
        MockAuditService.VerifyNoOtherCalls();
    }

    private Web.Services.AdminHub.ManageAdminHubUseCase CreateSut()
    {
        var user = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(
            localAccountId: RequestContextUserId,
            accountTypeInternal: AccountTypeInternal.AdminHubManager);
        var requestContext = new RequestContext(
            RequestContextCorrelationId,
            new RequestUserModel(user));

        MockAdminHubService.Reset();
        MockAuditService.Reset();
        MockUserAccountService.Reset();

        return new Web.Services.AdminHub.ManageAdminHubUseCase(
            MockUserAccountService.Object,
            MockAdminHubService.Object,
            MockAuditService.Object,
            requestContext,
            new NullLogger<Web.Services.AdminHub.ManageAdminHubUseCase>());
    }
}