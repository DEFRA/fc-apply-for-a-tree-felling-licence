using AutoFixture;
using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Infrastructure;
using Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;
using Forestry.Flo.Internal.Web.Services;
using Forestry.Flo.Internal.Web.Services.FellingLicenceApplication;
using Forestry.Flo.Services.Applicants.Entities.UserAccount;
using Forestry.Flo.Services.Applicants.Models;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.FellingLicenceApplications.Services.WoodlandOfficerReviewSubstatuses;
using Forestry.Flo.Services.Gis.Interfaces;
using Forestry.Flo.Services.InternalUsers.Services;
using Forestry.Flo.Services.Notifications.Services;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using NodaTime;
using FellingLicenceStatus = Forestry.Flo.Services.FellingLicenceApplications.Entities.FellingLicenceStatus;
using UserAccount = Forestry.Flo.Services.Applicants.Entities.UserAccount.UserAccount;

namespace Forestry.Flo.Internal.Web.Tests.Services.AssignToApplicantTests;

public abstract class AssignToApplicantUseCaseTestsBase
{
    protected readonly Mock<IUserAccountService> MockInternalUserAccountService;
    protected readonly Mock<IFellingLicenceApplicationInternalRepository> MockFlaRepository;
    protected readonly Mock<IRetrieveWoodlandOwners> MockWoodlandOwnerService;
    protected readonly Mock<IAuditService<AssignToApplicantUseCase>> MockAuditService;
    protected readonly Mock<IClock> MockClock;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    protected readonly Mock<ISendNotifications> MockSendNotifications;
    protected readonly Mock<IGetFellingLicenceApplicationForInternalUsers> MockGetFellingLicenceApplication;
    protected readonly InternalUser TestUser;
    protected readonly Mock<IOptions<ExternalApplicantSiteOptions>> Settings;
    protected readonly Mock<IOptions<LarchOptions>> LarchSettings;
    protected readonly Mock<IAgentAuthorityService> MockAgentAuthorityService;
    protected readonly Mock<IRetrieveUserAccountsService> MockRetrieveUserAccountsService;
    protected readonly Mock<IUpdateFellingLicenceApplication> UpdateFellingLicenceApplication;
    protected readonly Mock<ILarchCheckService> MockLarchCheckService;
    private readonly Mock<IGetConfiguredFcAreas> _mockGetConfiguredFcAreas;
    protected readonly Mock<IPublicRegister> MockPublicRegister;
    protected readonly Mock<IUpdateFellingLicenceApplication> MockUpdateFellingLicenceApplication;
    private readonly Mock<IWoodlandOfficerReviewSubStatusService> _woodlandOfficerReviewSubStatusService = new();


    protected const string AdminHubFooter = "admin hub address";
    protected readonly Fixture Fixture;

    protected AssignToApplicantUseCaseTestsBase()
    {
        var userPrincipal = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(
            localAccountId: Guid.NewGuid(),
            accountTypeInternal: AccountTypeInternal.AdminOfficer);
        TestUser = new InternalUser(userPrincipal);

        _mockUnitOfWork = new Mock<IUnitOfWork>();
        MockClock = new Mock<IClock>();
        MockInternalUserAccountService = new Mock<IUserAccountService>();
        MockAgentAuthorityService = new Mock<IAgentAuthorityService>();
        MockFlaRepository = new Mock<IFellingLicenceApplicationInternalRepository>();
        MockWoodlandOwnerService = new Mock<IRetrieveWoodlandOwners>();
        MockAuditService = new Mock<IAuditService<AssignToApplicantUseCase>>();
        MockSendNotifications = new Mock<ISendNotifications>();
        MockGetFellingLicenceApplication = new();
        MockRetrieveUserAccountsService = new Mock<IRetrieveUserAccountsService>();
        Settings = new Mock<IOptions<ExternalApplicantSiteOptions>>();
        LarchSettings = new Mock<IOptions<LarchOptions>>();
        UpdateFellingLicenceApplication = new Mock<IUpdateFellingLicenceApplication>();
        MockLarchCheckService = new Mock<ILarchCheckService>();
        _mockGetConfiguredFcAreas = new Mock<IGetConfiguredFcAreas>();
        MockPublicRegister = new Mock<IPublicRegister>();
        Fixture = new Fixture();
    }

    protected AssignToApplicantUseCase CreateSut()
    {
        MockInternalUserAccountService.Reset();
        MockFlaRepository.Reset();
        MockWoodlandOwnerService.Reset();
        MockAuditService.Reset();
        _mockUnitOfWork.Reset();
        MockClock.Reset();
        MockSendNotifications.Reset();
        Settings.Reset();
        LarchSettings.Reset();
        MockAgentAuthorityService.Reset();
        MockRetrieveUserAccountsService.Reset();
        UpdateFellingLicenceApplication.Reset();
        MockGetFellingLicenceApplication.Reset();
        MockLarchCheckService.Reset();
        _mockGetConfiguredFcAreas.Reset();

        Settings.Setup(c => c.Value).Returns(new ExternalApplicantSiteOptions { BaseUrl = "https://localhost:7253/" });

        _mockGetConfiguredFcAreas.Setup(x => x.TryGetAdminHubAddress(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AdminHubFooter);

        LarchSettings.Setup(c => c.Value).Returns(new LarchOptions
        {
            EarlyFadDay = 30,
            EarlyFadMonth = 6,
            LateFadDay = 31,
            LateFadMonth = 10,
            FlyoverPeriodStartDay = 1,
            FlyoverPeriodStartMonth = 4,
            FlyoverPeriodEndDay = 31,
            FlyoverPeriodEndMonth = 8
        });

        return new AssignToApplicantUseCase(
            MockInternalUserAccountService.Object,
            MockFlaRepository.Object,
            MockWoodlandOwnerService.Object,
            MockAuditService.Object,
            new RequestContext("test", new RequestUserModel(TestUser.Principal)),
            new NullLogger<AssignToApplicantUseCase>(),
            Settings.Object,
            MockSendNotifications.Object,
            MockGetFellingLicenceApplication.Object,
            MockRetrieveUserAccountsService.Object,
            UpdateFellingLicenceApplication.Object,
            MockAgentAuthorityService.Object,
            _mockGetConfiguredFcAreas.Object,
            LarchSettings.Object,
            MockLarchCheckService.Object,
            MockPublicRegister.Object,
            _woodlandOfficerReviewSubStatusService.Object,
            MockClock.Object);
    }

    protected async Task<Result<T>> RetrieveFlaSummaryShouldReturnFailureWhenFlaCouldNotBeFound<T>(
        Func<Task<Result<T>>> runTest,
        Guid applicationId,
        Action? assertInternalUserAccountService = null) where T : FellingLicenceApplicationPageViewModel
    {
        MockFlaRepository.Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.None);

        var result = await runTest();

        Assert.True(result.IsFailure);

        MockFlaRepository.Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        MockWoodlandOwnerService.VerifyNoOtherCalls();
        MockRetrieveUserAccountsService.VerifyNoOtherCalls();
        assertInternalUserAccountService?.Invoke();
        MockInternalUserAccountService.VerifyNoOtherCalls();
        MockAuditService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(y => y.EventName == AuditEvents.AssignFellingLicenceApplicationToApplicantFailure), It.IsAny<CancellationToken>()), Times.Once);

        return result;
    }

    protected async Task<Result<T>> RetrieveFlaSummaryShouldReturnFailureWhenWoodlandOwnerForFlaCouldNotBeFound<T>(
        Func<Task<Result<T>>> runTest,
        FellingLicenceApplication fla,
        Action? assertInternalUserAccountService = null) where T : FellingLicenceApplicationPageViewModel
    {
        MockFlaRepository.Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.From(fla));

        MockWoodlandOwnerService.Setup(x => x.RetrieveWoodlandOwnerByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<WoodlandOwnerModel>(UserDbErrorReason.NotFound.ToString()));

        var result = await runTest();

        Assert.True(result.IsFailure);

        MockFlaRepository.Verify(x => x.GetAsync(fla.Id, It.IsAny<CancellationToken>()), Times.Once);
        MockWoodlandOwnerService.Verify(x => x.RetrieveWoodlandOwnerByIdAsync(fla.WoodlandOwnerId, It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()), Times.Once);
        MockRetrieveUserAccountsService.VerifyNoOtherCalls();
        assertInternalUserAccountService?.Invoke();
        MockInternalUserAccountService.VerifyNoOtherCalls();
        MockAuditService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(y => y.EventName == AuditEvents.AssignFellingLicenceApplicationToApplicantFailure), It.IsAny<CancellationToken>()), Times.Once);

        return result;
    }

    protected async Task<Result<T>> RetrieveFlaSummaryShouldReturnFailureWhenExternalApplicantAccountForAssigneeOnFlaCouldNotBeFound<T>(
            Func<Task<Result<T>>> runTest,
            FellingLicenceApplication fla,
            WoodlandOwnerModel woodlandOwner,
            AssigneeHistory assigneeHistory,
            UserAccount authorAccount,
            Action? assertInternalUserAccountService = null) where T : FellingLicenceApplicationPageViewModel
    {
        assigneeHistory.Role = AssignedUserRole.Author;
        fla.AssigneeHistories = new List<AssigneeHistory> { assigneeHistory };

        MockFlaRepository.Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.From(fla));

        MockWoodlandOwnerService.Setup(x => x.RetrieveWoodlandOwnerByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(woodlandOwner));

        MockRetrieveUserAccountsService.Setup(x => x.RetrieveUserAccountEntityByIdAsync(It.Is<Guid>(u => u == fla.CreatedById), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(authorAccount));

        MockRetrieveUserAccountsService.Setup(x => x.RetrieveUserAccountEntityByIdAsync(It.Is<Guid>(u => u == assigneeHistory.AssignedUserId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<UserAccount>(UserDbErrorReason.NotFound.ToString()));

        var result = await runTest();

        Assert.True(result.IsFailure);

        MockFlaRepository.Verify(x => x.GetAsync(fla.Id, It.IsAny<CancellationToken>()), Times.Once);
        MockWoodlandOwnerService.Verify(x => x.RetrieveWoodlandOwnerByIdAsync(fla.WoodlandOwnerId, It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()), Times.Once);
        assertInternalUserAccountService?.Invoke();
        MockInternalUserAccountService.VerifyNoOtherCalls();
        MockAuditService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(y => y.EventName == AuditEvents.AssignFellingLicenceApplicationToApplicantFailure), It.IsAny<CancellationToken>()), Times.Once);

        return result;
    }

    protected async Task<Result<T>> RetrieveFlaSummaryShouldReturnFailureWhenInternalUserAccountForAssigneeOnFlaCouldNotBeFound<T>(
            Func<Task<Result<T>>> runTest,
            FellingLicenceApplication fla,
            WoodlandOwnerModel woodlandOwner,
            AssigneeHistory assigneeHistory,
            UserAccount authorAccount,
            Action? assertInternalUserAccountService = null) where T : FellingLicenceApplicationPageViewModel
    {
        assigneeHistory.Role = AssignedUserRole.AdminOfficer;
        fla.AssigneeHistories = new List<AssigneeHistory> { assigneeHistory };

        MockFlaRepository.Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.From(fla));

        MockWoodlandOwnerService.Setup(x => x.RetrieveWoodlandOwnerByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(woodlandOwner));

        MockRetrieveUserAccountsService.Setup(x => x.RetrieveUserAccountEntityByIdAsync(It.Is<Guid>(u => u == fla.CreatedById), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(authorAccount));

        MockInternalUserAccountService.Setup(x => x.GetUserAccountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<Flo.Services.InternalUsers.Entities.UserAccount.UserAccount>.None);

        var result = await runTest();

        Assert.True(result.IsFailure);

        MockFlaRepository.Verify(x => x.GetAsync(fla.Id, It.IsAny<CancellationToken>()), Times.Once);
        MockWoodlandOwnerService.Verify(x => x.RetrieveWoodlandOwnerByIdAsync(fla.WoodlandOwnerId, It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()), Times.Once);
        MockInternalUserAccountService.Verify(x => x.GetUserAccountAsync(assigneeHistory.AssignedUserId, It.IsAny<CancellationToken>()), Times.Once);
        assertInternalUserAccountService?.Invoke();
        MockAuditService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(y => y.EventName == AuditEvents.AssignFellingLicenceApplicationToApplicantFailure), It.IsAny<CancellationToken>()), Times.Once);

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
        bool authorIsFcAgent,
        List<UserAccountModel> externalApplicantAccountModels) where T : FellingLicenceApplicationPageViewModel
    {
        externalAssigneeHistory.Role = AssignedUserRole.Author;
        externalApplicantAccount.AccountType = AccountTypeExternal.WoodlandOwner;
        internalAssigneeHistory.Role = AssignedUserRole.AdminOfficer;
        internalUserAccount.AccountType = AccountTypeInternal.AdminOfficer;
        fla.AssigneeHistories = new List<AssigneeHistory> { externalAssigneeHistory, internalAssigneeHistory };

        MockFlaRepository.Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.From(fla));

        MockWoodlandOwnerService.Setup(x => x.RetrieveWoodlandOwnerByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(woodlandOwner));

        MockRetrieveUserAccountsService.Setup(x => x.RetrieveUserAccountEntityByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(externalApplicantAccount));

        MockInternalUserAccountService
            .Setup(x => x.GetUserAccountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<Flo.Services.InternalUsers.Entities.UserAccount.UserAccount>.From(internalUserAccount));

        MockRetrieveUserAccountsService
            .Setup(x => x.IsUserAccountLinkedToFcAgencyAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(authorIsFcAgent));

        if (authorIsFcAgent)
        {
            MockRetrieveUserAccountsService
                .Setup(x => x.RetrieveUserAccountsForFcAgencyAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Success(externalApplicantAccountModels));
        }
        else
        {
            MockRetrieveUserAccountsService
                .Setup(x => x.RetrieveUserAccountsForWoodlandOwnerAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Success(externalApplicantAccountModels));
        }

        var result = await runTest();

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value.FellingLicenceApplicationSummary);

        MockFlaRepository.Verify(x => x.GetAsync(fla.Id, It.IsAny<CancellationToken>()), Times.Once);
        MockWoodlandOwnerService.Verify(x => x.RetrieveWoodlandOwnerByIdAsync(fla.WoodlandOwnerId, It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()), Times.Once);
        MockInternalUserAccountService.Verify(x => x.GetUserAccountAsync(internalAssigneeHistory.AssignedUserId, It.IsAny<CancellationToken>()), Times.Once);
        MockAuditService.VerifyNoOtherCalls();

        Assert.Equal(fla.Id, result.Value.FellingLicenceApplicationSummary.Id);
        Assert.Equal(fla.ApplicationReference, result.Value.FellingLicenceApplicationSummary.ApplicationReference);
        var expectedStatus = fla.StatusHistories.Any()
            ? fla.StatusHistories.OrderByDescending(x => x.Created).First().Status
            : FellingLicenceStatus.Draft;
        Assert.Equal(expectedStatus, result.Value.FellingLicenceApplicationSummary.Status);
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
            x.Role == AssignedUserRole.AdminOfficer
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
        );

        MockRetrieveUserAccountsService.Verify(x => x.IsUserAccountLinkedToFcAgencyAsync(fla.CreatedById, It.IsAny<CancellationToken>()), Times.Once);

        if (authorIsFcAgent)
        {
            MockRetrieveUserAccountsService.Verify(x => x.RetrieveUserAccountsForFcAgencyAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
        else
        {
            MockRetrieveUserAccountsService.Verify(x => x.RetrieveUserAccountsForWoodlandOwnerAsync(fla.WoodlandOwnerId, It.IsAny<CancellationToken>()), Times.Once);
        }

        return result;
    }

    protected async Task<Result<T>> RetrieveFlaSummaryShouldReturnFailureWhenStatusIsNotValid<T>(
        Func<Task<Result<T>>> runTest,
        FellingLicenceApplication fla,
        WoodlandOwnerModel woodlandOwner,
        UserAccount externalApplicantAccount,
        Flo.Services.InternalUsers.Entities.UserAccount.UserAccount internalUserAccount,
        bool authorIsFcAgent,
        List<UserAccountModel> externalApplicantAccountModels) where T : FellingLicenceApplicationPageViewModel
    {
        MockFlaRepository.Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.From(fla));

        MockWoodlandOwnerService.Setup(x => x.RetrieveWoodlandOwnerByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(woodlandOwner));

        MockRetrieveUserAccountsService.Setup(x => x.RetrieveUserAccountEntityByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(externalApplicantAccount));

        MockInternalUserAccountService
            .Setup(x => x.GetUserAccountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<Flo.Services.InternalUsers.Entities.UserAccount.UserAccount>.From(internalUserAccount));

        MockRetrieveUserAccountsService
            .Setup(x => x.IsUserAccountLinkedToFcAgencyAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(authorIsFcAgent));

        if (!authorIsFcAgent)
        {
            MockRetrieveUserAccountsService
                .Setup(x => x.RetrieveUserAccountsForWoodlandOwnerAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Success(externalApplicantAccountModels));
        }
        else
        {
            MockRetrieveUserAccountsService
                .Setup(x => x.RetrieveUserAccountsForFcAgencyAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Success(externalApplicantAccountModels));
        }

        var result = await runTest();

        Assert.True(result.IsFailure);

        MockFlaRepository.Verify(x => x.GetAsync(fla.Id, It.IsAny<CancellationToken>()), Times.Once);
        MockWoodlandOwnerService.Verify(x => x.RetrieveWoodlandOwnerByIdAsync(fla.WoodlandOwnerId, It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()), Times.Once);
        MockAuditService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(y => y.EventName == AuditEvents.AssignFellingLicenceApplicationToApplicantFailure), It.IsAny<CancellationToken>()), Times.Once);

        if (!authorIsFcAgent)
        {
            MockRetrieveUserAccountsService.Verify(x => x.RetrieveUserAccountsForWoodlandOwnerAsync(fla.WoodlandOwnerId, It.IsAny<CancellationToken>()), Times.Once);
        }
        else
        {
            MockRetrieveUserAccountsService.Verify(x => x.RetrieveUserAccountsForFcAgencyAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        return result;
    }
}