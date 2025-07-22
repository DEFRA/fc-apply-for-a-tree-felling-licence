using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.Extensions;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.InternalUsers.Entities.UserAccount;
using Forestry.Flo.Services.InternalUsers.Services;
using Forestry.Flo.Services.Notifications.Entities;
using Forestry.Flo.Services.Notifications.Models;
using Forestry.Flo.Tests.Common;
using Moq;
using System.Text.Json;

namespace Forestry.Flo.Internal.Web.Tests.Services.AssignToUserUseCaseTests;

public class AssignToUserAsyncTests : AssignToUserUseCaseTestsBase
{
    private readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [Theory, AutoMoqData]
    public async Task WhenCannotRetrieveAssignToUserDetails(
        Guid applicationId,
        Guid assignToUserId,
        AssignedUserRole selectedRole,
        string fcAreaCode,
        string linkToApplication,
        string caseNote,
        string adminHubName)
    {
        var sut = CreateSut();

        MockInternalUserAccountService
            .Setup(x => x.GetUserAccountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<UserAccount>.None);

        var result = await sut.AssignToUserAsync(applicationId, assignToUserId, selectedRole, fcAreaCode, _testUser,
            linkToApplication, caseNote, adminHubName, CancellationToken.None);

        Assert.True(result.IsFailure);

        MockInternalUserAccountService.Verify(x => x.GetUserAccountAsync(assignToUserId, It.IsAny<CancellationToken>()), Times.Once);
        MockInternalUserAccountService.VerifyNoOtherCalls();

        MockGetFellingLicenceApplication.VerifyNoOtherCalls();
        MockExternalUserAccountService.VerifyNoOtherCalls();
        MockUpdateFellingLicenceApplication.VerifyNoOtherCalls();
        MockSendNotifications.VerifyNoOtherCalls();

        MockAuditService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(y =>
                y.EventName == AuditEvents.AssignFellingLicenceApplicationToStaffMemberFailure
                && y.SourceEntityId == applicationId
                && y.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && y.UserId == _testUser.UserAccountId
                && JsonSerializer.Serialize(y.AuditData, _options) ==
                JsonSerializer.Serialize(
                    new
                    {
                        AssignedStaffMemberId = assignToUserId,
                        AssignedUserRole = selectedRole,
                        Error = "No user account found for given assign to id"
                    }, _options)),
            It.IsAny<CancellationToken>()), Times.Once);
        MockAuditService.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenUserDoesNotHavePermissionsToBeFieldManager(
        Guid applicationId,
        Guid assignToUserId,
        UserAccount assignToUserAccount,
        string fcAreaCode,
        string linkToApplication,
        string caseNote,
        string adminHubName)
    {
        var sut = CreateSut();
        var selectedRole = AssignedUserRole.FieldManager;

        assignToUserAccount.CanApproveApplications = false;
        TestUtils.SetProtectedProperty(assignToUserAccount, "Id", assignToUserId);

        MockInternalUserAccountService
            .Setup(x => x.GetUserAccountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<UserAccount>.From(assignToUserAccount));

        var result = await sut.AssignToUserAsync(applicationId, assignToUserId, selectedRole, fcAreaCode, _testUser,
            linkToApplication, caseNote, adminHubName, CancellationToken.None);

        Assert.True(result.IsFailure);

        MockInternalUserAccountService.Verify(x => x.GetUserAccountAsync(assignToUserId, It.IsAny<CancellationToken>()), Times.Once);
        MockInternalUserAccountService.VerifyNoOtherCalls();

        MockGetFellingLicenceApplication.VerifyNoOtherCalls();
        MockExternalUserAccountService.VerifyNoOtherCalls();
        MockUpdateFellingLicenceApplication.VerifyNoOtherCalls();
        MockSendNotifications.VerifyNoOtherCalls();

        MockAuditService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(y =>
                y.EventName == AuditEvents.AssignFellingLicenceApplicationToStaffMemberFailure
                && y.SourceEntityId == applicationId
                && y.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && y.UserId == _testUser.UserAccountId
                && JsonSerializer.Serialize(y.AuditData, _options) ==
                JsonSerializer.Serialize(
                    new
                    {
                        AssignedStaffMemberId = assignToUserId,
                        AssignedUserRole = selectedRole,
                        Error = "Selected user cannot be assigned Field Manager role"
                    }, _options)),
            It.IsAny<CancellationToken>()), Times.Once);
        MockAuditService.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenCannotRetrieveApplicationStatusHistory(
        Guid applicationId,
        Guid assignToUserId,
        UserAccount assignToUserAccount,
        string fcAreaCode,
        string linkToApplication,
        string caseNote,
        string adminHubName,
        string error)
    {
        var sut = CreateSut();
        var selectedRole = AssignedUserRole.FieldManager;

        assignToUserAccount.CanApproveApplications = true;
        TestUtils.SetProtectedProperty(assignToUserAccount, "Id", assignToUserId);

        MockInternalUserAccountService
            .Setup(x => x.GetUserAccountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<UserAccount>.From(assignToUserAccount));
        MockGetFellingLicenceApplication
            .Setup(x => x.GetApplicationStatusHistory(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<List<StatusHistoryModel>>(error));

        var result = await sut.AssignToUserAsync(applicationId, assignToUserId, selectedRole, fcAreaCode, _testUser,
            linkToApplication, caseNote, adminHubName, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(error, result.Error);

        MockInternalUserAccountService.Verify(x => x.GetUserAccountAsync(assignToUserId, It.IsAny<CancellationToken>()), Times.Once);
        MockInternalUserAccountService.VerifyNoOtherCalls();

        MockGetFellingLicenceApplication
            .Verify(x => x.GetApplicationStatusHistory(applicationId, It.Is<UserAccessModel>(u => u.IsFcUser), It.IsAny<CancellationToken>()),
                Times.Once);
        MockGetFellingLicenceApplication.VerifyNoOtherCalls();
        MockExternalUserAccountService.VerifyNoOtherCalls();
        MockUpdateFellingLicenceApplication.VerifyNoOtherCalls();
        MockSendNotifications.VerifyNoOtherCalls();

        MockAuditService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(y =>
                y.EventName == AuditEvents.AssignFellingLicenceApplicationToStaffMemberFailure
                && y.SourceEntityId == applicationId
                && y.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && y.UserId == _testUser.UserAccountId
                && JsonSerializer.Serialize(y.AuditData, _options) ==
                JsonSerializer.Serialize(
                    new
                    {
                        AssignedStaffMemberId = assignToUserId,
                        AssignedUserRole = selectedRole,
                        Error = error
                    }, _options)),
            It.IsAny<CancellationToken>()), Times.Once);
        MockAuditService.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenCannotRetrieveApplicantAccountThatSubmittedApplication(
        Guid applicationId,
        Guid assignToUserId,
        Guid submittedById,
        UserAccount assignToUserAccount,
        string fcAreaCode,
        string linkToApplication,
        string caseNote,
        string adminHubName)
    {
        var sut = CreateSut();
        var selectedRole = AssignedUserRole.FieldManager;

        assignToUserAccount.CanApproveApplications = true;
        TestUtils.SetProtectedProperty(assignToUserAccount, "Id", assignToUserId);

        var submittedEntry = new StatusHistoryModel(DateTime.Today, submittedById, FellingLicenceStatus.Submitted);

        MockInternalUserAccountService
            .Setup(x => x.GetUserAccountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<UserAccount>.From(assignToUserAccount));
        MockGetFellingLicenceApplication.Setup(x =>
                x.GetApplicationStatusHistory(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new List<StatusHistoryModel> { submittedEntry }));
        MockExternalUserAccountService
            .Setup(x => x.RetrieveUserAccountEntityByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<Flo.Services.Applicants.Entities.UserAccount.UserAccount>(UserDbErrorReason.NotFound.ToString()));

        var result = await sut.AssignToUserAsync(applicationId, assignToUserId, selectedRole, fcAreaCode, _testUser,
            linkToApplication, caseNote, adminHubName, CancellationToken.None);

        Assert.True(result.IsFailure);

        MockInternalUserAccountService.Verify(x => x.GetUserAccountAsync(assignToUserId, It.IsAny<CancellationToken>()), Times.Once);
        MockInternalUserAccountService.VerifyNoOtherCalls();

        MockGetFellingLicenceApplication
            .Verify(x => x.GetApplicationStatusHistory(applicationId, It.Is<UserAccessModel>(u => u.IsFcUser), It.IsAny<CancellationToken>()),
                Times.Once);
        MockGetFellingLicenceApplication.VerifyNoOtherCalls();

        MockExternalUserAccountService.Verify(x => x.RetrieveUserAccountEntityByIdAsync(submittedById, It.IsAny<CancellationToken>()), Times.Once);
        MockExternalUserAccountService.VerifyNoOtherCalls();
        MockUpdateFellingLicenceApplication.VerifyNoOtherCalls();
        MockSendNotifications.VerifyNoOtherCalls();

        MockAuditService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(y =>
                y.EventName == AuditEvents.AssignFellingLicenceApplicationToStaffMemberFailure
                && y.SourceEntityId == applicationId
                && y.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && y.UserId == _testUser.UserAccountId
                && JsonSerializer.Serialize(y.AuditData, _options) ==
                JsonSerializer.Serialize(
                    new
                    {
                        AssignedStaffMemberId = assignToUserId,
                        AssignedUserRole = selectedRole,
                        Error = "Could not locate external user with the given id"
                    }, _options)),
            It.IsAny<CancellationToken>()), Times.Once);
        MockAuditService.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenUserCannotBeAssignedFieldManagerAsTheySubmittedTheApplication(
        Guid applicationId,
        Guid assignToUserId,
        Guid submittedById,
        string emailAddress,
        UserAccount assignToUserAccount,
        Flo.Services.Applicants.Entities.UserAccount.UserAccount submittedByAccount,
        string fcAreaCode,
        string linkToApplication,
        string caseNote,
        string adminHubName)
    {
        var sut = CreateSut();
        var selectedRole = AssignedUserRole.FieldManager;

        assignToUserAccount.CanApproveApplications = true;
        assignToUserAccount.Email = emailAddress;
        TestUtils.SetProtectedProperty(assignToUserAccount, "Id", assignToUserId);

        submittedByAccount.Email = emailAddress;

        var submittedEntry = new StatusHistoryModel(DateTime.Today, submittedById, FellingLicenceStatus.Submitted);

        MockInternalUserAccountService
            .Setup(x => x.GetUserAccountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<UserAccount>.From(assignToUserAccount));
        MockGetFellingLicenceApplication.Setup(x =>
                x.GetApplicationStatusHistory(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new List<StatusHistoryModel> { submittedEntry }));
        MockExternalUserAccountService
            .Setup(x => x.RetrieveUserAccountEntityByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(submittedByAccount));

        var result = await sut.AssignToUserAsync(applicationId, assignToUserId, selectedRole, fcAreaCode, _testUser,
            linkToApplication, caseNote, adminHubName, CancellationToken.None);

        Assert.True(result.IsFailure);

        MockInternalUserAccountService.Verify(x => x.GetUserAccountAsync(assignToUserId, It.IsAny<CancellationToken>()), Times.Once);
        MockInternalUserAccountService.VerifyNoOtherCalls();

        MockGetFellingLicenceApplication
            .Verify(x => x.GetApplicationStatusHistory(applicationId, It.Is<UserAccessModel>(u => u.IsFcUser), It.IsAny<CancellationToken>()),
                Times.Once);
        MockGetFellingLicenceApplication.VerifyNoOtherCalls();

        MockExternalUserAccountService.Verify(x => x.RetrieveUserAccountEntityByIdAsync(submittedById, It.IsAny<CancellationToken>()), Times.Once);
        MockExternalUserAccountService.VerifyNoOtherCalls();
        MockUpdateFellingLicenceApplication.VerifyNoOtherCalls();
        MockSendNotifications.VerifyNoOtherCalls();

        MockAuditService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(y =>
                y.EventName == AuditEvents.AssignFellingLicenceApplicationToStaffMemberFailure
                && y.SourceEntityId == applicationId
                && y.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && y.UserId == _testUser.UserAccountId
                && JsonSerializer.Serialize(y.AuditData, _options) ==
                JsonSerializer.Serialize(
                    new
                    {
                        AssignedStaffMemberId = assignToUserId,
                        AssignedUserRole = selectedRole,
                        Error = "Cannot assign the same user that submitted the application as Field Manager"
                    }, _options)),
            It.IsAny<CancellationToken>()), Times.Once);
        MockAuditService.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenPerformingTheAssignmentFails(
        Guid applicationId,
        Guid assignToUserId,
        Guid submittedById,
        UserAccount assignToUserAccount,
        Flo.Services.Applicants.Entities.UserAccount.UserAccount submittedByAccount,
        string fcAreaCode,
        string linkToApplication,
        string caseNote,
        string adminHubName,
        string error)
    {
        var sut = CreateSut();
        var selectedRole = AssignedUserRole.FieldManager;

        assignToUserAccount.CanApproveApplications = true;
        TestUtils.SetProtectedProperty(assignToUserAccount, "Id", assignToUserId);

        var submittedEntry = new StatusHistoryModel(DateTime.Today, submittedById, FellingLicenceStatus.Submitted);

        MockInternalUserAccountService
            .Setup(x => x.GetUserAccountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<UserAccount>.From(assignToUserAccount));
        MockGetFellingLicenceApplication.Setup(x =>
                x.GetApplicationStatusHistory(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new List<StatusHistoryModel> { submittedEntry }));
        MockExternalUserAccountService
            .Setup(x => x.RetrieveUserAccountEntityByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(submittedByAccount));
        MockUpdateFellingLicenceApplication
            .Setup(x => x.AssignToInternalUserAsync(It.IsAny<AssignToUserRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<AssignToUserResponse>(error));

        var result = await sut.AssignToUserAsync(applicationId, assignToUserId, selectedRole, fcAreaCode, _testUser,
            linkToApplication, caseNote, adminHubName, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(error, result.Error);

        MockInternalUserAccountService.Verify(x => x.GetUserAccountAsync(assignToUserId, It.IsAny<CancellationToken>()), Times.Once);
        MockInternalUserAccountService.VerifyNoOtherCalls();

        MockGetFellingLicenceApplication
            .Verify(x => x.GetApplicationStatusHistory(applicationId, It.Is<UserAccessModel>(u => u.IsFcUser), It.IsAny<CancellationToken>()),
                Times.Once);
        MockGetFellingLicenceApplication.VerifyNoOtherCalls();

        MockExternalUserAccountService.Verify(x => x.RetrieveUserAccountEntityByIdAsync(submittedById, It.IsAny<CancellationToken>()), Times.Once);
        MockExternalUserAccountService.VerifyNoOtherCalls();

        MockUpdateFellingLicenceApplication
            .Verify(x => x.AssignToInternalUserAsync(It.Is<AssignToUserRequest>(r =>
                r.ApplicationId == applicationId
                && r.AssignToUserId == assignToUserId
                && r.AssignedRole == selectedRole
                && r.CaseNoteContent == caseNote
                && r.FcAreaCostCode == fcAreaCode
                && r.PerformingUserId == _testUser.UserAccountId), It.IsAny<CancellationToken>()), Times.Once);
        MockUpdateFellingLicenceApplication.VerifyNoOtherCalls();
        MockSendNotifications.VerifyNoOtherCalls();

        MockAuditService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(y =>
                y.EventName == AuditEvents.AssignFellingLicenceApplicationToStaffMemberFailure
                && y.SourceEntityId == applicationId
                && y.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && y.UserId == _testUser.UserAccountId
                && JsonSerializer.Serialize(y.AuditData, _options) ==
                JsonSerializer.Serialize(
                    new
                    {
                        AssignedStaffMemberId = assignToUserId,
                        AssignedUserRole = selectedRole,
                        Error = error
                    }, _options)),
            It.IsAny<CancellationToken>()), Times.Once);
        MockAuditService.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenApplicationIsAlreadyAssignedToThisUser(
        Guid applicationId,
        Guid assignToUserId,
        Guid submittedById,
        UserAccount assignToUserAccount,
        Flo.Services.Applicants.Entities.UserAccount.UserAccount submittedByAccount,
        string fcAreaCode,
        string linkToApplication,
        string caseNote,
        string adminHubName,
        string originalReference,
        string updatedReference)
    {
        var sut = CreateSut();
        var selectedRole = AssignedUserRole.FieldManager;

        assignToUserAccount.CanApproveApplications = true;
        TestUtils.SetProtectedProperty(assignToUserAccount, "Id", assignToUserId);

        var submittedEntry = new StatusHistoryModel(DateTime.Today, submittedById, FellingLicenceStatus.Submitted);

        var assignToUserResponse =
            new AssignToUserResponse(updatedReference, originalReference, true, Maybe<Guid>.None);

        MockInternalUserAccountService
            .Setup(x => x.GetUserAccountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<UserAccount>.From(assignToUserAccount));
        MockGetFellingLicenceApplication.Setup(x =>
                x.GetApplicationStatusHistory(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new List<StatusHistoryModel> { submittedEntry }));
        MockExternalUserAccountService
            .Setup(x => x.RetrieveUserAccountEntityByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(submittedByAccount));
        MockUpdateFellingLicenceApplication
            .Setup(x => x.AssignToInternalUserAsync(It.IsAny<AssignToUserRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(assignToUserResponse));

        var result = await sut.AssignToUserAsync(applicationId, assignToUserId, selectedRole, fcAreaCode, _testUser,
            linkToApplication, caseNote, adminHubName, CancellationToken.None);

        Assert.True(result.IsSuccess);

        MockInternalUserAccountService.Verify(x => x.GetUserAccountAsync(assignToUserId, It.IsAny<CancellationToken>()), Times.Once);
        MockInternalUserAccountService.VerifyNoOtherCalls();

        MockGetFellingLicenceApplication
            .Verify(x => x.GetApplicationStatusHistory(applicationId, It.Is<UserAccessModel>(u => u.IsFcUser), It.IsAny<CancellationToken>()),
                Times.Once);
        MockGetFellingLicenceApplication.VerifyNoOtherCalls();

        MockExternalUserAccountService.Verify(x => x.RetrieveUserAccountEntityByIdAsync(submittedById, It.IsAny<CancellationToken>()), Times.Once);
        MockExternalUserAccountService.VerifyNoOtherCalls();

        MockUpdateFellingLicenceApplication
            .Verify(x => x.AssignToInternalUserAsync(It.Is<AssignToUserRequest>(r =>
                r.ApplicationId == applicationId
                && r.AssignToUserId == assignToUserId
                && r.AssignedRole == selectedRole
                && r.CaseNoteContent == caseNote
                && r.FcAreaCostCode == fcAreaCode
                && r.PerformingUserId == _testUser.UserAccountId), It.IsAny<CancellationToken>()), Times.Once);
        MockUpdateFellingLicenceApplication.VerifyNoOtherCalls();
        MockSendNotifications.VerifyNoOtherCalls();

        MockAuditService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(y =>
                y.EventName == AuditEvents.AssignFellingLicenceApplicationToStaffMember
                && y.SourceEntityId == applicationId
                && y.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && y.UserId == _testUser.UserAccountId
                && JsonSerializer.Serialize(y.AuditData, _options) ==
                JsonSerializer.Serialize(
                    new
                    {
                        AssignedStaffMemberId = assignToUserId,
                        AssignedUserRole = selectedRole,
                        UnassignedStaffMemberId = (Guid?)null,
                        NotificationSent = (bool?)null,
                        AreaCodeRequiredUpdate = true,
                        OriginalApplicationReference = originalReference,
                        AreaCode = fcAreaCode,
                        CurrentApplicationReference = updatedReference
                    }, _options)),
            It.IsAny<CancellationToken>()), Times.Once);
        MockAuditService.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenNotificationFailsToSend(
        Guid applicationId,
        Guid assignToUserId,
        Guid submittedById,
        UserAccount assignToUserAccount,
        Flo.Services.Applicants.Entities.UserAccount.UserAccount submittedByAccount,
        string fcAreaCode,
        string linkToApplication,
        string caseNote,
        string adminHubName,
        string originalReference,
        string updatedReference,
        Guid oldAssignedUserId)
    {
        var sut = CreateSut();
        var selectedRole = AssignedUserRole.FieldManager;

        assignToUserAccount.CanApproveApplications = true;
        TestUtils.SetProtectedProperty(assignToUserAccount, "Id", assignToUserId);

        var submittedEntry = new StatusHistoryModel(DateTime.Today, submittedById, FellingLicenceStatus.Submitted);

        var assignToUserResponse =
            new AssignToUserResponse(updatedReference, originalReference, false, Maybe<Guid>.From(oldAssignedUserId));

        MockInternalUserAccountService
            .Setup(x => x.GetUserAccountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<UserAccount>.From(assignToUserAccount));
        MockGetFellingLicenceApplication.Setup(x =>
                x.GetApplicationStatusHistory(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new List<StatusHistoryModel> { submittedEntry }));
        MockExternalUserAccountService
            .Setup(x => x.RetrieveUserAccountEntityByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(submittedByAccount));
        MockUpdateFellingLicenceApplication
            .Setup(x => x.AssignToInternalUserAsync(It.IsAny<AssignToUserRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(assignToUserResponse));
        MockSendNotifications
            .Setup(x => x.SendNotificationAsync(It.IsAny<UserAssignedToApplicationDataModel>(),
                It.IsAny<NotificationType>(), It.IsAny<NotificationRecipient>(), It.IsAny<NotificationRecipient[]?>(),
                It.IsAny<NotificationAttachment[]?>(), It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("error"));

        var result = await sut.AssignToUserAsync(applicationId, assignToUserId, selectedRole, fcAreaCode, _testUser,
            linkToApplication, caseNote, adminHubName, CancellationToken.None);

        Assert.True(result.IsSuccess);

        MockInternalUserAccountService.Verify(x => x.GetUserAccountAsync(assignToUserId, It.IsAny<CancellationToken>()), Times.Once);
        MockInternalUserAccountService.VerifyNoOtherCalls();

        MockGetFellingLicenceApplication
            .Verify(x => x.GetApplicationStatusHistory(applicationId, It.Is<UserAccessModel>(u => u.IsFcUser), It.IsAny<CancellationToken>()),
                Times.Once);
        MockGetFellingLicenceApplication.VerifyNoOtherCalls();

        MockExternalUserAccountService.Verify(x => x.RetrieveUserAccountEntityByIdAsync(submittedById, It.IsAny<CancellationToken>()), Times.Once);
        MockExternalUserAccountService.VerifyNoOtherCalls();

        MockUpdateFellingLicenceApplication
            .Verify(x => x.AssignToInternalUserAsync(It.Is<AssignToUserRequest>(r =>
                r.ApplicationId == applicationId
                && r.AssignToUserId == assignToUserId
                && r.AssignedRole == selectedRole
                && r.CaseNoteContent == caseNote
                && r.FcAreaCostCode == fcAreaCode
                && r.PerformingUserId == _testUser.UserAccountId), It.IsAny<CancellationToken>()), Times.Once);
        MockUpdateFellingLicenceApplication.VerifyNoOtherCalls();

        MockSendNotifications
            .Verify(x => x.SendNotificationAsync(It.Is<UserAssignedToApplicationDataModel>(m => m.AssignedRole == selectedRole.GetDisplayName() && m.ApplicationReference == updatedReference && m.Name == assignToUserAccount.FullName(false) && m.ViewApplicationURL == linkToApplication && m.AdminHubFooter == AdminHubAddress),
                NotificationType.UserAssignedToApplication, It.Is<NotificationRecipient>(r => r.Address == assignToUserAccount.Email && r.Name == assignToUserAccount.FullName(false)),
                null, null, _testUser.FullName, It.IsAny<CancellationToken>()), Times.Once);
        MockSendNotifications.VerifyNoOtherCalls();

        MockAuditService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(y =>
                y.EventName == AuditEvents.AssignFellingLicenceApplicationToStaffMember
                && y.SourceEntityId == applicationId
                && y.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && y.UserId == _testUser.UserAccountId
                && JsonSerializer.Serialize(y.AuditData, _options) ==
                JsonSerializer.Serialize(
                    new
                    {
                        AssignedStaffMemberId = assignToUserId,
                        AssignedUserRole = selectedRole,
                        UnassignedStaffMemberId = oldAssignedUserId,
                        NotificationSent = false,
                        AreaCodeRequiredUpdate = true,
                        OriginalApplicationReference = originalReference,
                        AreaCode = fcAreaCode,
                        CurrentApplicationReference = updatedReference
                    }, _options)),
            It.IsAny<CancellationToken>()), Times.Once);
        MockAuditService.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenNotificationSends(
        Guid applicationId,
        Guid assignToUserId,
        Guid submittedById,
        UserAccount assignToUserAccount,
        Flo.Services.Applicants.Entities.UserAccount.UserAccount submittedByAccount,
        string fcAreaCode,
        string linkToApplication,
        string caseNote,
        string adminHubName,
        string originalReference,
        string updatedReference,
        Guid oldAssignedUserId)
    {
        var sut = CreateSut();
        var selectedRole = AssignedUserRole.FieldManager;

        assignToUserAccount.CanApproveApplications = true;
        TestUtils.SetProtectedProperty(assignToUserAccount, "Id", assignToUserId);

        var submittedEntry = new StatusHistoryModel(DateTime.Today, submittedById, FellingLicenceStatus.Submitted);

        var assignToUserResponse =
            new AssignToUserResponse(updatedReference, originalReference, false, Maybe<Guid>.From(oldAssignedUserId));

        MockInternalUserAccountService
            .Setup(x => x.GetUserAccountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<UserAccount>.From(assignToUserAccount));
        MockGetFellingLicenceApplication.Setup(x =>
                x.GetApplicationStatusHistory(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new List<StatusHistoryModel> { submittedEntry }));
        MockExternalUserAccountService
            .Setup(x => x.RetrieveUserAccountEntityByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(submittedByAccount));
        MockUpdateFellingLicenceApplication
            .Setup(x => x.AssignToInternalUserAsync(It.IsAny<AssignToUserRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(assignToUserResponse));
        MockSendNotifications
            .Setup(x => x.SendNotificationAsync(It.IsAny<UserAssignedToApplicationDataModel>(),
                It.IsAny<NotificationType>(), It.IsAny<NotificationRecipient>(), It.IsAny<NotificationRecipient[]?>(),
                It.IsAny<NotificationAttachment[]?>(), It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var result = await sut.AssignToUserAsync(applicationId, assignToUserId, selectedRole, fcAreaCode, _testUser,
            linkToApplication, caseNote, adminHubName, CancellationToken.None);

        Assert.True(result.IsSuccess);

        MockInternalUserAccountService.Verify(x => x.GetUserAccountAsync(assignToUserId, It.IsAny<CancellationToken>()), Times.Once);
        MockInternalUserAccountService.VerifyNoOtherCalls();

        MockGetFellingLicenceApplication
            .Verify(x => x.GetApplicationStatusHistory(applicationId, It.Is<UserAccessModel>(u => u.IsFcUser), It.IsAny<CancellationToken>()),
                Times.Once);
        MockGetFellingLicenceApplication.VerifyNoOtherCalls();

        MockExternalUserAccountService.Verify(x => x.RetrieveUserAccountEntityByIdAsync(submittedById, It.IsAny<CancellationToken>()), Times.Once);
        MockExternalUserAccountService.VerifyNoOtherCalls();

        MockUpdateFellingLicenceApplication
            .Verify(x => x.AssignToInternalUserAsync(It.Is<AssignToUserRequest>(r =>
                r.ApplicationId == applicationId
                && r.AssignToUserId == assignToUserId
                && r.AssignedRole == selectedRole
                && r.CaseNoteContent == caseNote
                && r.FcAreaCostCode == fcAreaCode
                && r.PerformingUserId == _testUser.UserAccountId), It.IsAny<CancellationToken>()), Times.Once);
        MockUpdateFellingLicenceApplication.VerifyNoOtherCalls();

        MockSendNotifications
            .Verify(x => x.SendNotificationAsync(It.Is<UserAssignedToApplicationDataModel>(m => m.AssignedRole == selectedRole.GetDisplayName() && m.ApplicationReference == updatedReference && m.Name == assignToUserAccount.FullName(false) && m.ViewApplicationURL == linkToApplication && m.AdminHubFooter == AdminHubAddress),
                NotificationType.UserAssignedToApplication, It.Is<NotificationRecipient>(r => r.Address == assignToUserAccount.Email && r.Name == assignToUserAccount.FullName(false)),
                null, null, _testUser.FullName, It.IsAny<CancellationToken>()), Times.Once);
        MockSendNotifications.VerifyNoOtherCalls();

        MockAuditService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(y =>
                y.EventName == AuditEvents.AssignFellingLicenceApplicationToStaffMember
                && y.SourceEntityId == applicationId
                && y.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && y.UserId == _testUser.UserAccountId
                && JsonSerializer.Serialize(y.AuditData, _options) ==
                JsonSerializer.Serialize(
                    new
                    {
                        AssignedStaffMemberId = assignToUserId,
                        AssignedUserRole = selectedRole,
                        UnassignedStaffMemberId = oldAssignedUserId,
                        NotificationSent = true,
                        AreaCodeRequiredUpdate = true,
                        OriginalApplicationReference = originalReference,
                        AreaCode = fcAreaCode,
                        CurrentApplicationReference = updatedReference
                    }, _options)),
            It.IsAny<CancellationToken>()), Times.Once);
        MockAuditService.VerifyNoOtherCalls();
    }
}