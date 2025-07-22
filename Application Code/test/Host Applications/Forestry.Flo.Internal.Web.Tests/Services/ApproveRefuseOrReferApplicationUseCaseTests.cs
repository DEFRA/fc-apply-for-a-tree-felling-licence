using AutoFixture;
using CSharpFunctionalExtensions;
using FluentAssertions;
using Forestry.Flo.Internal.Web.Infrastructure;
using Forestry.Flo.Internal.Web.Services;
using Forestry.Flo.Internal.Web.Services.FellingLicenceApplication;
using Forestry.Flo.Services.Applicants.Models;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.Gis.Interfaces;
using Forestry.Flo.Services.Gis.Models.Internal.MapObjects;
using Forestry.Flo.Services.InternalUsers.Entities.UserAccount;
using Forestry.Flo.Services.InternalUsers.Services;
using Forestry.Flo.Services.Notifications.Entities;
using Forestry.Flo.Services.Notifications.Models;
using Forestry.Flo.Services.Notifications.Services;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using NodaTime;
using System.Text.Json;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;
using Forestry.Flo.Services.FellingLicenceApplications.Models;

namespace Forestry.Flo.Internal.Web.Tests.Services;

public class ApproveRefuseOrReferApplicationUseCaseTests
{
    private readonly Mock<IRetrieveUserAccountsService> _externalAccountServiceMock;
    private readonly Mock<IUserAccountService> _internalAccountServiceMock;
    private readonly Mock<ISendNotifications> _notificationsMock;
    private readonly Mock<IOptions<ExternalApplicantSiteOptions>> _externalSiteOptionsMock;
    private readonly Mock<IRetrieveWoodlandOwners> _woodlandOwnerServiceMock;
    private readonly Mock<IPublicRegister> _publicRegister;
    private readonly Mock<IClock> _clockMock;
    // ReSharper disable twice InconsistentNaming
    private readonly Mock<IGetFellingLicenceApplicationForInternalUsers> _getFLAMock;
    private readonly Mock<IUpdateFellingLicenceApplication> _updateFLAMock;
    private readonly Mock<IApproverReviewService> _approverReviewServiceMock;
    private readonly Mock<IAuditService<ApproveRefuseOrReferApplicationUseCase>> _auditMock;
    private readonly Mock<IForesterServices> _agolMock; 
    private readonly Mock<IGetConfiguredFcAreas> _getConfiguredFcAreasMock;

    private static readonly Fixture FixtureInstance = new();
    private const string ExternalUrl = "externalUrl";
    private readonly InternalUser _fieldManager;
    private const string AdminHubFooter = "admin hub address";

    public ApproveRefuseOrReferApplicationUseCaseTests()
    {
        _internalAccountServiceMock = new Mock<IUserAccountService>();
        _externalAccountServiceMock = new Mock<IRetrieveUserAccountsService>();
        _notificationsMock = new Mock<ISendNotifications>();
        _externalSiteOptionsMock = new Mock<IOptions<ExternalApplicantSiteOptions>>();
        _woodlandOwnerServiceMock = new Mock<IRetrieveWoodlandOwners>();
        _publicRegister = new Mock<IPublicRegister>();
        _clockMock = new Mock<IClock>();
        _getFLAMock = new Mock<IGetFellingLicenceApplicationForInternalUsers>();
        _updateFLAMock = new Mock<IUpdateFellingLicenceApplication>();
        _approverReviewServiceMock = new Mock<IApproverReviewService>();
        _auditMock = new Mock<IAuditService<ApproveRefuseOrReferApplicationUseCase>>();
        _agolMock = new Mock<IForesterServices>();
        _getConfiguredFcAreasMock = new Mock<IGetConfiguredFcAreas>();

        var fieldManager = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(
            FixtureInstance.Create<string>(),
            FixtureInstance.Create<string>(),
            FixtureInstance.Create<Guid>(),
            FixtureInstance.Create<string>(),
            AccountTypeInternal.FieldManager);

        _fieldManager = new InternalUser(fieldManager);
    }

    private readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [Theory, AutoMoqData]
    public async Task ApplicationSetToApproved_IsSuccess_And_AllSubProcessesSucceed(
        FellingLicenceApplication application,
        WoodlandOwnerModel woodlandOwnerModel,
        List<Flo.Services.InternalUsers.Models.UserAccountModel> fcStaff,
        UserAccountModel applicant, 
        string localAuthorityName,
        UserAccount approver)
    {
        var pr = FixtureInstance.Build<PublicRegister>()
            .With(x => x.WoodlandOfficerSetAsExemptFromConsultationPublicRegister, false)
            .Without(x => x.FellingLicenceApplication)
            .Create();

        application.PublicRegister = pr;

        var approverReview = FixtureInstance.Build<ApproverReviewModel>()
            .With(x => x.PublicRegisterPublish, true)
            .Create();

        application = PopulateStatusAndAssigneeHistory(application);
        var now = DateTime.UtcNow;
        _clockMock.Setup(s => s.GetCurrentInstant()).Returns(Instant.FromDateTimeUtc(now));

        var sut = CreateSut();

        _getFLAMock.Setup(s => s.GetApplicationByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(application);
        
        _notificationsMock.Setup(s => s.SendNotificationAsync(It.IsAny<object>(),
                It.IsAny<NotificationType>(), It.IsAny<NotificationRecipient>(), It.IsAny<NotificationRecipient[]?>(),
                It.IsAny<NotificationAttachment[]?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success);

        _woodlandOwnerServiceMock
            .Setup(s => s.RetrieveWoodlandOwnerByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(woodlandOwnerModel);

        _internalAccountServiceMock
            .Setup(s => s.RetrieveUserAccountsByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(fcStaff);

        _internalAccountServiceMock
            .Setup(s => s.GetUserAccountAsync(_fieldManager.UserAccountId.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(approver.AsMaybe());

        _externalAccountServiceMock
            .Setup(s => s.RetrieveUserAccountByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(applicant);

        _approverReviewServiceMock
            .Setup(r => r.GetApproverReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(approverReview);


        var layer = new Forestry.Flo.Services.Gis.Models.Esri.Responses.Layers.LocalAuthority {
            Name = localAuthorityName
        };
        _agolMock.Setup(x => x.GetLocalAuthorityAsync(It.IsAny<Point>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(layer));

        var result = await sut.ApproveOrRefuseOrReferApplicationAsync(
            _fieldManager,
            application.Id,
            FellingLicenceStatus.Approved,
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.SubProcessFailures.Should().BeEmpty();
        
        _publicRegister.Verify(x=>x.AddCaseToDecisionRegisterAsync(
            application.PublicRegister!.EsriId.Value, 
            application.ApplicationReference,
            FellingLicenceStatus.Approved.ToString(),
            now,
            It.IsAny<CancellationToken>()),
            Times.Once());

        _clockMock.Verify(x=>x.GetCurrentInstant(), Times.Once);

        _getFLAMock.Verify(v => v.GetApplicationByIdAsync(application.Id, CancellationToken.None), Times.Once);
        _woodlandOwnerServiceMock.Verify(v => v.RetrieveWoodlandOwnerByIdAsync(application.WoodlandOwnerId, CancellationToken.None), Times.Once);
        _internalAccountServiceMock.Verify(v => v.RetrieveUserAccountsByIdsAsync(It.Is<List<Guid>>(a =>
                a.TrueForAll(x => application.AssigneeHistories.Where(y => y.Role != AssignedUserRole.Applicant && y.Role != AssignedUserRole.Author && y.TimestampUnassigned == null)
                    .Select(y => y.AssignedUserId).Contains(x))),
            CancellationToken.None), Times.Exactly(2));
        _externalAccountServiceMock.Verify(v => v.RetrieveUserAccountByIdAsync(application.CreatedById, CancellationToken.None), Times.Once);
        _updateFLAMock.Verify(v => v.AddStatusHistoryAsync(It.IsAny<Guid>(), application.Id, FellingLicenceStatus.Approved, CancellationToken.None), Times.Once);
        _updateFLAMock.Verify(v => v.AddDecisionPublicRegisterDetailsAsync(application.Id, now, It.IsAny<DateTime>(), CancellationToken.None), Times.Once);

        _notificationsMock.Verify(v => v.SendNotificationAsync(It.Is<InformApplicantOfApplicationApprovalDataModel>(a => 
            a.ApplicationReference == application.ApplicationReference
            && a.PropertyName == application.SubmittedFlaPropertyDetail.Name
            && a.ApproverName == approver.FullName(false)
            && a.ViewApplicationURL == $"{ExternalUrl}FellingLicenceApplication/SupportingDocumentation/{application.Id}"
            && a.AdminHubFooter == AdminHubFooter
            && a.Name == applicant.FullName),
            NotificationType.InformApplicantOfApplicationApproval,
            It.Is<NotificationRecipient>(a => a.Address == applicant.Email && a.Name == applicant.FullName),
            It.Is<NotificationRecipient[]>(a => a.Length == fcStaff.Count + 1 
                                                && a.Any(x => x.Address == woodlandOwnerModel.ContactEmail && x.Name == woodlandOwnerModel.ContactName)
                                                && fcStaff.All(fc => a.Any(app => app.Address == fc.Email))),
            null, null,
            CancellationToken.None), Times.Once);

        foreach (var staff in fcStaff)
        {
            _notificationsMock
                .Verify(x => x.SendNotificationAsync(
                    It.Is<InformFcStaffOfApplicationAddedToPublicRegisterDataModel>(m =>
                        m.PropertyName == application.SubmittedFlaPropertyDetail.Name
                        && m.ApplicationReference == application.ApplicationReference
                        && m.PublishedDate == DateTimeDisplay.GetDateDisplayString(now)
                        && m.ExpiryDate == DateTimeDisplay.GetDateDisplayString(now.AddDays(28))
                        && m.AdminHubFooter == AdminHubFooter
                        && m.Name == staff.FullName),
                    NotificationType.InformFcStaffOfApplicationAddedToDecisionPublicRegister,
                    It.Is<NotificationRecipient>(
                        r => r.Name == staff.FullName && r.Address == staff.Email),
                    null,
                    null,
                    null,
                    It.IsAny<CancellationToken>()), Times.Once);
        }

        _notificationsMock.VerifyNoOtherCalls();

        _auditMock.Verify(v =>
            v.PublishAuditEventAsync(It.Is<AuditEvent>(
                    e => e.EventName == AuditEvents.ApplicationApproved
                         && JsonSerializer.Serialize(e.AuditData, _options) ==
                         JsonSerializer.Serialize(new
                         {
                             application.WoodlandOwnerId,
                             ApplicationAuthorId = application.CreatedById,
                             NotificationSent = true,
                             ApprovedByName = _fieldManager.FullName,
                             DecisionPublicRegisterOutcome = SendToDecisionPublicRegisterOutcome.Success
                         }, _options)),
                CancellationToken.None), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task ApplicationSetToRefused_IsSuccess__And_AllSubProcessesSucceed(
        FellingLicenceApplication application,
        WoodlandOwnerModel woodlandOwnerModel,
        List<Flo.Services.InternalUsers.Models.UserAccountModel> fcStaff,
        UserAccountModel applicant, 
        UserAccount approver,
        string localAuthorityName,
        ApproverReviewModel approverReview)
    {
        approverReview.PublicRegisterPublish = true;
        application = PopulateStatusAndAssigneeHistory(application);

        var sut = CreateSut();

        var now = DateTime.UtcNow;
        _clockMock.Setup(s => s.GetCurrentInstant()).Returns(Instant.FromDateTimeUtc(now));

        _getFLAMock.Setup(s => s.GetApplicationByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(application);

        _notificationsMock.Setup(s => s.SendNotificationAsync(It.IsAny<object>(),
                It.IsAny<NotificationType>(), It.IsAny<NotificationRecipient>(), It.IsAny<NotificationRecipient[]?>(),
                It.IsAny<NotificationAttachment[]?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success);

        _woodlandOwnerServiceMock
            .Setup(s => s.RetrieveWoodlandOwnerByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(woodlandOwnerModel);

        _internalAccountServiceMock
            .Setup(s => s.RetrieveUserAccountsByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(fcStaff);

        _internalAccountServiceMock
            .Setup(s => s.GetUserAccountAsync(_fieldManager.UserAccountId.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(approver.AsMaybe());

        _externalAccountServiceMock
            .Setup(s => s.RetrieveUserAccountByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(applicant);

        _approverReviewServiceMock
            .Setup(r => r.GetApproverReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(approverReview);

        var layer = new Forestry.Flo.Services.Gis.Models.Esri.Responses.Layers.LocalAuthority {
            Name = localAuthorityName
        };
        _agolMock.Setup(x => x.GetLocalAuthorityAsync(It.IsAny<Point>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(layer));

        var result = await sut.ApproveOrRefuseOrReferApplicationAsync(
            _fieldManager,
            application.Id,
            FellingLicenceStatus.Refused,
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.SubProcessFailures.Should().BeEmpty();

        _getFLAMock.Verify(v => v.GetApplicationByIdAsync(application.Id, CancellationToken.None), Times.Once);
        _woodlandOwnerServiceMock.Verify(v => v.RetrieveWoodlandOwnerByIdAsync(application.WoodlandOwnerId, CancellationToken.None), Times.Once);
        _internalAccountServiceMock.Verify(v => v.RetrieveUserAccountsByIdsAsync(It.Is<List<Guid>>(a =>
                a.TrueForAll(x => application.AssigneeHistories.Where(y => y.Role != AssignedUserRole.Applicant && y.Role != AssignedUserRole.Author && y.TimestampUnassigned == null)
                    .Select(y => y.AssignedUserId).Contains(x))),
            CancellationToken.None), Times.Exactly(2));
        _externalAccountServiceMock.Verify(v => v.RetrieveUserAccountByIdAsync(application.CreatedById, CancellationToken.None), Times.Once);
        _updateFLAMock.Verify(v => v.AddStatusHistoryAsync(It.IsAny<Guid>(), application.Id, FellingLicenceStatus.Refused, CancellationToken.None), Times.Once);
        _updateFLAMock.Verify(v => v.AddDecisionPublicRegisterDetailsAsync(application.Id, now,It.IsAny<DateTime>(), CancellationToken.None), Times.Once);

        _notificationsMock.Verify(v => v.SendNotificationAsync(It.Is<InformApplicantOfApplicationRefusalDataModel>(a =>
            a.ApplicationReference == application.ApplicationReference
            && a.PropertyName == application.SubmittedFlaPropertyDetail.Name
            && a.ApproverName == approver.FullName(false)
            && a.ApproverEmail == approver.Email
            && a.ViewApplicationURL == $"{ExternalUrl}FellingLicenceApplication/ApplicationTaskList/{application.Id}"
            && a.AdminHubFooter == AdminHubFooter
            && a.Name == applicant.FullName),
            NotificationType.InformApplicantOfApplicationRefusal,
            It.Is<NotificationRecipient>(a => a.Address == applicant.Email && a.Name == applicant.FullName),
            It.Is<NotificationRecipient[]>(a => a.Length == fcStaff.Count + 1 
                                                && a.Any(x => x.Address == woodlandOwnerModel.ContactEmail && x.Name == woodlandOwnerModel.ContactName)
                                                && fcStaff.All(fc => a.Any(app => app.Address == fc.Email))),
            null, null,
            CancellationToken.None), Times.Once);

        foreach (var staff in fcStaff)
        {
            _notificationsMock
                .Verify(x => x.SendNotificationAsync(
                    It.Is<InformFcStaffOfApplicationAddedToPublicRegisterDataModel>(m =>
                        m.PropertyName == application.SubmittedFlaPropertyDetail.Name
                        && m.ApplicationReference == application.ApplicationReference
                        && m.PublishedDate == DateTimeDisplay.GetDateDisplayString(now)
                        && m.ExpiryDate == DateTimeDisplay.GetDateDisplayString(now.AddDays(28))
                        && m.AdminHubFooter == AdminHubFooter
                        && m.Name == staff.FullName),
                    NotificationType.InformFcStaffOfApplicationAddedToDecisionPublicRegister,
                    It.Is<NotificationRecipient>(
                        r => r.Name == staff.FullName && r.Address == staff.Email),
                    null,
                    null,
                    null,
                    It.IsAny<CancellationToken>()), Times.Once);
        }

        _notificationsMock.VerifyNoOtherCalls();

        _auditMock.Verify(v =>
            v.PublishAuditEventAsync(It.Is<AuditEvent>(
                    e => e.EventName == AuditEvents.ApplicationRefused
                         && JsonSerializer.Serialize(e.AuditData, _options) ==
                         JsonSerializer.Serialize(new
                         {
                             application.WoodlandOwnerId,
                             ApplicationAuthorId = application.CreatedById,
                             NotificationSent = true,
                             RefusedByName = _fieldManager.FullName,
                             DecisionPublicRegisterOutcome = SendToDecisionPublicRegisterOutcome.Success
                         }, _options)),
                CancellationToken.None), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task ApplicationSetToApproved_IsSuccess_ApplicantAndAssignedFcStaffInformed_But_FailsToSendToDecisionPublicRegister(
        FellingLicenceApplication application,
        WoodlandOwnerModel woodlandOwnerModel,
        List<Flo.Services.InternalUsers.Models.UserAccountModel> fcStaff,
        UserAccountModel applicant,
        UserAccount approver,
        ApproverReviewModel approverReview)
    {
        approverReview.PublicRegisterPublish = true;
        application = PopulateStatusAndAssigneeHistory(application);
        var now = DateTime.UtcNow;
        _clockMock.Setup(s => s.GetCurrentInstant()).Returns(Instant.FromDateTimeUtc(now));

        var sut = CreateSut();

        _getFLAMock.Setup(s => s.GetApplicationByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(application);

        _notificationsMock.Setup(s => s.SendNotificationAsync(It.IsAny<object>(),
                It.IsAny<NotificationType>(), It.IsAny<NotificationRecipient>(), It.IsAny<NotificationRecipient[]?>(),
                It.IsAny<NotificationAttachment[]?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success);

        _woodlandOwnerServiceMock
            .Setup(s => s.RetrieveWoodlandOwnerByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(woodlandOwnerModel);

        _internalAccountServiceMock
            .Setup(s => s.RetrieveUserAccountsByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(fcStaff);

        _internalAccountServiceMock
            .Setup(s => s.GetUserAccountAsync(_fieldManager.UserAccountId.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(approver.AsMaybe());

        _externalAccountServiceMock
            .Setup(s => s.RetrieveUserAccountByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(applicant);

        _approverReviewServiceMock
            .Setup(r => r.GetApproverReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(approverReview);

        _publicRegister.Setup(x => x.AddCaseToDecisionRegisterAsync(
            application.PublicRegister!.EsriId!.Value,
            application.ApplicationReference,
            FellingLicenceStatus.Approved.ToString(),   
            now,
            It.IsAny<CancellationToken>())).ReturnsAsync(Result.Failure("Some error"));

        var result = await sut.ApproveOrRefuseOrReferApplicationAsync(
            _fieldManager,
            application.Id,
            FellingLicenceStatus.Approved,
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.SubProcessFailures.Single().Should().Be(FinaliseFellingLicenceApplicationProcessOutcomes.CouldNotPublishToDecisionPublicRegister);

        _publicRegister.Verify(x => x.AddCaseToDecisionRegisterAsync(
            application.PublicRegister!.EsriId!.Value,
            application.ApplicationReference,
            FellingLicenceStatus.Approved.ToString(),
            now,
            It.IsAny<CancellationToken>()),
            Times.Once());

        _clockMock.Verify(x => x.GetCurrentInstant(), Times.Once);

        _getFLAMock.Verify(v => v.GetApplicationByIdAsync(application.Id, CancellationToken.None), Times.Once);
        _woodlandOwnerServiceMock.Verify(v => v.RetrieveWoodlandOwnerByIdAsync(application.WoodlandOwnerId, CancellationToken.None), Times.Once);
        _internalAccountServiceMock.Verify(v => v.RetrieveUserAccountsByIdsAsync(It.Is<List<Guid>>(a =>
            a.TrueForAll(x => application.AssigneeHistories.Where(y => y.Role != AssignedUserRole.Applicant).Select(y => y.AssignedUserId).Contains(x))),
            CancellationToken.None), Times.Once);
        _externalAccountServiceMock.Verify(v => v.RetrieveUserAccountByIdAsync(application.CreatedById, CancellationToken.None), Times.Once);
        _updateFLAMock.Verify(v => v.AddStatusHistoryAsync(It.IsAny<Guid>(), application.Id, FellingLicenceStatus.Approved, CancellationToken.None), Times.Once);
        _updateFLAMock.Verify(v => v.AddDecisionPublicRegisterDetailsAsync(application.Id, It.IsAny<DateTime>(), It.IsAny<DateTime>(), CancellationToken.None), Times.Never);

        _notificationsMock.Verify(v => v.SendNotificationAsync(It.Is<InformApplicantOfApplicationApprovalDataModel>(a =>
            a.ApplicationReference == application.ApplicationReference
            && a.PropertyName == application.SubmittedFlaPropertyDetail.Name
            && a.ApproverName == approver.FullName(false)
            && a.ViewApplicationURL == $"{ExternalUrl}FellingLicenceApplication/SupportingDocumentation/{application.Id}"
            && a.AdminHubFooter == AdminHubFooter
            && a.Name == applicant.FullName),
            NotificationType.InformApplicantOfApplicationApproval,
            It.Is<NotificationRecipient>(a => a.Address == applicant.Email && a.Name == applicant.FullName),
            It.Is<NotificationRecipient[]>(a => a.Length == fcStaff.Count + 1 
                                                && a.Any(x => x.Address == woodlandOwnerModel.ContactEmail && x.Name == woodlandOwnerModel.ContactName)
                                                && fcStaff.All(fc => a.Any(app => app.Address == fc.Email))),
            null, null,
            CancellationToken.None), Times.Once);

        _auditMock.Verify(v =>
            v.PublishAuditEventAsync(It.Is<AuditEvent>(
                    e => e.EventName == AuditEvents.ApplicationApproved
                         && JsonSerializer.Serialize(e.AuditData, _options) ==
                         JsonSerializer.Serialize(new
                         {
                             application.WoodlandOwnerId,
                             ApplicationAuthorId = application.CreatedById,
                             NotificationSent = true,
                             ApprovedByName = _fieldManager.FullName,
                             DecisionPublicRegisterOutcome = SendToDecisionPublicRegisterOutcome.Failure
                         }, _options)),
                CancellationToken.None), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task ApplicationSetToApproved_IsSuccess_But_ApplicantCouldNotBeInformed_AndFailsToSendToDecisionPublicRegister(
        FellingLicenceApplication application,
        WoodlandOwnerModel woodlandOwnerModel,
        List<Flo.Services.InternalUsers.Models.UserAccountModel> fcStaff,
        UserAccountModel applicant, 
        UserAccount approver, 
        ApproverReviewModel approverReview)
    {
        approverReview.PublicRegisterPublish = true;
        application = PopulateStatusAndAssigneeHistory(application);
        var now = DateTime.UtcNow;
        _clockMock.Setup(s => s.GetCurrentInstant()).Returns(Instant.FromDateTimeUtc(now));

        var sut = CreateSut();

        _getFLAMock.Setup(s => s.GetApplicationByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(application);

        _notificationsMock.Setup(s => s.SendNotificationAsync(It.IsAny<object>(),
                It.Is<NotificationType>(n => n == NotificationType.InformApplicantOfApplicationApproval), It.IsAny<NotificationRecipient>(), It.IsAny<NotificationRecipient[]?>(),
                It.IsAny<NotificationAttachment[]?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("error"));

        _woodlandOwnerServiceMock
            .Setup(s => s.RetrieveWoodlandOwnerByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(woodlandOwnerModel);

        _internalAccountServiceMock
            .Setup(s => s.RetrieveUserAccountsByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(fcStaff);

        _internalAccountServiceMock
            .Setup(s => s.GetUserAccountAsync(_fieldManager.UserAccountId.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(approver.AsMaybe());

        _externalAccountServiceMock
            .Setup(s => s.RetrieveUserAccountByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(applicant);

        _approverReviewServiceMock
            .Setup(r => r.GetApproverReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(approverReview);

        _publicRegister.Setup(x => x.AddCaseToDecisionRegisterAsync(
            application.PublicRegister!.EsriId!.Value,
            application.ApplicationReference,
            FellingLicenceStatus.Approved.ToString(),
            now,
            It.IsAny<CancellationToken>())).ReturnsAsync(Result.Failure("Some error"));

        var result = await sut.ApproveOrRefuseOrReferApplicationAsync(
            _fieldManager,
            application.Id,
            FellingLicenceStatus.Approved,
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.SubProcessFailures.Count.Should().Be(2);
        result.SubProcessFailures.Should().Contain(FinaliseFellingLicenceApplicationProcessOutcomes.CouldNotPublishToDecisionPublicRegister);
        result.SubProcessFailures.Should().Contain(FinaliseFellingLicenceApplicationProcessOutcomes.CouldNotSendNotificationToApplicant);

        _publicRegister.Verify(x => x.AddCaseToDecisionRegisterAsync(
            application.PublicRegister!.EsriId!.Value,
            application.ApplicationReference,
            FellingLicenceStatus.Approved.ToString(),
            now,
            It.IsAny<CancellationToken>()),
            Times.Once());

        _clockMock.Verify(x => x.GetCurrentInstant(), Times.Once);

        _getFLAMock.Verify(v => v.GetApplicationByIdAsync(application.Id, CancellationToken.None), Times.Once);
        _woodlandOwnerServiceMock.Verify(v => v.RetrieveWoodlandOwnerByIdAsync(application.WoodlandOwnerId, CancellationToken.None), Times.Once);
        _internalAccountServiceMock.Verify(v => v.RetrieveUserAccountsByIdsAsync(It.Is<List<Guid>>(a =>
            a.TrueForAll(x => application.AssigneeHistories.Where(y => y.Role != AssignedUserRole.Applicant).Select(y => y.AssignedUserId).Contains(x))),
            CancellationToken.None), Times.Once);
        _externalAccountServiceMock.Verify(v => v.RetrieveUserAccountByIdAsync(application.CreatedById, CancellationToken.None), Times.Once);
        _updateFLAMock.Verify(v => v.AddStatusHistoryAsync(It.IsAny<Guid>(), application.Id, FellingLicenceStatus.Approved, CancellationToken.None), Times.Once);
        _updateFLAMock.Verify(v => v.AddDecisionPublicRegisterDetailsAsync(application.Id, It.IsAny<DateTime>(), It.IsAny<DateTime>(), CancellationToken.None), Times.Never);

        _notificationsMock.Verify(v => v.SendNotificationAsync(It.Is<InformApplicantOfApplicationApprovalDataModel>(a =>
            a.ApplicationReference == application.ApplicationReference
            && a.PropertyName == application.SubmittedFlaPropertyDetail.Name
            && a.ApproverName == approver.FullName(false)
            && a.ViewApplicationURL == $"{ExternalUrl}FellingLicenceApplication/SupportingDocumentation/{application.Id}"
            && a.AdminHubFooter == AdminHubFooter
            && a.Name == applicant.FullName),
            NotificationType.InformApplicantOfApplicationApproval,
            It.Is<NotificationRecipient>(a => a.Address == applicant.Email && a.Name == applicant.FullName),
            It.Is<NotificationRecipient[]>(a => a.Length == fcStaff.Count + 1 && 
                                                a.Any(x => x.Address == woodlandOwnerModel.ContactEmail && x.Name == woodlandOwnerModel.ContactName)
                                                && fcStaff.All(fc => a.Any(app => app.Address == fc.Email))),
            null, null, 
            CancellationToken.None), Times.Once);

        _auditMock.Verify(v =>
            v.PublishAuditEventAsync(It.Is<AuditEvent>(
                    e => e.EventName == AuditEvents.ApplicationApproved
                         && JsonSerializer.Serialize(e.AuditData, _options) ==
                         JsonSerializer.Serialize(new
                         {
                             application.WoodlandOwnerId,
                             ApplicationAuthorId = application.CreatedById,
                             NotificationSent = false,
                             ApprovedByName = _fieldManager.FullName,
                             DecisionPublicRegisterOutcome = SendToDecisionPublicRegisterOutcome.Failure
                         }, _options)),
                CancellationToken.None), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task ApplicationSetToApproved_IsSuccess_But_FailsToSendToDecisionPublicRegister(
         FellingLicenceApplication application,
         WoodlandOwnerModel woodlandOwnerModel,
         List<Flo.Services.InternalUsers.Models.UserAccountModel> fcStaff,
         UserAccountModel applicant,
         UserAccount approver,
         ApproverReviewModel approverReview)
    {
        approverReview.PublicRegisterPublish = true;
        application = PopulateStatusAndAssigneeHistory(application);
        var now = DateTime.UtcNow;
        _clockMock.Setup(s => s.GetCurrentInstant()).Returns(Instant.FromDateTimeUtc(now));

        var sut = CreateSut();

        _getFLAMock.Setup(s => s.GetApplicationByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(application);

        _notificationsMock.Setup(s => s.SendNotificationAsync(It.IsAny<object>(),
                It.Is<NotificationType>(n => n == NotificationType.InformApplicantOfApplicationApproval), It.IsAny<NotificationRecipient>(), It.IsAny<NotificationRecipient[]?>(),
                It.IsAny<NotificationAttachment[]?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success);

        _woodlandOwnerServiceMock
            .Setup(s => s.RetrieveWoodlandOwnerByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(woodlandOwnerModel);

        _internalAccountServiceMock
            .Setup(s => s.RetrieveUserAccountsByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(fcStaff);

        _internalAccountServiceMock
            .Setup(x => x.GetUserAccountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(approver.AsMaybe());

        _externalAccountServiceMock
            .Setup(s => s.RetrieveUserAccountByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(applicant);

        _approverReviewServiceMock
            .Setup(r => r.GetApproverReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(approverReview);

        _publicRegister.Setup(x => x.AddCaseToDecisionRegisterAsync(
            application.PublicRegister!.EsriId!.Value,
            application.ApplicationReference,
            FellingLicenceStatus.Approved.ToString(),
            now,
            It.IsAny<CancellationToken>())).ReturnsAsync(Result.Failure("Some error"));

        var result = await sut.ApproveOrRefuseOrReferApplicationAsync(
            _fieldManager,
            application.Id,
            FellingLicenceStatus.Approved,
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.SubProcessFailures.Count.Should().Be(1);
        result.SubProcessFailures.Should().Contain(FinaliseFellingLicenceApplicationProcessOutcomes.CouldNotPublishToDecisionPublicRegister);

        _publicRegister.Verify(x => x.AddCaseToDecisionRegisterAsync(
            application.PublicRegister!.EsriId!.Value,
            application.ApplicationReference,
            FellingLicenceStatus.Approved.ToString(),
            now,
            It.IsAny<CancellationToken>()),
            Times.Once());

        _clockMock.Verify(x => x.GetCurrentInstant(), Times.Once);

        _getFLAMock.Verify(v => v.GetApplicationByIdAsync(application.Id, CancellationToken.None), Times.Once);
        _woodlandOwnerServiceMock.Verify(v => v.RetrieveWoodlandOwnerByIdAsync(application.WoodlandOwnerId, CancellationToken.None), Times.Once);
        _internalAccountServiceMock.Verify(v => v.RetrieveUserAccountsByIdsAsync(It.Is<List<Guid>>(a =>
            a.TrueForAll(x => application.AssigneeHistories.Where(y => y.Role != AssignedUserRole.Applicant).Select(y => y.AssignedUserId).Contains(x))),
            CancellationToken.None), Times.Once);
        _externalAccountServiceMock.Verify(v => v.RetrieveUserAccountByIdAsync(application.CreatedById, CancellationToken.None), Times.Once);
        _updateFLAMock.Verify(v => v.AddStatusHistoryAsync(It.IsAny<Guid>(), application.Id, FellingLicenceStatus.Approved, CancellationToken.None), Times.Once);
        _updateFLAMock.Verify(v => v.AddDecisionPublicRegisterDetailsAsync(application.Id, It.IsAny<DateTime>(), It.IsAny<DateTime>(), CancellationToken.None), Times.Never);

        _notificationsMock.Verify(v => v.SendNotificationAsync(It.Is<InformApplicantOfApplicationApprovalDataModel>(a =>
            a.ApplicationReference == application.ApplicationReference
            && a.ViewApplicationURL == $"{ExternalUrl}FellingLicenceApplication/SupportingDocumentation/{application.Id}"
            && a.AdminHubFooter == AdminHubFooter
            && a.Name == applicant.FullName),
            NotificationType.InformApplicantOfApplicationApproval,
            It.Is<NotificationRecipient>(a => a.Address == applicant.Email && a.Name == applicant.FullName),
            It.Is<NotificationRecipient[]>(a => a.Length == fcStaff.Count + 1 &&
                                                a.Any(x => x.Address == woodlandOwnerModel.ContactEmail && x.Name == woodlandOwnerModel.ContactName)
                                                && fcStaff.All(fc => a.Any(app => app.Address == fc.Email))),
            null, null,
            CancellationToken.None), Times.Once);


        _auditMock.Verify(v =>
            v.PublishAuditEventAsync(It.Is<AuditEvent>(
                    e => e.EventName == AuditEvents.ApplicationApproved
                         && JsonSerializer.Serialize(e.AuditData, _options) ==
                         JsonSerializer.Serialize(new
                         {
                             application.WoodlandOwnerId,
                             ApplicationAuthorId = application.CreatedById,
                             NotificationSent = true,
                             ApprovedByName = _fieldManager.FullName,
                             DecisionPublicRegisterOutcome = SendToDecisionPublicRegisterOutcome.Failure
                         }, _options)),
                CancellationToken.None), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task ApplicationSetToApproved_IsSuccess_But_CouldNotStorePublicRegisterDecisionDetailsLocally(
       FellingLicenceApplication application,
       WoodlandOwnerModel woodlandOwnerModel,
       List<Flo.Services.InternalUsers.Models.UserAccountModel> fcStaff,
       UserAccountModel applicant, 
       UserAccount approver, 
       ApproverReviewModel approverReview)
    {
        approverReview.PublicRegisterPublish = true;
        application = PopulateStatusAndAssigneeHistory(application);
        var now = DateTime.UtcNow;
        _clockMock.Setup(s => s.GetCurrentInstant()).Returns(Instant.FromDateTimeUtc(now));

        var sut = CreateSut();

        _getFLAMock.Setup(s => s.GetApplicationByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(application);

        _notificationsMock.Setup(s => s.SendNotificationAsync(It.IsAny<object>(),
                It.Is<NotificationType>(n => n == NotificationType.InformApplicantOfApplicationApproval), It.IsAny<NotificationRecipient>(), It.IsAny<NotificationRecipient[]?>(),
                It.IsAny<NotificationAttachment[]?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success);

        _woodlandOwnerServiceMock
            .Setup(s => s.RetrieveWoodlandOwnerByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(woodlandOwnerModel);

        _internalAccountServiceMock
            .Setup(s => s.RetrieveUserAccountsByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(fcStaff);

        _internalAccountServiceMock
            .Setup(s => s.GetUserAccountAsync(_fieldManager.UserAccountId.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(approver.AsMaybe());

        _externalAccountServiceMock
            .Setup(s => s.RetrieveUserAccountByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(applicant);

        _approverReviewServiceMock
            .Setup(r => r.GetApproverReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(approverReview);

        _publicRegister.Setup(x => x.AddCaseToDecisionRegisterAsync(
            application.PublicRegister!.EsriId!.Value,
            application.ApplicationReference,
            FellingLicenceStatus.Approved.ToString(),
            now,
            It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success);

        _updateFLAMock
            .Setup(
                x => x.AddDecisionPublicRegisterDetailsAsync(application.Id, It.IsAny<DateTime>(),
                    It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("oops"));

        var result = await sut.ApproveOrRefuseOrReferApplicationAsync(
            _fieldManager,
            application.Id,
            FellingLicenceStatus.Approved,
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.SubProcessFailures.Count.Should().Be(1);
        result.SubProcessFailures.Should().Contain(FinaliseFellingLicenceApplicationProcessOutcomes.CouldNotStoreDecisionDetailsLocally);

        _publicRegister.Verify(x => x.AddCaseToDecisionRegisterAsync(
            application.PublicRegister!.EsriId!.Value,
            application.ApplicationReference,
            FellingLicenceStatus.Approved.ToString(),
            now,
            It.IsAny<CancellationToken>()),
            Times.Once());

        _clockMock.Verify(x => x.GetCurrentInstant(), Times.Once);

        _getFLAMock.Verify(v => v.GetApplicationByIdAsync(application.Id, CancellationToken.None), Times.Once);
        _woodlandOwnerServiceMock.Verify(v => v.RetrieveWoodlandOwnerByIdAsync(application.WoodlandOwnerId, CancellationToken.None), Times.Once);
        _internalAccountServiceMock.Verify(v => v.RetrieveUserAccountsByIdsAsync(It.Is<List<Guid>>(a =>
            a.TrueForAll(x => application.AssigneeHistories.Where(y => y.Role != AssignedUserRole.Applicant).Select(y => y.AssignedUserId).Contains(x))),
            CancellationToken.None), Times.Once);
        _externalAccountServiceMock.Verify(v => v.RetrieveUserAccountByIdAsync(application.CreatedById, CancellationToken.None), Times.Once);
        _updateFLAMock.Verify(v => v.AddStatusHistoryAsync(It.IsAny<Guid>(), application.Id, FellingLicenceStatus.Approved, CancellationToken.None), Times.Once);
        _updateFLAMock.Verify(v => v.AddDecisionPublicRegisterDetailsAsync(
            application.Id, 
            now, 
            now.AddDays(28), CancellationToken.None), Times.Once);

        _notificationsMock.Verify(v => v.SendNotificationAsync(It.Is<InformApplicantOfApplicationApprovalDataModel>(a =>
            a.ApplicationReference == application.ApplicationReference
            && a.PropertyName == application.SubmittedFlaPropertyDetail.Name
            && a.ApproverName == approver.FullName(false)
            && a.ViewApplicationURL == $"{ExternalUrl}FellingLicenceApplication/SupportingDocumentation/{application.Id}"
            && a.AdminHubFooter == AdminHubFooter
            && a.Name == applicant.FullName),
            NotificationType.InformApplicantOfApplicationApproval,
            It.Is<NotificationRecipient>(a => a.Address == applicant.Email && a.Name == applicant.FullName),
            It.Is<NotificationRecipient[]>(a => a.Length == fcStaff.Count + 1 
                                                && a.Any(x => x.Address == woodlandOwnerModel.ContactEmail && x.Name == woodlandOwnerModel.ContactName) 
                                                && fcStaff.All(fc => a.Any(app => app.Address == fc.Email))),
            null, null, 
            CancellationToken.None), Times.Once);

        _auditMock.Verify(v =>
            v.PublishAuditEventAsync(It.Is<AuditEvent>(
                    e => e.EventName == AuditEvents.ApplicationApproved
                         && JsonSerializer.Serialize(e.AuditData, _options) ==
                         JsonSerializer.Serialize(new
                         {
                             application.WoodlandOwnerId,
                             ApplicationAuthorId = application.CreatedById,
                             NotificationSent = true,
                             ApprovedByName = _fieldManager.FullName,
                             DecisionPublicRegisterOutcome = SendToDecisionPublicRegisterOutcome.FailedToSaveDecisionDetailsLocally
                         }, _options)),
                CancellationToken.None), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task ApplicationSetToRefused_IsSuccess__But_ApplicantCouldNotBeInformed_AndFailsToSendToDecisionPublicRegister(
        FellingLicenceApplication application,
        WoodlandOwnerModel woodlandOwnerModel,
        List<Flo.Services.InternalUsers.Models.UserAccountModel> fcStaff,
        UserAccountModel applicant, 
        UserAccount approver, 
        ApproverReviewModel approverReview)
    {
        approverReview.PublicRegisterPublish = true;
        application = PopulateStatusAndAssigneeHistory(application);

        var now = DateTime.UtcNow;
        _clockMock.Setup(s => s.GetCurrentInstant()).Returns(Instant.FromDateTimeUtc(now));

        var sut = CreateSut();

        _getFLAMock.Setup(s => s.GetApplicationByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(application);

        _notificationsMock.Setup(s => s.SendNotificationAsync(It.IsAny<object>(),
                It.Is<NotificationType>(n => n == NotificationType.InformApplicantOfApplicationRefusal), It.IsAny<NotificationRecipient>(), It.IsAny<NotificationRecipient[]?>(),
                It.IsAny<NotificationAttachment[]?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("error"));

        _woodlandOwnerServiceMock
            .Setup(s => s.RetrieveWoodlandOwnerByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(woodlandOwnerModel);

        _internalAccountServiceMock
            .Setup(s => s.RetrieveUserAccountsByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(fcStaff);

        _internalAccountServiceMock
            .Setup(s => s.GetUserAccountAsync(_fieldManager.UserAccountId.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(approver.AsMaybe());

        _externalAccountServiceMock
            .Setup(s => s.RetrieveUserAccountByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(applicant);

        _approverReviewServiceMock
            .Setup(r => r.GetApproverReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(approverReview);

        _publicRegister.Setup(x => x.AddCaseToDecisionRegisterAsync(
            application.PublicRegister!.EsriId!.Value,
            application.ApplicationReference,
            FellingLicenceStatus.Refused.ToString(),
            now,
            It.IsAny<CancellationToken>())).ReturnsAsync(Result.Failure("Some error"));

        var result = await sut.ApproveOrRefuseOrReferApplicationAsync(
            _fieldManager,
            application.Id,
            FellingLicenceStatus.Refused,
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.SubProcessFailures.Count.Should().Be(2);
        result.SubProcessFailures.Should().Contain(FinaliseFellingLicenceApplicationProcessOutcomes.CouldNotPublishToDecisionPublicRegister);
        result.SubProcessFailures.Should().Contain(FinaliseFellingLicenceApplicationProcessOutcomes.CouldNotSendNotificationToApplicant);

        _getFLAMock.Verify(v => v.GetApplicationByIdAsync(application.Id, CancellationToken.None), Times.Once);
        _woodlandOwnerServiceMock.Verify(v => v.RetrieveWoodlandOwnerByIdAsync(application.WoodlandOwnerId, CancellationToken.None), Times.Once);
        _internalAccountServiceMock.Verify(v => v.RetrieveUserAccountsByIdsAsync(It.Is<List<Guid>>(a =>
                a.TrueForAll(x => application.AssigneeHistories.Where(y => y.Role != AssignedUserRole.Applicant).Select(y => y.AssignedUserId).Contains(x))),
            CancellationToken.None), Times.Once);
        _externalAccountServiceMock.Verify(v => v.RetrieveUserAccountByIdAsync(application.CreatedById, CancellationToken.None), Times.Once);
        _updateFLAMock.Verify(v => v.AddStatusHistoryAsync(It.IsAny<Guid>(), application.Id, FellingLicenceStatus.Refused, CancellationToken.None), Times.Once);
        _updateFLAMock.Verify(v => v.AddDecisionPublicRegisterDetailsAsync(application.Id, It.IsAny<DateTime>(), It.IsAny<DateTime>(), CancellationToken.None), Times.Never);

        _notificationsMock.Verify(v => v.SendNotificationAsync(It.Is<InformApplicantOfApplicationRefusalDataModel>(a =>
            a.ApplicationReference == application.ApplicationReference
            && a.PropertyName == application.SubmittedFlaPropertyDetail.Name
            && a.ApproverName == approver.FullName(false)
            && a.ApproverEmail == approver.Email
            && a.ViewApplicationURL == $"{ExternalUrl}FellingLicenceApplication/ApplicationTaskList/{application.Id}"
            && a.AdminHubFooter == AdminHubFooter
            && a.Name == applicant.FullName),
            NotificationType.InformApplicantOfApplicationRefusal,
            It.Is<NotificationRecipient>(a => a.Address == applicant.Email && a.Name == applicant.FullName),
            It.Is<NotificationRecipient[]>(a => a.Length == fcStaff.Count + 1 
                                                && a.Any(x => x.Address == woodlandOwnerModel.ContactEmail && x.Name == woodlandOwnerModel.ContactName)
                                                && fcStaff.All(fc => a.Any(app => app.Address == fc.Email))),
            null, null, 
            CancellationToken.None), Times.Once);

        _auditMock.Verify(v =>
            v.PublishAuditEventAsync(It.Is<AuditEvent>(
                    e => e.EventName == AuditEvents.ApplicationRefused
                         && JsonSerializer.Serialize(e.AuditData, _options) ==
                         JsonSerializer.Serialize(new
                         {
                             application.WoodlandOwnerId,
                             ApplicationAuthorId = application.CreatedById,
                             NotificationSent = false,
                             RefusedByName = _fieldManager.FullName,
                             DecisionPublicRegisterOutcome = SendToDecisionPublicRegisterOutcome.Failure
                         }, _options)),
                CancellationToken.None), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task ApplicationSetToRefused_IsSuccess__But_FailsToSendToDecisionPublicRegister(
        FellingLicenceApplication application,
        WoodlandOwnerModel woodlandOwnerModel,
        List<Flo.Services.InternalUsers.Models.UserAccountModel> fcStaff,
        UserAccountModel applicant,
        UserAccount approver,
        ApproverReviewModel approverReview)
    {
        approverReview.PublicRegisterPublish = true;
        application = PopulateStatusAndAssigneeHistory(application);

        var now = DateTime.UtcNow;
        _clockMock.Setup(s => s.GetCurrentInstant()).Returns(Instant.FromDateTimeUtc(now));

        var sut = CreateSut();

        _getFLAMock.Setup(s => s.GetApplicationByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(application);

        _notificationsMock.Setup(s => s.SendNotificationAsync(It.IsAny<object>(),
                It.Is<NotificationType>(n => n == NotificationType.InformApplicantOfApplicationRefusal), It.IsAny<NotificationRecipient>(), It.IsAny<NotificationRecipient[]?>(),
                It.IsAny<NotificationAttachment[]?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success);

        _woodlandOwnerServiceMock
            .Setup(s => s.RetrieveWoodlandOwnerByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(woodlandOwnerModel);

        _internalAccountServiceMock
            .Setup(s => s.RetrieveUserAccountsByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(fcStaff);

        _internalAccountServiceMock
            .Setup(x => x.GetUserAccountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(approver.AsMaybe);

        _externalAccountServiceMock
            .Setup(s => s.RetrieveUserAccountByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(applicant);

        _approverReviewServiceMock
            .Setup(r => r.GetApproverReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(approverReview);

        _publicRegister.Setup(x => x.AddCaseToDecisionRegisterAsync(
            application.PublicRegister!.EsriId!.Value,
            application.ApplicationReference,
            FellingLicenceStatus.Refused.ToString(),
            now,
            It.IsAny<CancellationToken>())).ReturnsAsync(Result.Failure("Some error"));

        var result = await sut.ApproveOrRefuseOrReferApplicationAsync(
            _fieldManager,
            application.Id,
            FellingLicenceStatus.Refused,
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.SubProcessFailures.Count.Should().Be(1);
        result.SubProcessFailures.Should().Contain(FinaliseFellingLicenceApplicationProcessOutcomes.CouldNotPublishToDecisionPublicRegister);

        _getFLAMock.Verify(v => v.GetApplicationByIdAsync(application.Id, CancellationToken.None), Times.Once);
        _woodlandOwnerServiceMock.Verify(v => v.RetrieveWoodlandOwnerByIdAsync(application.WoodlandOwnerId, CancellationToken.None), Times.Once);
        _internalAccountServiceMock.Verify(v => v.RetrieveUserAccountsByIdsAsync(It.Is<List<Guid>>(a =>
                a.TrueForAll(x => application.AssigneeHistories.Where(y => y.Role != AssignedUserRole.Applicant).Select(y => y.AssignedUserId).Contains(x))),
            CancellationToken.None), Times.Once);
        _externalAccountServiceMock.Verify(v => v.RetrieveUserAccountByIdAsync(application.CreatedById, CancellationToken.None), Times.Once);
        _updateFLAMock.Verify(v => v.AddStatusHistoryAsync(It.IsAny<Guid>(), application.Id, FellingLicenceStatus.Refused, CancellationToken.None), Times.Once);
        _updateFLAMock.Verify(v => v.AddDecisionPublicRegisterDetailsAsync(application.Id, It.IsAny<DateTime>(), It.IsAny<DateTime>(), CancellationToken.None), Times.Never);

        _notificationsMock.Verify(v => v.SendNotificationAsync(It.Is<InformApplicantOfApplicationRefusalDataModel>(a =>
            a.ApplicationReference == application.ApplicationReference
            && a.ViewApplicationURL == $"{ExternalUrl}FellingLicenceApplication/ApplicationTaskList/{application.Id}"
            && a.AdminHubFooter == AdminHubFooter
            && a.Name == applicant.FullName),
            NotificationType.InformApplicantOfApplicationRefusal,
            It.Is<NotificationRecipient>(a => a.Address == applicant.Email && a.Name == applicant.FullName),
            It.Is<NotificationRecipient[]>(a => a.Length == fcStaff.Count + 1
                                                && a.Any(x => x.Address == woodlandOwnerModel.ContactEmail && x.Name == woodlandOwnerModel.ContactName)
                                                && fcStaff.All(fc => a.Any(app => app.Address == fc.Email))),
            null, null, 
            CancellationToken.None), Times.Once);

        _auditMock.Verify(v =>
            v.PublishAuditEventAsync(It.Is<AuditEvent>(
                    e => e.EventName == AuditEvents.ApplicationRefused
                         && JsonSerializer.Serialize(e.AuditData, _options) ==
                         JsonSerializer.Serialize(new
                         {
                             application.WoodlandOwnerId,
                             ApplicationAuthorId = application.CreatedById,
                             NotificationSent = true,
                             RefusedByName = _fieldManager.FullName,
                             DecisionPublicRegisterOutcome = SendToDecisionPublicRegisterOutcome.Failure
                         }, _options)),
                CancellationToken.None), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task ApplicationSetToApproved_IsSuccess_But_ApplicantAndAssignedFcStaffCouldNotBeInformed_AndFailsToSendToDecisionPublicRegister(
        FellingLicenceApplication application,
        WoodlandOwnerModel woodlandOwnerModel,
        List<Flo.Services.InternalUsers.Models.UserAccountModel> fcStaff,
        UserAccountModel applicant, 
        UserAccount approver,
        ApproverReviewModel approverReview)
    {
        approverReview.PublicRegisterPublish = true;
        application = PopulateStatusAndAssigneeHistory(application);
        var now = DateTime.UtcNow;
        _clockMock.Setup(s => s.GetCurrentInstant()).Returns(Instant.FromDateTimeUtc(now));

        var sut = CreateSut();

        _getFLAMock.Setup(s => s.GetApplicationByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(application);

        _notificationsMock.Setup(s => s.SendNotificationAsync(It.IsAny<object>(),
                It.IsAny<NotificationType>(), It.IsAny<NotificationRecipient>(), It.IsAny<NotificationRecipient[]?>(),
                It.IsAny<NotificationAttachment[]?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("error"));

        _woodlandOwnerServiceMock
            .Setup(s => s.RetrieveWoodlandOwnerByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(woodlandOwnerModel);

        _internalAccountServiceMock
            .Setup(s => s.RetrieveUserAccountsByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(fcStaff);

        _internalAccountServiceMock
            .Setup(s => s.GetUserAccountAsync(_fieldManager.UserAccountId.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(approver.AsMaybe());

        _externalAccountServiceMock
            .Setup(s => s.RetrieveUserAccountByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(applicant);

        _approverReviewServiceMock
            .Setup(r => r.GetApproverReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(approverReview);

        _publicRegister.Setup(x => x.AddCaseToDecisionRegisterAsync(
            application.PublicRegister!.EsriId!.Value,
            application.ApplicationReference,
            FellingLicenceStatus.Approved.ToString(),
            now,
            It.IsAny<CancellationToken>())).ReturnsAsync(Result.Failure("Some error"));

        var result = await sut.ApproveOrRefuseOrReferApplicationAsync(
            _fieldManager,
            application.Id,
            FellingLicenceStatus.Approved,
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.SubProcessFailures.Count.Should().Be(2);
        result.SubProcessFailures.Should().Contain(FinaliseFellingLicenceApplicationProcessOutcomes.CouldNotPublishToDecisionPublicRegister);
        result.SubProcessFailures.Should().Contain(FinaliseFellingLicenceApplicationProcessOutcomes.CouldNotSendNotificationToApplicant);

        _publicRegister.Verify(x => x.AddCaseToDecisionRegisterAsync(
            application.PublicRegister!.EsriId!.Value,
            application.ApplicationReference,
            FellingLicenceStatus.Approved.ToString(),
            now,
            It.IsAny<CancellationToken>()),
            Times.Once());

        _clockMock.Verify(x => x.GetCurrentInstant(), Times.Once);

        _getFLAMock.Verify(v => v.GetApplicationByIdAsync(application.Id, CancellationToken.None), Times.Once);
        _woodlandOwnerServiceMock.Verify(v => v.RetrieveWoodlandOwnerByIdAsync(application.WoodlandOwnerId, CancellationToken.None), Times.Once);
        _internalAccountServiceMock.Verify(v => v.RetrieveUserAccountsByIdsAsync(It.Is<List<Guid>>(a =>
            a.TrueForAll(x => application.AssigneeHistories.Where(y => y.Role != AssignedUserRole.Applicant).Select(y => y.AssignedUserId).Contains(x))),
            CancellationToken.None), Times.Once);
        _externalAccountServiceMock.Verify(v => v.RetrieveUserAccountByIdAsync(application.CreatedById, CancellationToken.None), Times.Once);
        _updateFLAMock.Verify(v => v.AddStatusHistoryAsync(It.IsAny<Guid>(), application.Id, FellingLicenceStatus.Approved, CancellationToken.None), Times.Once);
        _updateFLAMock.Verify(v => v.AddDecisionPublicRegisterDetailsAsync(application.Id, It.IsAny<DateTime>(), It.IsAny<DateTime>(), CancellationToken.None), Times.Never);

        _notificationsMock.Verify(v => v.SendNotificationAsync(It.Is<InformApplicantOfApplicationApprovalDataModel>(a =>
            a.ApplicationReference == application.ApplicationReference
            && a.PropertyName == application.SubmittedFlaPropertyDetail.Name
            && a.ApproverName == approver.FullName(false)
            && a.ViewApplicationURL == $"{ExternalUrl}FellingLicenceApplication/SupportingDocumentation/{application.Id}"
            && a.AdminHubFooter == AdminHubFooter
            && a.Name == applicant.FullName),
            NotificationType.InformApplicantOfApplicationApproval,
            It.Is<NotificationRecipient>(a => a.Address == applicant.Email && a.Name == applicant.FullName),
            It.Is<NotificationRecipient[]>(a => a.Length == fcStaff.Count + 1 
                                                && a.Any(x => x.Address == woodlandOwnerModel.ContactEmail && x.Name == woodlandOwnerModel.ContactName) 
                                                && fcStaff.All(fc => a.Any(app => app.Address == fc.Email))),
            null, null,
            CancellationToken.None), Times.Once);

        _auditMock.Verify(v =>
            v.PublishAuditEventAsync(It.Is<AuditEvent>(
                    e => e.EventName == AuditEvents.ApplicationApproved
                         && JsonSerializer.Serialize(e.AuditData, _options) ==
                         JsonSerializer.Serialize(new
                         {
                             application.WoodlandOwnerId,
                             ApplicationAuthorId = application.CreatedById,
                             NotificationSent = false,
                             ApprovedByName = _fieldManager.FullName,
                             DecisionPublicRegisterOutcome = SendToDecisionPublicRegisterOutcome.Failure
                         }, _options)),
                CancellationToken.None), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task ApplicationSetToApproved_IsSuccess_ApplicantAndAssignedFcStaffInformed_But_WasExemptedFromPublicRegister(
        FellingLicenceApplication application,
        WoodlandOwnerModel woodlandOwnerModel,
        List<Flo.Services.InternalUsers.Models.UserAccountModel> fcStaff,
        UserAccountModel applicant,
        UserAccount approver, 
        ApproverReviewModel approverReview)
    {
        approverReview.PublicRegisterPublish = false;
        application = PopulateStatusAndAssigneeHistory(application);
        application.PublicRegister!.WoodlandOfficerSetAsExemptFromConsultationPublicRegister = true;
        
        var sut = CreateSut();

        _getFLAMock.Setup(s => s.GetApplicationByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(application);

        _notificationsMock.Setup(s => s.SendNotificationAsync(It.IsAny<object>(),
                It.IsAny<NotificationType>(), It.IsAny<NotificationRecipient>(), It.IsAny<NotificationRecipient[]?>(),
                It.IsAny<NotificationAttachment[]?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success);

        _woodlandOwnerServiceMock
            .Setup(s => s.RetrieveWoodlandOwnerByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(woodlandOwnerModel);

        _internalAccountServiceMock
            .Setup(s => s.RetrieveUserAccountsByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(fcStaff);

        _internalAccountServiceMock
            .Setup(s => s.GetUserAccountAsync(_fieldManager.UserAccountId.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(approver.AsMaybe());

        _externalAccountServiceMock
            .Setup(s => s.RetrieveUserAccountByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(applicant);

        _approverReviewServiceMock
            .Setup(r => r.GetApproverReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(approverReview);

        var result = await sut.ApproveOrRefuseOrReferApplicationAsync(
            _fieldManager,
            application.Id,
            FellingLicenceStatus.Approved,
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.SubProcessFailures.Should().BeEmpty();

        _publicRegister.Verify(x => x.AddCaseToDecisionRegisterAsync(
           It.IsAny<int>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<DateTime>(),
            It.IsAny<CancellationToken>()),
            Times.Never);

        _getFLAMock.Verify(v => v.GetApplicationByIdAsync(application.Id, CancellationToken.None), Times.Once);
        _woodlandOwnerServiceMock.Verify(v => v.RetrieveWoodlandOwnerByIdAsync(application.WoodlandOwnerId, CancellationToken.None), Times.Once);
        _internalAccountServiceMock.Verify(v => v.RetrieveUserAccountsByIdsAsync(It.Is<List<Guid>>(a =>
            a.TrueForAll(x => application.AssigneeHistories.Where(y => y.Role != AssignedUserRole.Applicant).Select(y => y.AssignedUserId).Contains(x))),
            CancellationToken.None), Times.Once);
        _externalAccountServiceMock.Verify(v => v.RetrieveUserAccountByIdAsync(application.CreatedById, CancellationToken.None), Times.Once);
        _updateFLAMock.Verify(v => v.AddStatusHistoryAsync(It.IsAny<Guid>(), application.Id, FellingLicenceStatus.Approved, CancellationToken.None), Times.Once);

        _notificationsMock.Verify(v => v.SendNotificationAsync(It.Is<InformApplicantOfApplicationApprovalDataModel>(a =>
            a.ApplicationReference == application.ApplicationReference
            && a.PropertyName == application.SubmittedFlaPropertyDetail.Name
            && a.ApproverName == approver.FullName(false)
            && a.ViewApplicationURL == $"{ExternalUrl}FellingLicenceApplication/SupportingDocumentation/{application.Id}"
            && a.AdminHubFooter == AdminHubFooter
            && a.Name == applicant.FullName),
            NotificationType.InformApplicantOfApplicationApproval,
            It.Is<NotificationRecipient>(a => a.Address == applicant.Email && a.Name == applicant.FullName),
            It.Is<NotificationRecipient[]>(a => a.Length == fcStaff.Count + 1 
                                                && a.Any(x => x.Address == woodlandOwnerModel.ContactEmail && x.Name == woodlandOwnerModel.ContactName) 
                                                && fcStaff.All(fc => a.Any(app => app.Address == fc.Email))),
            null, null,
            CancellationToken.None), Times.Once);

        _auditMock.Verify(v =>
            v.PublishAuditEventAsync(It.Is<AuditEvent>(
                    e => e.EventName == AuditEvents.ApplicationApproved
                         && JsonSerializer.Serialize(e.AuditData, _options) ==
                         JsonSerializer.Serialize(new
                         {
                             application.WoodlandOwnerId,
                             ApplicationAuthorId = application.CreatedById,
                             NotificationSent = true,
                             ApprovedByName = _fieldManager.FullName,
                             DecisionPublicRegisterOutcome = SendToDecisionPublicRegisterOutcome.Exempt
                         }, _options)),
                CancellationToken.None), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task ApplicationSetToRefused_IsSuccess_ApplicantAndAssignedFcStaffInformed_But_FailsToSendToDecisionPublicRegister(
        FellingLicenceApplication application,
        WoodlandOwnerModel woodlandOwnerModel,
        List<Flo.Services.InternalUsers.Models.UserAccountModel> fcStaff,
        UserAccountModel applicant, 
        UserAccount approver, 
        ApproverReviewModel approverReview)
    {
        approverReview.PublicRegisterPublish = true;
        application = PopulateStatusAndAssigneeHistory(application);
        var now = DateTime.UtcNow;
        _clockMock.Setup(s => s.GetCurrentInstant()).Returns(Instant.FromDateTimeUtc(now));

        var sut = CreateSut();

        _getFLAMock.Setup(s => s.GetApplicationByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(application);

        _notificationsMock.Setup(s => s.SendNotificationAsync(It.IsAny<object>(),
                It.IsAny<NotificationType>(), It.IsAny<NotificationRecipient>(), It.IsAny<NotificationRecipient[]?>(),
                It.IsAny<NotificationAttachment[]?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success);

        _woodlandOwnerServiceMock
            .Setup(s => s.RetrieveWoodlandOwnerByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(woodlandOwnerModel);

        _internalAccountServiceMock
            .Setup(s => s.RetrieveUserAccountsByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(fcStaff);

        _internalAccountServiceMock
            .Setup(s => s.GetUserAccountAsync(_fieldManager.UserAccountId.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(approver.AsMaybe());

        _externalAccountServiceMock
            .Setup(s => s.RetrieveUserAccountByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(applicant);

        _approverReviewServiceMock
            .Setup(r => r.GetApproverReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(approverReview);

        _publicRegister.Setup(x => x.AddCaseToDecisionRegisterAsync(
            application.PublicRegister!.EsriId!.Value,
            application.ApplicationReference,
            FellingLicenceStatus.Refused.ToString(),
            now,
            It.IsAny<CancellationToken>())).ReturnsAsync(Result.Failure("Some error"));

        var result = await sut.ApproveOrRefuseOrReferApplicationAsync(
            _fieldManager,
            application.Id,
            FellingLicenceStatus.Refused,
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.SubProcessFailures.Single().Should().Be(FinaliseFellingLicenceApplicationProcessOutcomes.CouldNotPublishToDecisionPublicRegister);
        
        _getFLAMock.Verify(v => v.GetApplicationByIdAsync(application.Id, CancellationToken.None), Times.Once);
        _woodlandOwnerServiceMock.Verify(v => v.RetrieveWoodlandOwnerByIdAsync(application.WoodlandOwnerId, CancellationToken.None), Times.Once);
        _internalAccountServiceMock.Verify(v => v.RetrieveUserAccountsByIdsAsync(It.Is<List<Guid>>(a =>
                a.TrueForAll(x => application.AssigneeHistories.Where(y => y.Role != AssignedUserRole.Applicant).Select(y => y.AssignedUserId).Contains(x))),
            CancellationToken.None), Times.Once);
        _externalAccountServiceMock.Verify(v => v.RetrieveUserAccountByIdAsync(application.CreatedById, CancellationToken.None), Times.Once);
        _updateFLAMock.Verify(v => v.AddStatusHistoryAsync(It.IsAny<Guid>(), application.Id, FellingLicenceStatus.Refused, CancellationToken.None), Times.Once);

        _updateFLAMock.Verify(v => v.AddDecisionPublicRegisterDetailsAsync(application.Id, It.IsAny<DateTime>(), It.IsAny<DateTime>(), CancellationToken.None), Times.Never);

        _notificationsMock.Verify(v => v.SendNotificationAsync(It.Is<InformApplicantOfApplicationRefusalDataModel>(a =>
            a.ApplicationReference == application.ApplicationReference
            && a.PropertyName == application.SubmittedFlaPropertyDetail.Name
            && a.ApproverName == approver.FullName(false)
            && a.ApproverEmail == approver.Email
            && a.ViewApplicationURL == $"{ExternalUrl}FellingLicenceApplication/ApplicationTaskList/{application.Id}"
            && a.AdminHubFooter == AdminHubFooter
            && a.Name == applicant.FullName),
            NotificationType.InformApplicantOfApplicationRefusal,
            It.Is<NotificationRecipient>(a => a.Address == applicant.Email && a.Name == applicant.FullName),
            It.Is<NotificationRecipient[]>(a => a.Length == fcStaff.Count + 1 
                                                && a.Any(x => x.Address == woodlandOwnerModel.ContactEmail && x.Name == woodlandOwnerModel.ContactName)
                                                && fcStaff.All(fc => a.Any(app => app.Address == fc.Email))),
            null, null,
            CancellationToken.None), Times.Once);

        _publicRegister.Verify(x => x.AddCaseToDecisionRegisterAsync(
                application.PublicRegister!.EsriId!.Value,
                application.ApplicationReference,
                It.Is<string>(a => a == "Refused"),
                now,
                It.IsAny<CancellationToken>()),
            Times.Once());

        _auditMock.Verify(v =>
            v.PublishAuditEventAsync(It.Is<AuditEvent>(
                    e => e.EventName == AuditEvents.ApplicationRefused
                         && JsonSerializer.Serialize(e.AuditData, _options) ==
                         JsonSerializer.Serialize(new
                         {
                             application.WoodlandOwnerId,
                             ApplicationAuthorId = application.CreatedById,
                             NotificationSent = true,
                             RefusedByName = _fieldManager.FullName,
                             DecisionPublicRegisterOutcome = SendToDecisionPublicRegisterOutcome.Failure
                         }, _options)),
                CancellationToken.None), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task ApplicationSetToRefused_IsSuccess_But_ApplicantAndAssignedFcStaffCouldNotBeInformed_And_FailsToSendToDecisionPublicRegister(
        FellingLicenceApplication application,
        WoodlandOwnerModel woodlandOwnerModel,
        List<Flo.Services.InternalUsers.Models.UserAccountModel> fcStaff,
        UserAccountModel applicant, 
        UserAccount approver,
        ApproverReviewModel approverReview)
    {
        approverReview.PublicRegisterPublish = true;
        application = PopulateStatusAndAssigneeHistory(application);
        var now = DateTime.UtcNow;
        _clockMock.Setup(s => s.GetCurrentInstant()).Returns(Instant.FromDateTimeUtc(now));

        var sut = CreateSut();

        _getFLAMock.Setup(s => s.GetApplicationByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(application);

        _notificationsMock.Setup(s => s.SendNotificationAsync(It.IsAny<object>(),
                It.IsAny<NotificationType>(), It.IsAny<NotificationRecipient>(), It.IsAny<NotificationRecipient[]?>(),
                It.IsAny<NotificationAttachment[]?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("err"));

        _woodlandOwnerServiceMock
            .Setup(s => s.RetrieveWoodlandOwnerByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(woodlandOwnerModel);

        _internalAccountServiceMock
            .Setup(s => s.RetrieveUserAccountsByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(fcStaff);

        _internalAccountServiceMock
            .Setup(s => s.GetUserAccountAsync(_fieldManager.UserAccountId.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(approver.AsMaybe());

        _externalAccountServiceMock
            .Setup(s => s.RetrieveUserAccountByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(applicant);

        _approverReviewServiceMock
            .Setup(r => r.GetApproverReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(approverReview);

        _publicRegister.Setup(x => x.AddCaseToDecisionRegisterAsync(
            application.PublicRegister!.EsriId!.Value,
            application.ApplicationReference,
            FellingLicenceStatus.Refused.ToString(),
            now,
            It.IsAny<CancellationToken>())).ReturnsAsync(Result.Failure("Some error"));

        var result = await sut.ApproveOrRefuseOrReferApplicationAsync(
            _fieldManager,
            application.Id,
            FellingLicenceStatus.Refused,
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.SubProcessFailures.Count.Should().Be(2);
        result.SubProcessFailures.Should().Contain(FinaliseFellingLicenceApplicationProcessOutcomes.CouldNotPublishToDecisionPublicRegister);
        result.SubProcessFailures.Should().Contain(FinaliseFellingLicenceApplicationProcessOutcomes.CouldNotSendNotificationToApplicant);

        _getFLAMock.Verify(v => v.GetApplicationByIdAsync(application.Id, CancellationToken.None), Times.Once);
        _woodlandOwnerServiceMock.Verify(v => v.RetrieveWoodlandOwnerByIdAsync(application.WoodlandOwnerId, CancellationToken.None), Times.Once);
        _internalAccountServiceMock.Verify(v => v.RetrieveUserAccountsByIdsAsync(It.Is<List<Guid>>(a =>
                a.TrueForAll(x => application.AssigneeHistories.Where(y => y.Role != AssignedUserRole.Applicant).Select(y => y.AssignedUserId).Contains(x))),
            CancellationToken.None), Times.Once);
        _externalAccountServiceMock.Verify(v => v.RetrieveUserAccountByIdAsync(application.CreatedById, CancellationToken.None), Times.Once);
        _updateFLAMock.Verify(v => v.AddStatusHistoryAsync(It.IsAny<Guid>(), application.Id, FellingLicenceStatus.Refused, CancellationToken.None), Times.Once);
        _updateFLAMock.Verify(v => v.AddDecisionPublicRegisterDetailsAsync(application.Id, It.IsAny<DateTime>(), It.IsAny<DateTime>(), CancellationToken.None), Times.Never);

        _notificationsMock.Verify(v => v.SendNotificationAsync(It.Is<InformApplicantOfApplicationRefusalDataModel>(a =>
            a.ApplicationReference == application.ApplicationReference
            && a.PropertyName == application.SubmittedFlaPropertyDetail.Name
            && a.ApproverName == approver.FullName(false)
            && a.ApproverEmail == approver.Email
            && a.ViewApplicationURL == $"{ExternalUrl}FellingLicenceApplication/ApplicationTaskList/{application.Id}"
            && a.AdminHubFooter == AdminHubFooter
            && a.Name == applicant.FullName),
            NotificationType.InformApplicantOfApplicationRefusal,
            It.Is<NotificationRecipient>(a => a.Address == applicant.Email && a.Name == applicant.FullName),
            It.Is<NotificationRecipient[]>(a => a.Length == fcStaff.Count + 1 
                                                && a.Any(x => x.Address == woodlandOwnerModel.ContactEmail && x.Name == woodlandOwnerModel.ContactName) 
                                                && fcStaff.All(fc => a.Any(app => app.Address == fc.Email))),
            null, null,
            CancellationToken.None), Times.Once);

        _publicRegister.Verify(x => x.AddCaseToDecisionRegisterAsync(
                application.PublicRegister!.EsriId!.Value,
                application.ApplicationReference,
                FellingLicenceStatus.Refused.ToString(),
                now,
                It.IsAny<CancellationToken>()),
            Times.Once());

        _auditMock.Verify(v =>
            v.PublishAuditEventAsync(It.Is<AuditEvent>(
                    e => e.EventName == AuditEvents.ApplicationRefused
                         && JsonSerializer.Serialize(e.AuditData, _options) ==
                         JsonSerializer.Serialize(new
                         {
                             application.WoodlandOwnerId,
                             ApplicationAuthorId = application.CreatedById,
                             NotificationSent = false,
                             RefusedByName = _fieldManager.FullName,
                             DecisionPublicRegisterOutcome = SendToDecisionPublicRegisterOutcome.Failure
                         }, _options)),
                CancellationToken.None), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task ApplicationSetToRefused_IsSuccess_ApplicantAndAssignedFcStaffInformed_But_WasExemptedFromPublicRegister(
        FellingLicenceApplication application,
        WoodlandOwnerModel woodlandOwnerModel,
        List<Flo.Services.InternalUsers.Models.UserAccountModel> fcStaff,
        UserAccountModel applicant, 
        UserAccount approver,
        ApproverReviewModel approverReview)
    {
        approverReview.PublicRegisterPublish = false;
        application = PopulateStatusAndAssigneeHistory(application);

        application.PublicRegister!.WoodlandOfficerSetAsExemptFromConsultationPublicRegister = true;

        var sut = CreateSut();

        _getFLAMock.Setup(s => s.GetApplicationByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(application);

        _notificationsMock.Setup(s => s.SendNotificationAsync(It.IsAny<object>(),
                It.IsAny<NotificationType>(), It.IsAny<NotificationRecipient>(), It.IsAny<NotificationRecipient[]?>(),
                It.IsAny<NotificationAttachment[]?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success);

        _woodlandOwnerServiceMock
            .Setup(s => s.RetrieveWoodlandOwnerByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(woodlandOwnerModel);

        _internalAccountServiceMock
            .Setup(s => s.RetrieveUserAccountsByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(fcStaff);

        _internalAccountServiceMock
            .Setup(s => s.GetUserAccountAsync(_fieldManager.UserAccountId.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(approver.AsMaybe());

        _externalAccountServiceMock
            .Setup(s => s.RetrieveUserAccountByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(applicant);

        _approverReviewServiceMock
            .Setup(r => r.GetApproverReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(approverReview);

        var result = await sut.ApproveOrRefuseOrReferApplicationAsync(
            _fieldManager,
            application.Id,
            FellingLicenceStatus.Refused,
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.SubProcessFailures.Should().BeEmpty();

        _getFLAMock.Verify(v => v.GetApplicationByIdAsync(application.Id, CancellationToken.None), Times.Once);
        _woodlandOwnerServiceMock.Verify(v => v.RetrieveWoodlandOwnerByIdAsync(application.WoodlandOwnerId, CancellationToken.None), Times.Once);
        _internalAccountServiceMock.Verify(v => v.RetrieveUserAccountsByIdsAsync(It.Is<List<Guid>>(a =>
                a.TrueForAll(x => application.AssigneeHistories.Where(y => y.Role != AssignedUserRole.Applicant).Select(y => y.AssignedUserId).Contains(x))),
            CancellationToken.None), Times.Once);
        _externalAccountServiceMock.Verify(v => v.RetrieveUserAccountByIdAsync(application.CreatedById, CancellationToken.None), Times.Once);
        _updateFLAMock.Verify(v => v.AddStatusHistoryAsync(It.IsAny<Guid>(), application.Id, FellingLicenceStatus.Refused, CancellationToken.None), Times.Once);

        _notificationsMock.Verify(v => v.SendNotificationAsync(It.Is<InformApplicantOfApplicationRefusalDataModel>(a =>
            a.ApplicationReference == application.ApplicationReference
            && a.PropertyName == application.SubmittedFlaPropertyDetail.Name
            && a.ApproverName == approver.FullName(false)
            && a.ApproverEmail == approver.Email
            && a.ViewApplicationURL == $"{ExternalUrl}FellingLicenceApplication/ApplicationTaskList/{application.Id}"
            && a.AdminHubFooter == AdminHubFooter
            && a.Name == applicant.FullName),
            NotificationType.InformApplicantOfApplicationRefusal,
            It.Is<NotificationRecipient>(a => a.Address == applicant.Email && a.Name == applicant.FullName),
            It.Is<NotificationRecipient[]>(a => a.Length == fcStaff.Count + 1 
                                                && a.Any(x => x.Address == woodlandOwnerModel.ContactEmail && x.Name == woodlandOwnerModel.ContactName) 
                                                && fcStaff.All(fc => a.Any(app => app.Address == fc.Email))),
            null, null,
            CancellationToken.None), Times.Once);

        _publicRegister.Verify(x => x.AddCaseToDecisionRegisterAsync(
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _auditMock.Verify(v =>
            v.PublishAuditEventAsync(It.Is<AuditEvent>(
                    e => e.EventName == AuditEvents.ApplicationRefused
                         && JsonSerializer.Serialize(e.AuditData, _options) ==
                         JsonSerializer.Serialize(new
                         {
                             application.WoodlandOwnerId,
                             ApplicationAuthorId = application.CreatedById,
                             NotificationSent = true,
                             RefusedByName = _fieldManager.FullName,
                             DecisionPublicRegisterOutcome = SendToDecisionPublicRegisterOutcome.Exempt
                         }, _options)),
                CancellationToken.None), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task ApplicationSetToReferredToLocalAuthority_ApplicantAndAssignedFcStaffInformed(
      FellingLicenceApplication application,
      WoodlandOwnerModel woodlandOwnerModel,
      List<Flo.Services.InternalUsers.Models.UserAccountModel> fcStaff,
      UserAccountModel applicant, 
      UserAccount approver,
      string localAuthorityName,
      ApproverReviewModel approverReview)
    {
        approverReview.PublicRegisterPublish = true;
        application = PopulateStatusAndAssigneeHistory(application);

        application.CentrePoint = JsonConvert.SerializeObject(new Point(0, 0));
        
        var submittedDate = application.StatusHistories
            .Where(x => x.Status == FellingLicenceStatus.Submitted)
            .MaxBy(x => x.Created)?.Created;

        var now = DateTime.UtcNow;
        _clockMock.Setup(s => s.GetCurrentInstant()).Returns(Instant.FromDateTimeUtc(now));

        var sut = CreateSut();

        _getFLAMock.Setup(s => s.GetApplicationByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(application);

        _notificationsMock.Setup(s => s.SendNotificationAsync(It.IsAny<object>(),
                It.IsAny<NotificationType>(), It.IsAny<NotificationRecipient>(), It.IsAny<NotificationRecipient[]?>(),
                It.IsAny<NotificationAttachment[]?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success);

        _woodlandOwnerServiceMock
            .Setup(s => s.RetrieveWoodlandOwnerByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(woodlandOwnerModel);

        _internalAccountServiceMock
            .Setup(s => s.RetrieveUserAccountsByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(fcStaff);

        _internalAccountServiceMock
            .Setup(s => s.GetUserAccountAsync(_fieldManager.UserAccountId.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(approver.AsMaybe());

        _externalAccountServiceMock
            .Setup(s => s.RetrieveUserAccountByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(applicant);

        var layer = new Forestry.Flo.Services.Gis.Models.Esri.Responses.Layers.LocalAuthority
        {
            Name = localAuthorityName
        };
        _agolMock.Setup(x => x.GetLocalAuthorityAsync(It.IsAny<Point>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(layer));
    
        _approverReviewServiceMock
            .Setup(r => r.GetApproverReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(approverReview);

        var result = await sut.ApproveOrRefuseOrReferApplicationAsync(
            _fieldManager,
            application.Id,
            FellingLicenceStatus.ReferredToLocalAuthority,
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.SubProcessFailures.Should().BeEmpty();

        _getFLAMock.Verify(v => v.GetApplicationByIdAsync(application.Id, CancellationToken.None), Times.Once);
        _woodlandOwnerServiceMock.Verify(v => v.RetrieveWoodlandOwnerByIdAsync(application.WoodlandOwnerId, CancellationToken.None), Times.Once);
        _internalAccountServiceMock.Verify(v => v.RetrieveUserAccountsByIdsAsync(It.Is<List<Guid>>(a =>
                a.TrueForAll(x => application.AssigneeHistories.Where(y => y.Role != AssignedUserRole.Applicant && y.Role != AssignedUserRole.Author && y.TimestampUnassigned == null)
                    .Select(y => y.AssignedUserId).Contains(x))),
            CancellationToken.None), Times.Exactly(2));
        _externalAccountServiceMock.Verify(v => v.RetrieveUserAccountByIdAsync(application.CreatedById, CancellationToken.None), Times.Once);
        _updateFLAMock.Verify(v => v.AddStatusHistoryAsync(It.IsAny<Guid>(), application.Id, FellingLicenceStatus.ReferredToLocalAuthority, CancellationToken.None), Times.Once);
        _updateFLAMock.Verify(v => v.AddDecisionPublicRegisterDetailsAsync(
            application.Id, 
            now, 
            now.AddDays(28), 
            CancellationToken.None), Times.Once);

        _publicRegister.Verify(x => x.AddCaseToDecisionRegisterAsync(
                application.PublicRegister!.EsriId.Value,
                application.ApplicationReference,
                FellingLicenceStatus.ReferredToLocalAuthority.ToString(),
                now,
                It.IsAny<CancellationToken>()),
            Times.Once());

        _notificationsMock.Verify(v => v.SendNotificationAsync(It.Is<InformApplicantOfApplicationReferredToLocalAuthorityDataModel>(a =>
            a.ApplicationReference == application.ApplicationReference
            && a.PropertyName == application.SubmittedFlaPropertyDetail.Name
            && a.SubmittedDate == (submittedDate.HasValue ? DateTimeDisplay.GetDateDisplayString(submittedDate) : null)
            && a.ApproverName == approver.FullName(false)
            && a.ViewApplicationURL == $"{ExternalUrl}FellingLicenceApplication/ApplicationTaskList/{application.Id}"
            && a.LocalAuthorityName == localAuthorityName
            && a.AdminHubFooter == AdminHubFooter
            && a.Name == applicant.FullName),
            NotificationType.InformApplicantOfApplicationReferredToLocalAuthority,
            It.Is<NotificationRecipient>(a => a.Address == applicant.Email && a.Name == applicant.FullName),
            It.Is<NotificationRecipient[]>(a => a.Length == fcStaff.Count + 1 
                                                && a.Any(x => x.Address == woodlandOwnerModel.ContactEmail && x.Name == woodlandOwnerModel.ContactName) 
                                                && fcStaff.All(fc => a.Any(app => app.Address == fc.Email))),
            null, null,
            CancellationToken.None), Times.Once);

        foreach (var staff in fcStaff)
        {
            _notificationsMock
                .Verify(x => x.SendNotificationAsync(
                    It.Is<InformFcStaffOfApplicationAddedToPublicRegisterDataModel>(m =>
                        m.PropertyName == application.SubmittedFlaPropertyDetail.Name
                        && m.ApplicationReference == application.ApplicationReference
                        && m.PublishedDate == DateTimeDisplay.GetDateDisplayString(now)
                        && m.ExpiryDate == DateTimeDisplay.GetDateDisplayString(now.AddDays(28))
                        && m.AdminHubFooter == AdminHubFooter
                        && m.Name == staff.FullName),
                    NotificationType.InformFcStaffOfApplicationAddedToDecisionPublicRegister,
                    It.Is<NotificationRecipient>(
                        r => r.Name == staff.FullName && r.Address == staff.Email),
                    null,
                    null,
                    null,
                    It.IsAny<CancellationToken>()), Times.Once);
        }

        _notificationsMock.VerifyNoOtherCalls();

        _auditMock.Verify(v =>
            v.PublishAuditEventAsync(It.Is<AuditEvent>(
                    e => e.EventName == AuditEvents.ApplicationReferredToLocalAuthority
                         && JsonSerializer.Serialize(e.AuditData, _options) ==
                         JsonSerializer.Serialize(new
                         {
                             application.WoodlandOwnerId,
                             ApplicationAuthorId = application.CreatedById,
                             NotificationSent = true,
                             ReferredToLocalAuthorityByName = _fieldManager.FullName,
                             DecisionPublicRegisterOutcome = SendToDecisionPublicRegisterOutcome.Success
                         }, _options)),
                CancellationToken.None), Times.Once);
    }

    [Theory]
    [MemberData(nameof(InvalidRequestedStatusesForTransition))]
    public async Task ReturnsFailure_WhenRequestedStatusIsIncorrect(FellingLicenceStatus requestedStatus)
    {
        var sut = CreateSut();

        var result = await sut.ApproveOrRefuseOrReferApplicationAsync(
            _fieldManager,
            Guid.NewGuid(),
            requestedStatus,
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.SubProcessFailures.Single().Should().Be(FinaliseFellingLicenceApplicationProcessOutcomes.IncorrectFellingApplicationStatusRequested);

        _getFLAMock.VerifyNoOtherCalls();
        _notificationsMock.VerifyNoOtherCalls();
        _woodlandOwnerServiceMock.VerifyNoOtherCalls();
        _internalAccountServiceMock.VerifyNoOtherCalls();
        _updateFLAMock.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task ReturnsFailure_WhenApplicationNotFound(Guid applicationId)
    {
        var sut = CreateSut();

        _getFLAMock.Setup(s => s.GetApplicationByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<FellingLicenceApplication>("Application not found"));

        var result = await sut.ApproveOrRefuseOrReferApplicationAsync(
            _fieldManager,
            applicationId,
            FellingLicenceStatus.Refused,
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.SubProcessFailures.Single().Should().Be(FinaliseFellingLicenceApplicationProcessOutcomes.CouldNotRetrieveApplication);

        _getFLAMock.Verify(v => v.GetApplicationByIdAsync(applicationId, CancellationToken.None), Times.Once);

        _notificationsMock.VerifyNoOtherCalls();
        _woodlandOwnerServiceMock.VerifyNoOtherCalls();
        _internalAccountServiceMock.VerifyNoOtherCalls();
        _updateFLAMock.VerifyNoOtherCalls();
    }

    [Theory]
    [MemberData(nameof(InvalidCurrentStatusesForTransition))]
    public async Task ReturnsFailure_WhenApplicationIsNotInTheCorrectStatus(FellingLicenceStatus currentFellingLicenceStatus)
    {
        var sut = CreateSut();
        FixtureInstance.Behaviors.Add(new OmitOnRecursionBehavior());
        var application = FixtureInstance.Create<FellingLicenceApplication>();
        application = PopulateStatusAndAssigneeHistory(application);
        application.StatusHistories[0].Status = currentFellingLicenceStatus;

        _getFLAMock.Setup(s => s.GetApplicationByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(application);
        

        var result = await sut.ApproveOrRefuseOrReferApplicationAsync(
            _fieldManager,
            application.Id,
            FellingLicenceStatus.Refused,// any valid status to be set in the use case.
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.SubProcessFailures.Single().Should().Be(FinaliseFellingLicenceApplicationProcessOutcomes.IncorrectFellingApplicationState);

        _getFLAMock.Verify(v => v.GetApplicationByIdAsync(application.Id, CancellationToken.None),Times.Once);

        _notificationsMock.VerifyNoOtherCalls();
        _woodlandOwnerServiceMock.VerifyNoOtherCalls();
        _internalAccountServiceMock.VerifyNoOtherCalls();
        _updateFLAMock.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task ReturnsFailure_WhenRequestingUserIsNotAssignedFieldManager(FellingLicenceApplication application)
    {
        var sut = CreateSut();

        application = PopulateStatusAndAssigneeHistory(application);
        application.AssigneeHistories.Clear();

        _getFLAMock.Setup(s => s.GetApplicationByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(application);

        var result = await sut.ApproveOrRefuseOrReferApplicationAsync(
            _fieldManager,
            application.Id,
            FellingLicenceStatus.Refused,
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.SubProcessFailures.Single().Should().Be(FinaliseFellingLicenceApplicationProcessOutcomes.UserRoleNotAuthorised);

        _getFLAMock.Verify(v => v.GetApplicationByIdAsync(application.Id, CancellationToken.None), Times.Once);

        _notificationsMock.VerifyNoOtherCalls();
        _woodlandOwnerServiceMock.VerifyNoOtherCalls();
        _internalAccountServiceMock.VerifyNoOtherCalls();
        _updateFLAMock.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task CanUpdateApproverIdOnApplication(
        Guid applicationId,
        Guid? approverId)
    {
        var sut = CreateSut();

        _updateFLAMock.Setup(x =>
                x.SetApplicationApproverAsync(It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success);

        var result = await sut.UpdateApproverIdAsync(applicationId, approverId, CancellationToken.None);

        Assert.True(result.IsSuccess);

        _updateFLAMock.Verify(x => x.SetApplicationApproverAsync(applicationId, approverId, It.IsAny<CancellationToken>()), Times.Once);
    }

    private FellingLicenceApplication PopulateStatusAndAssigneeHistory(FellingLicenceApplication application)
    {
        application.StatusHistories = new List<StatusHistory>
        {
            new()
            {
                Created = DateTime.Today,
                FellingLicenceApplication = application,
                Status = FellingLicenceStatus.SentForApproval
            }
        };

        application.AssigneeHistories.Add(new AssigneeHistory
        {
            AssignedUserId = _fieldManager.UserAccountId!.Value,
            FellingLicenceApplication = application,
            Role = AssignedUserRole.FieldManager,
            TimestampAssigned = DateTime.Today,
        });
        application.PublicRegister!.WoodlandOfficerSetAsExemptFromConsultationPublicRegister = false;

        return application;
    }

    private ApproveRefuseOrReferApplicationUseCase CreateSut()
    {
        _internalAccountServiceMock.Reset();
        _externalAccountServiceMock.Reset();
        _notificationsMock.Reset();
        _woodlandOwnerServiceMock.Reset();
        _getFLAMock.Reset();
        _updateFLAMock.Reset();
        _approverReviewServiceMock.Reset();
        _getConfiguredFcAreasMock.Reset();

        _externalSiteOptionsMock.Setup(s => s.Value).Returns(new ExternalApplicantSiteOptions{BaseUrl = ExternalUrl});
        _getConfiguredFcAreasMock.Setup(x => x.TryGetAdminHubAddress(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AdminHubFooter);

        return new ApproveRefuseOrReferApplicationUseCase(
            new NullLogger<ApproveRefuseOrReferApplicationUseCase>(),
            _getFLAMock.Object,
            _updateFLAMock.Object,
            _notificationsMock.Object,
            _internalAccountServiceMock.Object,
            _externalAccountServiceMock.Object,
            _woodlandOwnerServiceMock.Object,
            _publicRegister.Object,
            _clockMock.Object,
            _externalSiteOptionsMock.Object,
            _auditMock.Object,
            new RequestContext("test", new RequestUserModel(_fieldManager.Principal)),
            _approverReviewServiceMock.Object,
            _getConfiguredFcAreasMock.Object,
            _agolMock.Object
        );
    }

    public static IEnumerable<object[]> InvalidCurrentStatusesForTransition()
    {
        var incorrectStatus = Enum.GetValues<FellingLicenceStatus>()
            .Except(new[] { FellingLicenceStatus.SentForApproval });

        foreach (var incorrectState in incorrectStatus)
        {
            yield return new object[] { incorrectState };
        }
    }

    public static IEnumerable<object[]> InvalidRequestedStatusesForTransition()
    {
        var incorrectStatus = Enum.GetValues<FellingLicenceStatus>()
            .Except(new[] { FellingLicenceStatus.Approved, FellingLicenceStatus.ReferredToLocalAuthority, FellingLicenceStatus.Refused });

        foreach (var incorrectState in incorrectStatus)
        {
            yield return new object[] { incorrectState };
        }
    }
}
