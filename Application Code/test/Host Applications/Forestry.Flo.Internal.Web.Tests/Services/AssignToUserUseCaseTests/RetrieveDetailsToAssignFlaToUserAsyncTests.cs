using Forestry.Flo.Services.Applicants.Entities.WoodlandOwner;
using Forestry.Flo.Services.Applicants.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.InternalUsers.Entities.UserAccount;
using Forestry.Flo.Tests.Common;
using Moq;

namespace Forestry.Flo.Internal.Web.Tests.Services.AssignToUserUseCaseTests;

public class RetrieveDetailsToAssignFlaToUserAsyncTests : AssignToUserUseCaseTestsBase
{
    //TODO - add this test if and when there is error handling in useraccountservice
    //[Theory, AutoData]
    //public async Task ShouldReturnFailureWhenUsersUnableToBeRetrieved(
    //    Guid applicationId, 
    //    AssignedUserRole role)
    //{

    //}

    [Theory, AutoMoqData]
    public async Task ShouldReturnFailureWhenFellingLicenceApplicationCouldNotBeFound(
        Guid applicationId,
        AssignedUserRole role,
        IEnumerable<UserAccount> users,
        string returnUrl)
    {
        var sut = CreateSut();

        MockInternalUserAccountService.Setup(x => x.ListConfirmedUserAccountsAsync(It.IsAny<CancellationToken>(), null))
            .ReturnsAsync(users);

        await RetrieveFlaSummaryShouldReturnFailureWhenFlaCouldNotBeFound(
            async () => await sut.RetrieveDetailsToAssignFlaToUserAsync(applicationId, role, returnUrl, _testUser, CancellationToken.None),
            applicationId,
            () => MockInternalUserAccountService.Verify(x => x.ListConfirmedUserAccountsAsync(It.IsAny<CancellationToken>(), null)));
    }

    [Theory, AutoMoqData]
    public async Task ShouldReturnFailureWhenWoodlandOwnerForFlaCouldNotBeFound(
        FellingLicenceApplication fla,
        AssignedUserRole role,
        IEnumerable<UserAccount> users,
        string returnUrl)
    {
        var sut = CreateSut();

        MockInternalUserAccountService.Setup(x => x.ListConfirmedUserAccountsAsync(It.IsAny<CancellationToken>(), null))
            .ReturnsAsync(users);

        await RetrieveFlaSummaryShouldReturnFailureWhenWoodlandOwnerForFlaCouldNotBeFound(
            async () => await sut.RetrieveDetailsToAssignFlaToUserAsync(fla.Id, role, returnUrl, _testUser, CancellationToken.None),
            fla,
            () => MockInternalUserAccountService.Verify(x => x.ListConfirmedUserAccountsAsync(It.IsAny<CancellationToken>(), null)));
    }

    [Theory, AutoMoqData]
    public async Task ShouldReturnFailureWhenInternalUserAccountForAssigneeOnFlaCouldNotBeFound(
        FellingLicenceApplication fla,
        AssignedUserRole role,
        WoodlandOwnerModel woodlandOwner,
        Forestry.Flo.Services.Applicants.Entities.UserAccount.UserAccount createdByUser,
        AssigneeHistory assigneeHistory,
        IEnumerable<UserAccount> users,
        string returnUrl)
    {
        var sut = CreateSut();

        MockInternalUserAccountService.Setup(x => x.ListConfirmedUserAccountsAsync(It.IsAny<CancellationToken>(), null))
            .ReturnsAsync(users);

        await RetrieveFlaSummaryShouldReturnFailureWhenInternalUserAccountForAssigneeOnFlaCouldNotBeFound(
            async () => await sut.RetrieveDetailsToAssignFlaToUserAsync(fla.Id, role, returnUrl, _testUser, CancellationToken.None),
            fla,
            woodlandOwner,
            createdByUser,
            assigneeHistory,
            () => MockInternalUserAccountService.Verify(x => x.ListConfirmedUserAccountsAsync(It.IsAny<CancellationToken>(), null)));
    }

    [Theory, AutoMoqData]
    public async Task ShouldReturnSuccessWhenDetailsRetrieved(
        FellingLicenceApplication fla,
        WoodlandOwnerModel woodlandOwner,
        Flo.Services.Applicants.Entities.UserAccount.UserAccount externalApplicantAccount,
        UserAccount internalUserAccount,
        AssigneeHistory externalAssigneeHistory,
        AssigneeHistory internalAssigneeHistory,
        AssignedUserRole role,
        IEnumerable<UserAccount> users,
        string returnUrl)
    {
        var sut = CreateSut();

        fla.LinkedPropertyProfile.ProposedFellingDetails = new List<ProposedFellingDetail>();

        MockInternalUserAccountService.Setup(x => x.ListConfirmedUserAccountsAsync(It.IsAny<CancellationToken>(), null))
            .ReturnsAsync(users);

        foreach (var user in users)
        {
            user.CanApproveApplications = true;
        }

        var result = await RetrieveFlaSummaryShouldReturnSuccessWhenDetailsRetrieved(
            async () => await sut.RetrieveDetailsToAssignFlaToUserAsync(fla.Id, role, returnUrl, _testUser, CancellationToken.None),
            fla,
            woodlandOwner,
            externalApplicantAccount,
            internalUserAccount,
            externalAssigneeHistory,
            internalAssigneeHistory,
            () => MockInternalUserAccountService.Verify(x => x.ListConfirmedUserAccountsAsync(It.IsAny<CancellationToken>(), null)));

        Assert.NotNull(result.Value.UserAccounts);
        Assert.NotEmpty(result.Value.UserAccounts!);
        Assert.Equal(users.Count(), result.Value.UserAccounts!.Count());
        Assert.Equal(returnUrl, result.Value.ReturnUrl);

        foreach (var userAccount in users)
        {
            Assert.Contains(result.Value.UserAccounts!, x =>
                x.AccountType == userAccount.AccountType
                && x.Id == userAccount.Id
                && x.Email == userAccount.Email
                && x.FirstName == userAccount.FirstName
                && x.LastName == userAccount.LastName
                && x.IsActiveAccount == userAccount.Status is Status.Confirmed);
        }
    }

    [Theory, AutoMoqData]
    public async Task ShouldReturnFailure_WhenStatusWithdrawn(
        FellingLicenceApplication fla,
        WoodlandOwnerModel woodlandOwner,
        AssigneeHistory assigneeHistory,
        UserAccount internalUserAccount,
        string returnUrl,
        Flo.Services.Applicants.Entities.UserAccount.UserAccount createdByUser,
        AssignedUserRole role,
        IEnumerable<UserAccount> users)
    {
        var sut = CreateSut();

        fla.LinkedPropertyProfile.ProposedFellingDetails = new List<ProposedFellingDetail>();

        MockInternalUserAccountService.Setup(x => x.ListConfirmedUserAccountsAsync(It.IsAny<CancellationToken>(), null))
            .ReturnsAsync(users);

        fla.StatusHistories = new List<StatusHistory>()
        {
            new StatusHistory() { Status = FellingLicenceStatus.Draft, Created = DateTime.Now.AddDays(-3), CreatedById = fla.CreatedById },
            new StatusHistory() { Status = FellingLicenceStatus.Submitted, Created = DateTime.Now.AddDays(-2), CreatedById = createdByUser.Id },
            new StatusHistory() { Status = FellingLicenceStatus.WithApplicant, Created = DateTime.Now.AddDays(-1), CreatedById = _testUser.UserAccountId },
            new StatusHistory() { Status = FellingLicenceStatus.Withdrawn, Created = DateTime.Now.AddMinutes(-30), CreatedById = createdByUser.Id }
        };

        var errorMessage = "Cannot assign to an application that has been Withdrawn.";

        var result = await RetrieveFlaSummaryShouldReturnFailure_WhenStatusIsNotValid(
            async () => await sut.RetrieveDetailsToAssignFlaToUserAsync(fla.Id, role, returnUrl, _testUser, CancellationToken.None),
            fla,
            woodlandOwner,
            createdByUser,
            internalUserAccount,
            assigneeHistory,
            errorMessage,
            () => MockInternalUserAccountService.Verify(x => x.ListConfirmedUserAccountsAsync(It.IsAny<CancellationToken>(), null)));

        Assert.True(result.IsFailure);
        Assert.Equal(errorMessage, result.Error);
    }

    [Theory, AutoMoqData]
    public async Task ShouldReturnFailure_WhenStatusRefused(
        FellingLicenceApplication fla,
        WoodlandOwnerModel woodlandOwner,
        AssigneeHistory assigneeHistory,
        UserAccount internalUserAccount,
        string returnUrl,
        Flo.Services.Applicants.Entities.UserAccount.UserAccount createdByUser,
        AssignedUserRole role,
        IEnumerable<UserAccount> users)
    {
        var sut = CreateSut();

        fla.LinkedPropertyProfile.ProposedFellingDetails = new List<ProposedFellingDetail>();

        MockInternalUserAccountService.Setup(x => x.ListConfirmedUserAccountsAsync(It.IsAny<CancellationToken>(), null))
            .ReturnsAsync(users);

        fla.StatusHistories = new List<StatusHistory>()
        {
            new StatusHistory() { Status = FellingLicenceStatus.Draft, Created = DateTime.Now.AddDays(-3), CreatedById = fla.CreatedById },
            new StatusHistory() { Status = FellingLicenceStatus.Submitted, Created = DateTime.Now.AddDays(-2), CreatedById = createdByUser.Id },
            new StatusHistory() { Status = FellingLicenceStatus.Refused, Created = DateTime.Now.AddDays(-1), CreatedById = _testUser.UserAccountId }
        };

        var errorMessage = "Cannot assign to an application that has been Refused.";

        var result = await RetrieveFlaSummaryShouldReturnFailure_WhenStatusIsNotValid(
            async () => await sut.RetrieveDetailsToAssignFlaToUserAsync(fla.Id, role, returnUrl, _testUser, CancellationToken.None),
            fla,
            woodlandOwner,
            createdByUser,
            internalUserAccount,
            assigneeHistory,
            errorMessage,
            () => MockInternalUserAccountService.Verify(x => x.ListConfirmedUserAccountsAsync(It.IsAny<CancellationToken>(), null)));

        Assert.True(result.IsFailure);
        Assert.Equal(errorMessage, result.Error);
    }

    [Theory, AutoMoqData]
    public async Task ShouldReturnFailure_WhenStatusApproved(
        FellingLicenceApplication fla,
        WoodlandOwnerModel woodlandOwner,
        AssigneeHistory assigneeHistory,
        UserAccount internalUserAccount,
        string returnUrl,
        Flo.Services.Applicants.Entities.UserAccount.UserAccount createdByUser,
        AssignedUserRole role,
        IEnumerable<UserAccount> users)
    {
        var sut = CreateSut();

        fla.LinkedPropertyProfile.ProposedFellingDetails = new List<ProposedFellingDetail>();

        MockInternalUserAccountService.Setup(x => x.ListConfirmedUserAccountsAsync(It.IsAny<CancellationToken>(), null))
            .ReturnsAsync(users);

        fla.StatusHistories = new List<StatusHistory>()
        {
            new StatusHistory() { Status = FellingLicenceStatus.Draft, Created = DateTime.Now.AddDays(-3), CreatedById = fla.CreatedById },
            new StatusHistory() { Status = FellingLicenceStatus.Submitted, Created = DateTime.Now.AddDays(-2), CreatedById = createdByUser.Id },
            new StatusHistory() { Status = FellingLicenceStatus.SentForApproval, Created = DateTime.Now.AddDays(-1), CreatedById = _testUser.UserAccountId },
            new StatusHistory() { Status = FellingLicenceStatus.Approved, Created = DateTime.Now.AddMinutes(-30), CreatedById = _testUser.UserAccountId }
        };

        var errorMessage = "Cannot assign to an application that has been Approved.";

        var result = await RetrieveFlaSummaryShouldReturnFailure_WhenStatusIsNotValid(
            async () => await sut.RetrieveDetailsToAssignFlaToUserAsync(fla.Id, role, returnUrl, _testUser, CancellationToken.None),
            fla,
            woodlandOwner,
            createdByUser,
            internalUserAccount,
            assigneeHistory,
            errorMessage,
            () => MockInternalUserAccountService.Verify(x => x.ListConfirmedUserAccountsAsync(It.IsAny<CancellationToken>(), null)));

        Assert.True(result.IsFailure);
        Assert.Equal(errorMessage, result.Error);
    }

}