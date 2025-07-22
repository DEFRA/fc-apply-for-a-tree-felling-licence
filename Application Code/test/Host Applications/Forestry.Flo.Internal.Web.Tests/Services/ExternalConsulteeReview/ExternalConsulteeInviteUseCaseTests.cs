using System.Reflection.Metadata;
using AutoFixture;
using CSharpFunctionalExtensions;
using FluentAssertions;
using Forestry.Flo.Internal.Web.Infrastructure;
using Forestry.Flo.Internal.Web.Models.ExternalConsulteeInvite;
using Forestry.Flo.Internal.Web.Services;
using Forestry.Flo.Internal.Web.Services.ExternalConsulteeReview;
using Forestry.Flo.Services.Applicants.Entities.UserAccount;
using Forestry.Flo.Services.Applicants.Models;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.FileStorage.ResultModels;
using Forestry.Flo.Services.FileStorage.Services;
using Forestry.Flo.Services.InternalUsers.Services;
using Forestry.Flo.Services.Notifications.Entities;
using Forestry.Flo.Services.Notifications.Models;
using Forestry.Flo.Services.Notifications.Services;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using NodaTime;
using NodaTime.Testing;
using Document = Forestry.Flo.Services.FellingLicenceApplications.Entities.Document;

namespace Forestry.Flo.Internal.Web.Tests.Services.ExternalConsulteeReview;

public class ExternalConsulteeInviteUseCaseTests
{
    private const int InviteTokenExpiryDays = 5;
    private readonly Mock<IUserAccountService> _internalUserAccountService;
    private readonly Mock<IRetrieveUserAccountsService> _externalUserAccountService;
    private readonly Mock<IFellingLicenceApplicationInternalRepository> _internalUserContextFlaRepository;
    private readonly Mock<IRetrieveWoodlandOwners> _woodlandOwnerService;
    private readonly Mock<ISendNotifications> _emailService;
    private readonly Mock<IFileStorageService> _fileStorageService;
    private readonly Mock<IAuditService<ExternalConsulteeInviteUseCase>> _auditService;
    private readonly Mock<IAgentAuthorityService> _mockAgentAuthorityService;
    private readonly IClock _fakeClock;
    private readonly Mock<IOptions<UserInviteOptions>> _userInviteOptions;
    private readonly Mock<IGetConfiguredFcAreas> _getConfiguredFcAreas;
    private readonly InternalUser _testUser;

    private readonly ExternalConsulteeInviteUseCase _sut;
    private readonly Fixture _fixture;

    private readonly string _commentsEndDate;

    public ExternalConsulteeInviteUseCaseTests()
    {
        _fixture = new Fixture();
        var userPrincipal = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(
            localAccountId: Guid.NewGuid(),
            accountTypeInternal: AccountTypeInternal.AdminOfficer);
        _testUser = new InternalUser(userPrincipal);

         _emailService = new Mock<ISendNotifications>();
         _fileStorageService = new Mock<IFileStorageService>();
         _auditService = new Mock<IAuditService<ExternalConsulteeInviteUseCase>>();
         _fakeClock = new FakeClock(Instant.FromDateTimeUtc(DateTime.UtcNow));
         
         _userInviteOptions = new Mock<IOptions<UserInviteOptions>>();
         _userInviteOptions.Setup(c => c.Value).Returns(new UserInviteOptions { InviteLinkExpiryDays = InviteTokenExpiryDays });

         _getConfiguredFcAreas = new Mock<IGetConfiguredFcAreas>();

        _internalUserAccountService = new Mock<IUserAccountService>();
         _externalUserAccountService = new Mock<IRetrieveUserAccountsService>();
         _woodlandOwnerService = new Mock<IRetrieveWoodlandOwners>();
         _internalUserContextFlaRepository = new Mock<IFellingLicenceApplicationInternalRepository>();
         _mockAgentAuthorityService = new();

         _sut = CreateSut();

        var commentsEndDate = _fakeClock.GetCurrentInstant().ToDateTimeUtc().AddDays(28);
        _commentsEndDate = DateTimeDisplay.GetDateDisplayString(commentsEndDate);
    }

    private ExternalConsulteeInviteUseCase CreateSut()
    {
         var logger = new NullLogger<ExternalConsulteeInviteUseCase>();
         _externalUserAccountService.Setup(r => r.RetrieveUserAccountEntityByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(_fixture.Create<UserAccount>()));
        _woodlandOwnerService.Setup(r => r.RetrieveWoodlandOwnerByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(_fixture.Create<WoodlandOwnerModel>()));
        _internalUserAccountService.Setup(s => s.GetUserAccountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<Flo.Services.InternalUsers.Entities.UserAccount.UserAccount>.From(_fixture.Create<Flo.Services.InternalUsers.Entities.UserAccount.UserAccount>()));

        return new ExternalConsulteeInviteUseCase(
            _internalUserAccountService.Object,
            _externalUserAccountService.Object,
            _internalUserContextFlaRepository.Object,
            _woodlandOwnerService.Object,
            _emailService.Object,
            _fileStorageService.Object,
            _auditService.Object,
            _mockAgentAuthorityService.Object,
            _getConfiguredFcAreas.Object,
            logger,
            _fakeClock,
            _userInviteOptions.Object,
            new RequestContext("test", new RequestUserModel(_testUser.Principal)));
    }

    [Theory, AutoMoqData]
    public async Task ShouldRetrieveExternalConsulteeInviteViewModel_GivenApplicationId(
        FellingLicenceApplication fla, 
        string returnUrl)
    {
        //arrange
        _internalUserContextFlaRepository.Setup(r => r.GetAsync(fla.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.From(fla));

        fla.LinkedPropertyProfile.ProposedFellingDetails = new List<ProposedFellingDetail>();

        //act
        var (isSuccess, _, model) = await _sut.RetrieveExternalConsulteeInviteViewModelAsync(fla.Id, null, returnUrl,CancellationToken.None);

        //assert
        isSuccess.Should().BeTrue();
        model.ExternalConsulteeInvite.Should().NotBeNull();
        model.InviteFormModel.FellingLicenceApplicationSummary!.Id.Should().Be(fla.Id);
        model.InviteFormModel.FellingLicenceApplicationSummary.ApplicationReference.Should().Be(fla.ApplicationReference);
        model.ExternalConsulteeInvite.ConsulteeEmailText.Should().NotBeNullOrEmpty();
        model.ExternalConsulteeInvite.ExternalAccessCode.Should().NotBe(Guid.Empty);
        model.InviteFormModel.FellingLicenceApplicationSummary.Should().NotBeNull();
        model.InviteFormModel.Id.Should().NotBeEmpty();
        model.InviteFormModel.InviteLinks.Should().NotBeEmpty();
        model.InviteFormModel.InviteLinks.Should().HaveCount(fla.ExternalAccessLinks.Count);
        model.InviteFormModel.InviteLinks.First().Id.Should().Be(fla.ExternalAccessLinks.First().Id);
        _internalUserContextFlaRepository.VerifyAll();
    }
    
    [Theory, AutoMoqData]
    public async Task ShouldReturnFailure_WhenRetrieveExternalConsulteeInviteViewModel_GivenApplicationDoesNotExist(
        Guid applicationId, 
        string returnUrl)
    {
        //arrange
        _internalUserContextFlaRepository.Setup(r => r.GetAsync(applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.None);

        //act
        var (isSuccess, _, _) = await _sut.RetrieveExternalConsulteeInviteViewModelAsync(applicationId, null, returnUrl,CancellationToken.None);

        //assert
        _internalUserContextFlaRepository.VerifyAll();
        isSuccess.Should().BeFalse();
    }

    [Theory,AutoMoqData]
    public async Task ShouldReturnTrue_WhenCheckIfEmailHasAlreadyBeenSentToConsultee_GivenAlreadySentInviteModel(
        List<ExternalAccessLink> links, ExternalConsulteeInviteFormModel inviteModel)
    {
        //arrange
        _internalUserContextFlaRepository.Setup(r =>
                r.GetUserExternalAccessLinksByApplicationIdAndPurposeAsync(
                    inviteModel.ApplicationId, inviteModel.ConsulteeName, inviteModel.Email,
                    inviteModel.Purpose!, It.IsAny<CancellationToken>()))
            .ReturnsAsync(links);

        //act
        var (isSuccess, _, value) = await _sut.CheckIfEmailHasAlreadyBeenSentToConsulteeForThisPurposeAsync(inviteModel,
            cancellationToken: CancellationToken.None);

        //assert
        isSuccess.Should().BeTrue();
        value.Should().BeTrue();
        _internalUserContextFlaRepository.VerifyAll();
    }
    
    [Theory,AutoMoqData]
    public async Task ShouldReturnFalse_WhenCheckIfEmailHasAlreadyBeenSentToConsultee_GivenNotSentInviteModel(
        ExternalConsulteeInviteFormModel inviteModel)
    {
        //arrange
        _internalUserContextFlaRepository.Setup(r =>
                r.GetUserExternalAccessLinksByApplicationIdAndPurposeAsync(
                    inviteModel.ApplicationId, inviteModel.ConsulteeName, inviteModel.Email,
                    inviteModel.Purpose!, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ExternalAccessLink>());

        //act
        var (isSuccess, _, value) = await _sut.CheckIfEmailHasAlreadyBeenSentToConsulteeForThisPurposeAsync(inviteModel,
            cancellationToken: CancellationToken.None);

        //assert
        isSuccess.Should().BeTrue();
        value.Should().BeFalse();
        _internalUserContextFlaRepository.VerifyAll();
    }
    
    [Theory,AutoMoqData]
    public async Task ShouldReturnFailure_WhenCheckIfEmailHasAlreadyBeenSentToConsultee_GivenDatabaseError(
        ExternalConsulteeInviteFormModel inviteModel)
    {
        //arrange
        _internalUserContextFlaRepository.Setup(r =>
                r.GetUserExternalAccessLinksByApplicationIdAndPurposeAsync(
                    inviteModel.ApplicationId, inviteModel.ConsulteeName, inviteModel.Email,
                    inviteModel.Purpose!, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("error"));

        //act
        var (isSuccess, _, _) = await _sut.CheckIfEmailHasAlreadyBeenSentToConsulteeForThisPurposeAsync(inviteModel,
            cancellationToken: CancellationToken.None);

        //assert
        isSuccess.Should().BeFalse();
        _internalUserContextFlaRepository.VerifyAll();
    }

    [Theory, AutoMoqData]
    public async Task ShouldSaveAccessLinkWithSuccess_GivenValidInviteModel(
        ExternalConsulteeInviteModel inviteModel, FellingLicenceApplication fla)
    {
        //arrange
        _internalUserContextFlaRepository.Setup(r => r.GetAsync(fla.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.From(fla));
        fla.Documents!.Clear();

        //act
        var (isSuccess, _) = await _sut.InviteExternalConsulteeAsync(inviteModel, fla.Id, _testUser, CancellationToken.None);

        //assert
        isSuccess.Should().BeTrue();
        _internalUserContextFlaRepository.Verify(r => r.AddExternalAccessLinkAsync(It.Is<ExternalAccessLink>(l =>
            l.Name == inviteModel.ConsulteeName
            && l.Purpose == inviteModel.Purpose
            && l.AccessCode == inviteModel.ExternalAccessCode
            && l.ContactEmail == inviteModel.Email
            && l.FellingLicenceApplicationId == fla.Id
            && l.CreatedTimeStamp == _fakeClock.GetCurrentInstant().ToDateTimeUtc()
            && l.ExpiresTimeStamp == _fakeClock.GetCurrentInstant().ToDateTimeUtc().AddDays(InviteTokenExpiryDays)
            && l.IsMultipleUseAllowed
        ), It.IsAny<CancellationToken>()), Times.Once);
    }
    
    [Theory, AutoMoqData]
    public async Task ShouldUpdateAccessLinkWithSuccess_GivenValidInviteModel_AndAlreadyInvitedConsulteeDetails(
        ExternalConsulteeInviteModel inviteModel, FellingLicenceApplication fla)
    {
        //arrange
        _internalUserContextFlaRepository.Setup(r => r.GetAsync(fla.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.From(fla));
        var existingLink = fla.ExternalAccessLinks.First();
        inviteModel = inviteModel with { ConsulteeName = existingLink.Name, Email = existingLink.ContactEmail, Purpose = existingLink.Purpose };
        fla.Documents!.Clear();

        //act
        var (isSuccess, _) = await _sut.InviteExternalConsulteeAsync(inviteModel, fla.Id, _testUser, CancellationToken.None);

        //assert
        isSuccess.Should().BeTrue();
        _internalUserContextFlaRepository.Verify(r => r.UpdateExternalAccessLinkAsync(It.Is<ExternalAccessLink>(l =>
            l.Name == inviteModel.ConsulteeName
            && l.Purpose == inviteModel.Purpose
            && l.AccessCode == inviteModel.ExternalAccessCode
            && l.ContactEmail == inviteModel.Email
            && l.FellingLicenceApplicationId == fla.Id
            && l.CreatedTimeStamp == existingLink.CreatedTimeStamp
            && l.ExpiresTimeStamp == _fakeClock.GetCurrentInstant().ToDateTimeUtc().AddDays(InviteTokenExpiryDays)
            && l.IsMultipleUseAllowed
        ), It.IsAny<CancellationToken>()), Times.Once);
    }
    
    [Theory, AutoMoqData]
    public async Task ShouldInviteExternalConsulteeWithSuccess_GivenValidInviteModel(
        ExternalConsulteeInviteModel inviteModel, FellingLicenceApplication fla, string adminHubFooter)
    {
        //arrange
        _internalUserContextFlaRepository.Setup(r => r.GetAsync(fla.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.From(fla));
        fla.Documents!.Clear();

        _getConfiguredFcAreas
            .Setup(x => x.TryGetAdminHubAddress(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(adminHubFooter);
        
        //act
        var (isSuccess, _) = await _sut.InviteExternalConsulteeAsync(inviteModel, fla.Id,_testUser, CancellationToken.None);

        //assert
        isSuccess.Should().BeTrue();
        _emailService.Verify(s => s.SendNotificationAsync(It.Is<ExternalConsulteeInviteDataModel>(m =>
                m.ApplicationReference == fla.ApplicationReference
                && m.ConsulteeName == inviteModel.ConsulteeName
                && m.EmailText == inviteModel.ConsulteeEmailText
                && m.ViewApplicationURL == inviteModel.ExternalAccessLink
                && m.SenderName == _testUser.FullName
                && m.SenderEmail == _testUser.EmailAddress
                && m.AdminHubFooter == adminHubFooter
                && m.CommentsEndDate == _commentsEndDate
            ), NotificationType.ExternalConsulteeInvite, It.Is<NotificationRecipient>(r =>
                r.Name == inviteModel.ConsulteeName
                && r.Address == inviteModel.Email),
            It.IsAny<NotificationRecipient[]?>(), It.IsAny<NotificationAttachment[]?>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Once);
        _auditService.Verify(a => a.PublishAuditEventAsync(It.Is<AuditEvent>(ev => 
            ev.EventName == AuditEvents.ExternalConsulteeInvitationSent
            && ev.SourceEntityId == fla.Id
            && ev.UserId == _testUser.UserAccountId), It.IsAny<CancellationToken>()), Times.Once);
    }
    
    [Theory, AutoMoqData]
    public async Task ShouldAttacheSupportingDocuments_WhenInviteExternalConsultee_GivenValidInviteModel(
        ExternalConsulteeInviteModel inviteModel, 
        GetFileSuccessResult fileResult, 
        FellingLicenceApplication fla,
        string adminHubFooter)
    {
        //arrange
        foreach (var flaDocument in fla.Documents)
        {
            flaDocument.VisibleToConsultee = true;
        }
        inviteModel.SelectedDocumentIds = fla.Documents.Select(x => x.Id).ToList();
        

        _internalUserContextFlaRepository.Setup(r => r.GetAsync(fla.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.From(fla));
        _fileStorageService.Setup(s => s.GetFileAsync(It.Is<string>(n => 
               fla.Documents!.Any(d => d.Location == n)), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<GetFileSuccessResult,FileAccessFailureReason>(fileResult));

        _getConfiguredFcAreas
            .Setup(x => x.TryGetAdminHubAddress(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(adminHubFooter);

        //act
        var (isSuccess, _) = await _sut.InviteExternalConsulteeAsync(inviteModel, fla.Id,_testUser, CancellationToken.None);

        //assert
        isSuccess.Should().BeTrue();
        _emailService.Verify(s => s.SendNotificationAsync(It.Is<ExternalConsulteeInviteDataModel>(m =>
                m.ApplicationReference == fla.ApplicationReference
                && m.ConsulteeName == inviteModel.ConsulteeName
                && m.EmailText == inviteModel.ConsulteeEmailText
                && m.SenderName == _testUser.FullName
                && m.ViewApplicationURL == inviteModel.ExternalAccessLink
                && m.SenderName == _testUser.FullName
                && m.SenderEmail == _testUser.EmailAddress
                && m.AdminHubFooter == adminHubFooter
                && m.CommentsEndDate == _commentsEndDate
            ), NotificationType.ExternalConsulteeInvite, It.Is<NotificationRecipient>(r =>
                r.Name == inviteModel.ConsulteeName
                && r.Address == inviteModel.Email),
            It.IsAny<NotificationRecipient[]?>(),
            It.Is<NotificationAttachment[]?>(l => 
                l!.Any(a => fla.Documents!.Any(d => d.FileName == a.FileName))),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Once);
        _fileStorageService.VerifyAll();
    }
    
    [Theory, AutoMoqData]
    public async Task ShouldNotInviteExternalConsultee_WhenAccessLinkWasNotSaved(
        ExternalConsulteeInviteModel inviteModel, FellingLicenceApplication fla, string adminHubFooter)
    {
        //arrange
        _internalUserContextFlaRepository.Setup(r => r.GetAsync(fla.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.From(fla));
        fla.Documents!.Clear();
        _internalUserContextFlaRepository.Setup(r =>
                r.AddExternalAccessLinkAsync(It.IsAny<ExternalAccessLink>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Failure(UserDbErrorReason.General));

        _getConfiguredFcAreas
            .Setup(x => x.TryGetAdminHubAddress(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(adminHubFooter);

        //act
        var (isSuccess, _) = await _sut.InviteExternalConsulteeAsync(inviteModel, fla.Id,_testUser, CancellationToken.None);

        //assert
        isSuccess.Should().BeFalse();
        _emailService.Verify(s => s.SendNotificationAsync(
            It.IsAny<ExternalConsulteeInviteDataModel>(), 
            It.IsAny<NotificationType>(), 
            It.IsAny<NotificationRecipient>(), 
            It.IsAny<NotificationRecipient[]?>(), It.IsAny<NotificationAttachment[]?>(),
            It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
        _auditService.Verify(a => a.PublishAuditEventAsync(It.Is<AuditEvent>(ev => 
            ev.EventName == AuditEvents.ExternalConsulteeInvitationFailure), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task ShouldCreateExternalConsulteeInviteConfirmation_GivenConsulteeInviteModel(
        ExternalConsulteeInviteModel inviteModel, string emailContent, FellingLicenceApplication fla,
        string returnUrl)
    {
        //arrange
        _internalUserContextFlaRepository.Setup(r => r.GetAsync(fla.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.From(fla));
        fla.Documents!.Clear();
        inviteModel.SelectedDocumentIds =
            fla.Documents.Select(d => d.Id).ToList();
        _emailService
            .Setup(n => n.CreateNotificationContentAsync(It.IsAny<ExternalConsulteeInviteDataModel>(), NotificationType.ExternalConsulteeInvite, It.IsAny<NotificationAttachment[]?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(emailContent));

        fla.LinkedPropertyProfile.ProposedFellingDetails = new List<ProposedFellingDetail>();

        //act
        var (isSuccess, _,  value) = await _sut.CreateExternalConsulteeInviteConfirmationAsync(fla.Id, returnUrl ,inviteModel,_testUser, CancellationToken.None);

        //assert
        isSuccess.Should().BeTrue();
        value.Email.Should().Be(inviteModel.Email);
        value.Id.Should().Be(inviteModel.Id);
        value.ApplicationId.Should().Be(fla.Id);
        value.AttachedDocuments.Should().HaveCount(fla.Documents.Count);
        value.EmailContent.Should().Be(emailContent);
        value.PreviewEmailContent.Should().NotContain("<a");
        value.FellingLicenceApplicationSummary.Should().NotBeNull();
        value.ReturnUrl.Should().Be(returnUrl);
        _emailService.VerifyAll();
    }
    
    [Theory, AutoMoqData]
    public async Task ShouldFail_WhenCreateExternalConsulteeInviteConfirmation_GivenEmailContentNotCreated(
        ExternalConsulteeInviteModel inviteModel, FellingLicenceApplication fla, string returnUrl)
    {
        //arrange
        _internalUserContextFlaRepository.Setup(r => r.GetAsync(fla.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.From(fla));
        fla.Documents!.Clear();
        inviteModel.SelectedDocumentIds =
            fla.Documents.Select(d => d.Id).ToList();
        _emailService
            .Setup(n => n.CreateNotificationContentAsync(It.IsAny<ExternalConsulteeInviteDataModel>(), NotificationType.ExternalConsulteeInvite, It.IsAny<NotificationAttachment[]?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<string>("Error"));

        //act
        var (isSuccess, _,  _) = await _sut.CreateExternalConsulteeInviteConfirmationAsync(fla.Id, returnUrl, inviteModel, _testUser, CancellationToken.None);

        //assert
        isSuccess.Should().BeFalse();
    }
    
    [Theory, AutoMoqData]
    public async Task ShouldRetrieveExternalConsulteeEmailTextViewModel_GivenApplicationId_AndInviteLink(
        FellingLicenceApplication fla, string returnUrl, ExternalConsulteeInviteModel inviteModel)
    {
        //arrange
        _internalUserContextFlaRepository.Setup(r => r.GetAsync(fla.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.From(fla));

        fla.LinkedPropertyProfile.ProposedFellingDetails = new List<ProposedFellingDetail>();

        //act
        var (isSuccess, _, model) = await _sut.RetrieveExternalConsulteeEmailTextViewModelAsync(fla.Id, returnUrl, inviteModel, CancellationToken.None);

        //assert
        isSuccess.Should().BeTrue();
        model.FellingLicenceApplicationSummary.Should().NotBeNull();
        model.FellingLicenceApplicationSummary!.Id.Should().Be(fla.Id);
        model.FellingLicenceApplicationSummary.ApplicationReference.Should().Be(fla.ApplicationReference);
        model.ConsulteeEmailText.Should().Be(inviteModel.ConsulteeEmailText);
        model.Email.Should().Be(inviteModel.Email);
        model.Id.Should().Be(inviteModel.Id);
        model.ApplicationId.Should().Be(fla.Id);
        model.ConsulteeName.Should().Be(inviteModel.ConsulteeName);
        model.ReturnUrl.Should().Be(returnUrl);
        model.ApplicationDocumentsCount.Should().Be(fla.Documents!.Count);
        _internalUserContextFlaRepository.VerifyAll();
    }
    
    [Theory, AutoMoqData]
    public async Task ShouldReturnFailure_WhenRetrieveExternalConsulteeEmailTextViewModel_GivenApplicationDoesNotExist(
        Guid applicationId, string returnUrl, ExternalConsulteeInviteModel inviteModel)
    {
        //arrange
        _internalUserContextFlaRepository.Setup(r => r.GetAsync(applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.None);

        //act
        var (isSuccess, _, model) = await _sut.RetrieveExternalConsulteeEmailTextViewModelAsync(applicationId, returnUrl, inviteModel, CancellationToken.None);

        //assert
        _internalUserContextFlaRepository.VerifyAll();
        isSuccess.Should().BeFalse();
    }
    
    [Theory, AutoMoqData]
    public async Task ShouldRetrieveExternalConsulteeEmailDocumentsViewModel_GivenApplicationId_AndInviteLink(
        FellingLicenceApplication fla, 
        Document visibleDocument,
        Document notVisibleDocument,
        string returnUrl, 
        ExternalConsulteeInviteModel inviteModel)
    {
        //arrange

        visibleDocument.VisibleToConsultee = true;
        notVisibleDocument.VisibleToConsultee = false;
        fla.Documents = new List<Document> { visibleDocument, notVisibleDocument };

        fla.LinkedPropertyProfile.ProposedFellingDetails = new List<ProposedFellingDetail>();

        _internalUserContextFlaRepository.Setup(r => r.GetAsync(fla.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.From(fla));

        //act
        var (isSuccess, _, model) = await _sut.RetrieveExternalConsulteeEmailDocumentsViewModelAsync(fla.Id, returnUrl, inviteModel, CancellationToken.None);

        //assert
        isSuccess.Should().BeTrue();
        model.FellingLicenceApplicationSummary.Should().NotBeNull();
        model.FellingLicenceApplicationSummary!.Id.Should().Be(fla.Id);
        model.FellingLicenceApplicationSummary.ApplicationReference.Should().Be(fla.ApplicationReference);
        model.SelectedDocumentIds.Should().NotBeNull();
        model.SelectedDocumentIds.Should().BeEquivalentTo(inviteModel.SelectedDocumentIds);
        model.Email.Should().Be(inviteModel.Email);
        model.Id.Should().Be(inviteModel.Id);
        model.ApplicationId.Should().Be(fla.Id);
        model.ConsulteeName.Should().Be(inviteModel.ConsulteeName);
        model.ReturnUrl.Should().Be(returnUrl);
        model.SupportingDocuments.Count.Should().Be(1);
        model.SupportingDocuments.Single().Id.Should().Be(visibleDocument.Id);
        model.SupportingDocuments.Single().FileName.Should().Be(visibleDocument.FileName);
        model.SupportingDocuments.Single().MimeType.Should().Be(visibleDocument.MimeType);
        model.SupportingDocuments.Single().Purpose.Should().Be(visibleDocument.Purpose);
        model.SupportingDocuments.Single().CreatedTimestamp.Should().Be(visibleDocument.CreatedTimestamp);
        model.SupportingDocuments.Single().Location.Should().Be(visibleDocument.Location);
        _internalUserContextFlaRepository.VerifyAll();
    }
    
    [Theory, AutoMoqData]
    public async Task ShouldReturnFailure_WhenRetrieveExternalConsulteeEmailDocumentsViewModel_GivenApplicationDoesNotExist(
        Guid applicationId, string returnUrl, ExternalConsulteeInviteModel inviteModel)
    {
        //arrange
        _internalUserContextFlaRepository.Setup(r => r.GetAsync(applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.None);

        //act
        var (isSuccess, _, model) = await _sut.RetrieveExternalConsulteeEmailTextViewModelAsync(applicationId, returnUrl, inviteModel, CancellationToken.None);

        //assert
        _internalUserContextFlaRepository.VerifyAll();
        isSuccess.Should().BeFalse();
    }
    
    [Theory, AutoMoqData]
    public async Task ShouldRetrieveExternalConsulteeReInviteViewModel_GivenApplicationId_AndInviteLink(
        FellingLicenceApplication fla, string returnUrl, ExternalConsulteeInviteModel inviteModel)
    {
        //arrange
        _internalUserContextFlaRepository.Setup(r => r.GetAsync(fla.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.From(fla));

        fla.LinkedPropertyProfile.ProposedFellingDetails = new List<ProposedFellingDetail>();

        //act
        var (isSuccess, _, model) = await _sut.RetrieveExternalConsulteeReInviteViewModelAsync(fla.Id, returnUrl, inviteModel, CancellationToken.None);

        //assert
        isSuccess.Should().BeTrue();
        model.FellingLicenceApplicationSummary.Should().NotBeNull();
        model.FellingLicenceApplicationSummary!.Id.Should().Be(fla.Id);
        model.FellingLicenceApplicationSummary.ApplicationReference.Should().Be(fla.ApplicationReference);
        model.Email.Should().Be(inviteModel.Email);
        model.Id.Should().Be(inviteModel.Id);
        model.ApplicationId.Should().Be(fla.Id);
        model.ConsulteeName.Should().Be(inviteModel.ConsulteeName);
        model.ReturnUrl.Should().Be(returnUrl);
        model.Purpose.Should().Be(inviteModel.Purpose);
        _internalUserContextFlaRepository.VerifyAll();
    }
    
    [Theory, AutoMoqData]
    public async Task ShouldReturnFailure_WhenRetrieveExternalConsulteeReInviteViewModel_GivenApplicationDoesNotExist(
        Guid applicationId, string returnUrl, ExternalConsulteeInviteModel inviteModel)
    {
        //arrange
        _internalUserContextFlaRepository.Setup(r => r.GetAsync(applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.None);

        //act
        var (isSuccess, _, model) = await _sut.RetrieveExternalConsulteeReInviteViewModelAsync(applicationId, returnUrl, inviteModel, CancellationToken.None);

        //assert
        _internalUserContextFlaRepository.VerifyAll();
        isSuccess.Should().BeFalse();
    }
    [Theory, AutoMoqData]
    public async Task ShouldRetrieveApplicationSummaryModel_GivenApplicationId_AndInviteLink(
        FellingLicenceApplication fla)
    {
        //arrange
        _internalUserContextFlaRepository.Setup(r => r.GetAsync(fla.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.From(fla));

        fla.LinkedPropertyProfile.ProposedFellingDetails = new List<ProposedFellingDetail>();

        //act
        var (isSuccess, _, model) = await _sut.RetrieveApplicationSummaryAsync(fla.Id, CancellationToken.None);

        //assert
        isSuccess.Should().BeTrue();
        model.Should().NotBeNull();
        model.Id.Should().Be(fla.Id);
        model.ApplicationReference.Should().Be(fla.ApplicationReference);
        _internalUserContextFlaRepository.VerifyAll();
    }
    
    [Theory, AutoMoqData]
    public async Task ShouldReturnFailure_WhenRetrieveApplicationSummaryModel_GivenApplicationDoesNotExist(
        Guid applicationId)
    {
        //arrange
        _internalUserContextFlaRepository.Setup(r => r.GetAsync(applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.None);

        //act
        var (isSuccess, _, model) = await _sut.RetrieveApplicationSummaryAsync(applicationId, CancellationToken.None);

        //assert
        _internalUserContextFlaRepository.VerifyAll();
        isSuccess.Should().BeFalse();
    }
}