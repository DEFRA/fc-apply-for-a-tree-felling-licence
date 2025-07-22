using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;
using Forestry.Flo.Internal.Web.Services;
using Forestry.Flo.Internal.Web.Services.FellingLicenceApplication;
using Forestry.Flo.Services.Applicants.Entities.UserAccount;
using Forestry.Flo.Services.Applicants.Entities.WoodlandOwner;
using Forestry.Flo.Services.Applicants.Repositories;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.InternalUsers.Services;
using Forestry.Flo.Services.Notifications;
using Forestry.Flo.Services.Notifications.Services;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NodaTime;
using System.Reflection;
using Forestry.Flo.Services.AdminHubs.Model;
using Forestry.Flo.Services.Applicants.Models;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications;
using Forestry.Flo.Services.InternalUsers.Entities.UserAccount;
using FellingLicenceStatus = Forestry.Flo.Services.FellingLicenceApplications.Entities.FellingLicenceStatus;
using UserAccount = Forestry.Flo.Services.Applicants.Entities.UserAccount.UserAccount;
using Forestry.Flo.Services.FellingLicenceApplications.Services;

namespace Forestry.Flo.Internal.Web.Tests.Services.AssignToUserUseCaseTests;

public abstract class AssignToUserUseCaseTestsBase
{
    protected readonly Mock<IUserAccountService> MockInternalUserAccountService = new();
    protected readonly Mock<IRetrieveUserAccountsService> MockExternalUserAccountService = new();
    protected readonly Mock<IFellingLicenceApplicationInternalRepository> MockFlaRepository = new();
    protected readonly Mock<IRetrieveWoodlandOwners> MockWoodlandOwnerRepository = new();
    protected readonly Mock<IAuditService<AssignToUserUseCase>> MockAuditService = new();
    private readonly Mock<IUnitOfWork> _mockUnitOfWork = new();
    protected readonly Mock<ISendNotifications> MockSendNotifications = new();
    protected readonly Mock<IAgencyRepository> MockAgencyRepository = new();
    protected readonly Mock<IGetConfiguredFcAreas> MockGetConfiguredFcAreas = new();
    protected readonly Mock<IGetFellingLicenceApplicationForInternalUsers> MockGetFellingLicenceApplication = new();
    protected readonly Mock<IUpdateFellingLicenceApplication> MockUpdateFellingLicenceApplication = new();
    protected readonly Mock<IAgentAuthorityService> MockAgentAuthorityService = new();

    protected readonly InternalUser _testUser;
    protected readonly List<string> _validAreaCodes;
    protected readonly string AdminHubAddress = "admin hub address";

    protected AssignToUserUseCaseTestsBase()
    {
        var userPrincipal = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(
            localAccountId: Guid.NewGuid(),
            accountTypeInternal: Flo.Services.Common.User.AccountTypeInternal.AdminOfficer);
        _testUser = new InternalUser(userPrincipal);
        _validAreaCodes = new List<string>{"018","010"};
    }

    protected AssignToUserUseCase CreateSut()
    {
        MockInternalUserAccountService.Reset();
        MockExternalUserAccountService.Reset();
        MockFlaRepository.Reset();
        MockWoodlandOwnerRepository.Reset();
        MockAuditService.Reset();
        _mockUnitOfWork.Reset();
        MockSendNotifications.Reset();
        MockAgencyRepository.Reset();
        MockGetConfiguredFcAreas.Reset();
        MockGetFellingLicenceApplication.Reset();
        MockUpdateFellingLicenceApplication.Reset();
        MockAgentAuthorityService.Reset();

        MockGetConfiguredFcAreas.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(
            Result.Success(
                new List<ConfiguredFcArea>
                {
                    new (new AreaModel { Code = "NW", Id = Guid.NewGuid(), Name = "North West" }, _validAreaCodes[0], "TestHub1"),
                    new (new AreaModel { Code = "SW", Id = Guid.NewGuid(), Name = "South West" }, _validAreaCodes[1], "TestHub2")
                }));
        MockGetConfiguredFcAreas.Setup(x => x.TryGetAdminHubAddress(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AdminHubAddress);

        return new AssignToUserUseCase(
            MockInternalUserAccountService.Object,
            MockExternalUserAccountService.Object,
            MockFlaRepository.Object,
            MockWoodlandOwnerRepository.Object,
            MockAuditService.Object,
            MockGetConfiguredFcAreas.Object,
            new RequestContext("test", new RequestUserModel(_testUser.Principal)),
            MockSendNotifications.Object,
            MockGetFellingLicenceApplication.Object,
            MockUpdateFellingLicenceApplication.Object,
            MockAgentAuthorityService.Object,
            new NullLogger<AssignToUserUseCase>());

    }

    protected async Task<Result<T>> RetrieveFlaSummaryShouldReturnFailureWhenFlaCouldNotBeFound<T>(
        Func<Task<Result<T>>> runTest,
        Guid applicationId,
        Action? assertInternalUserAccountService = null) where T : FellingLicenceApplicationPageViewModel
    {
        // Arrange
        MockFlaRepository.Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.None);

        // Act
        var result = await runTest();

        // Assert
        Assert.True(result.IsFailure);

        MockFlaRepository.Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        MockWoodlandOwnerRepository.VerifyNoOtherCalls();
        MockExternalUserAccountService.VerifyNoOtherCalls();
        assertInternalUserAccountService?.Invoke();
        MockInternalUserAccountService.VerifyNoOtherCalls();
        MockAuditService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(y => y.EventName == AuditEvents.AssignFellingLicenceApplicationToStaffMemberFailure), It.IsAny<CancellationToken>()), Times.Once);

        return result;
    }

    protected async Task<Result<T>> RetrieveFlaSummaryShouldReturnFailureWhenWoodlandOwnerForFlaCouldNotBeFound<T>(
        Func<Task<Result<T>>> runTest,
        FellingLicenceApplication fla,
        Action? assertInternalUserAccountService = null) where T : FellingLicenceApplicationPageViewModel
    {
        // Arrange
        MockFlaRepository.Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.From(fla));

        MockWoodlandOwnerRepository.Setup(x => x.RetrieveWoodlandOwnerByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<WoodlandOwnerModel>(UserDbErrorReason.NotFound.ToString()));

        // Act
        var result = await runTest();

        // Assert
        Assert.True(result.IsFailure);

        MockFlaRepository.Verify(x => x.GetAsync(fla.Id, It.IsAny<CancellationToken>()), Times.Once);
        MockWoodlandOwnerRepository.Verify(x => x.RetrieveWoodlandOwnerByIdAsync(fla.WoodlandOwnerId, It.IsAny<CancellationToken>()), Times.Once);
        MockExternalUserAccountService.VerifyNoOtherCalls();
        assertInternalUserAccountService?.Invoke();
        MockInternalUserAccountService.VerifyNoOtherCalls();
        MockAuditService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(y => y.EventName == AuditEvents.AssignFellingLicenceApplicationToStaffMemberFailure), It.IsAny<CancellationToken>()), Times.Once);

        return result;
    }

    protected async Task<Result<T>> RetrieveFlaSummaryShouldReturnFailureWhenInternalUserAccountForAssigneeOnFlaCouldNotBeFound<T>(
            Func<Task<Result<T>>> runTest,
            FellingLicenceApplication fla,
            WoodlandOwnerModel woodlandOwner,
            UserAccount createdByUser,
            AssigneeHistory assigneeHistory,
            Action? assertInternalUserAccountService = null) where T : FellingLicenceApplicationPageViewModel
    {
        // Arrange
        assigneeHistory.Role = AssignedUserRole.AdminOfficer;
        fla.AssigneeHistories = new List<AssigneeHistory> { assigneeHistory };

        MockFlaRepository.Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.From(fla));

        MockWoodlandOwnerRepository.Setup(x => x.RetrieveWoodlandOwnerByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(woodlandOwner));

        MockInternalUserAccountService.Setup(x => x.GetUserAccountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<Flo.Services.InternalUsers.Entities.UserAccount.UserAccount>.None);

        MockExternalUserAccountService.Setup(x => x.RetrieveUserAccountEntityByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(createdByUser));

        // Act
        var result = await runTest();

        // Assert
        Assert.True(result.IsFailure);

        MockFlaRepository.Verify(x => x.GetAsync(fla.Id, It.IsAny<CancellationToken>()), Times.Once);
        MockWoodlandOwnerRepository.Verify(x => x.RetrieveWoodlandOwnerByIdAsync(fla.WoodlandOwnerId, It.IsAny<CancellationToken>()), Times.Once);
        MockInternalUserAccountService.Verify(x => x.GetUserAccountAsync(assigneeHistory.AssignedUserId, It.IsAny<CancellationToken>()), Times.Once);
        assertInternalUserAccountService?.Invoke();
        MockAuditService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(y => y.EventName == AuditEvents.AssignFellingLicenceApplicationToStaffMemberFailure), It.IsAny<CancellationToken>()), Times.Once);

        return result;
    }

    protected async Task<Result<T>> RetrieveFlaSummaryShouldReturnSuccessWhenDetailsRetrieved<T>(
        Func<Task<Result<T>>> runTest,
        FellingLicenceApplication fla,
        WoodlandOwnerModel woodlandOwner,
        UserAccount externalApplicantAccount,
        Flo.Services.InternalUsers.Entities.UserAccount.UserAccount internalUserAccount,
        AssigneeHistory externalAssigneeHistory,
        AssigneeHistory internalAssigneeHistory,
        Action? assertInternalUserAccountService = null,
        bool setupAsSubmitted = true,
        bool setupInternalUser = true,
        bool setupAssigneeHistory = true) where T: FellingLicenceApplicationPageViewModel
    {
        // Arrange
        externalAssigneeHistory.Role = AssignedUserRole.Author;
        externalApplicantAccount.AccountType = AccountTypeExternal.WoodlandOwner;

        if (setupInternalUser)
        {
            internalUserAccount.AccountType = Flo.Services.Common.User.AccountTypeInternal.AdminOfficer;
        }

        if (setupAssigneeHistory)
        {
            internalAssigneeHistory.Role = AssignedUserRole.AdminOfficer;
        }

        fla.AssigneeHistories = new List<AssigneeHistory> { externalAssigneeHistory, internalAssigneeHistory };
        
        if (setupAsSubmitted)
        {
            fla.StatusHistories = new List<StatusHistory> { new StatusHistory { Created = DateTime.UtcNow.AddHours(-1), Status = FellingLicenceStatus.Submitted, CreatedById = Guid.NewGuid() } };
        }

        MockFlaRepository.Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.From(fla));

        MockWoodlandOwnerRepository.Setup(x => x.RetrieveWoodlandOwnerByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(woodlandOwner));

        MockExternalUserAccountService.Setup(x => x.RetrieveUserAccountEntityByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(externalApplicantAccount));

        MockInternalUserAccountService
            .Setup(x => x.GetUserAccountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<Flo.Services.InternalUsers.Entities.UserAccount.UserAccount>.From(internalUserAccount));

        MockFlaRepository.Setup(x => x.GetStatusHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(fla.StatusHistories);

        // Act
        var result = await runTest();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value.FellingLicenceApplicationSummary);

        MockFlaRepository.Verify(x => x.GetAsync(fla.Id, It.IsAny<CancellationToken>()), Times.Once);
        MockWoodlandOwnerRepository.Verify(x => x.RetrieveWoodlandOwnerByIdAsync(fla.WoodlandOwnerId, It.IsAny<CancellationToken>()), Times.Once);
        MockExternalUserAccountService.Verify(x => x.RetrieveUserAccountEntityByIdAsync(externalAssigneeHistory.AssignedUserId, It.IsAny<CancellationToken>()), Times.Once);
        assertInternalUserAccountService?.Invoke();
        MockInternalUserAccountService.Verify(x => x.GetUserAccountAsync(internalAssigneeHistory.AssignedUserId, It.IsAny<CancellationToken>()), Times.Once);
        MockAuditService.VerifyNoOtherCalls();

        Assert.Equal(fla.Id, result.Value.FellingLicenceApplicationSummary.Id);
        Assert.Equal(fla.ApplicationReference, result.Value.FellingLicenceApplicationSummary.ApplicationReference);
        var expectedStatus = fla.StatusHistories.Any()
            ? fla.StatusHistories.OrderByDescending(x => x.Created).First().Status
            : FellingLicenceStatus.Draft;
        Assert.Equal(expectedStatus,(FellingLicenceStatus) result.Value.FellingLicenceApplicationSummary.Status);
        Assert.Equal(fla.SubmittedFlaPropertyDetail?.Name, result.Value.FellingLicenceApplicationSummary.PropertyName);
        var expectedWoodlandOwnerName = woodlandOwner.IsOrganisation
            ? woodlandOwner.OrganisationName!
            : woodlandOwner.ContactName!;
        Assert.Equal(expectedWoodlandOwnerName, result.Value.FellingLicenceApplicationSummary.WoodlandOwnerName);

        Assert.Equal(2, result.Value.FellingLicenceApplicationSummary.AssigneeHistories.Count);

        Assert.Contains(result.Value.FellingLicenceApplicationSummary.AssigneeHistories, x =>
            x.Role == AssignedUserRole.Author
            && x.TimestampAssigned == externalAssigneeHistory.TimestampAssigned
            && x.TimestampUnassigned == externalAssigneeHistory.TimestampUnassigned
            && x.ExternalApplicant != null
            && x.ExternalApplicant.Id == externalApplicantAccount.Id
            && x.ExternalApplicant.ApplicantType == ExternalApplicantType.WoodlandOwner
            && x.ExternalApplicant.Email == externalApplicantAccount.Email
            && x.ExternalApplicant.FirstName == externalApplicantAccount.FirstName
            && x.ExternalApplicant.LastName == externalApplicantAccount.LastName
            && x.ExternalApplicant.IsActiveAccount == externalApplicantAccount.Status is UserAccountStatus.Active
            && x.UserAccount == null);
        Assert.Contains(result.Value.FellingLicenceApplicationSummary.AssigneeHistories, x =>
            x.Role == internalAssigneeHistory.Role
            && x.TimestampAssigned == internalAssigneeHistory.TimestampAssigned
            && x.TimestampUnassigned == internalAssigneeHistory.TimestampUnassigned
            && x.ExternalApplicant == null
            && x.UserAccount != null
            && x.UserAccount.Id == internalUserAccount.Id
            && x.UserAccount.Status == internalUserAccount.Status
            && x.UserAccount.AccountType == internalUserAccount.AccountType
            && x.UserAccount.Email == internalUserAccount.Email
            && x.UserAccount.FirstName == internalUserAccount.FirstName
            && x.UserAccount.LastName == internalUserAccount.LastName
            && x.UserAccount.IsActiveAccount == internalUserAccount.Status is Status.Confirmed
           );

        return result;
    }

    protected async Task<Result<T>> RetrieveFlaSummaryShouldReturnFailure_WhenStatusIsNotValid<T>(
            Func<Task<Result<T>>> runTest,
            FellingLicenceApplication fla,
            WoodlandOwnerModel woodlandOwner,
            UserAccount createdByUser,
            Flo.Services.InternalUsers.Entities.UserAccount.UserAccount internalUserAccount,
            AssigneeHistory assigneeHistory,
            string errorMessage,
            Action? assertInternalUserAccountService = null) where T : FellingLicenceApplicationPageViewModel
    {
        // Arrange
        assigneeHistory.Role = AssignedUserRole.AdminOfficer;
        fla.AssigneeHistories = new List<AssigneeHistory> { assigneeHistory };

        MockFlaRepository.Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.From(fla));

        MockWoodlandOwnerRepository.Setup(x => x.RetrieveWoodlandOwnerByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(woodlandOwner));

        MockInternalUserAccountService.Setup(x => x.GetUserAccountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<Flo.Services.InternalUsers.Entities.UserAccount.UserAccount>.From(internalUserAccount));

        MockExternalUserAccountService.Setup(x => x.RetrieveUserAccountEntityByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(createdByUser));

        // Act
        var result = await runTest();

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(errorMessage, result.Error);

        MockFlaRepository.Verify(x => x.GetAsync(fla.Id, It.IsAny<CancellationToken>()), Times.Once);
        MockWoodlandOwnerRepository.Verify(x => x.RetrieveWoodlandOwnerByIdAsync(fla.WoodlandOwnerId, It.IsAny<CancellationToken>()), Times.Once);
        MockInternalUserAccountService.Verify(x => x.GetUserAccountAsync(assigneeHistory.AssignedUserId, It.IsAny<CancellationToken>()), Times.Once);
        assertInternalUserAccountService?.Invoke();
        MockAuditService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(y => y.EventName == AuditEvents.AssignFellingLicenceApplicationToStaffMemberFailure), It.IsAny<CancellationToken>()), Times.Once);

        return result;
    }
}