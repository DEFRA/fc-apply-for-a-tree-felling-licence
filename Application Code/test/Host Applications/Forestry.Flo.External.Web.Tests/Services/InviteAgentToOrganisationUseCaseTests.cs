using AutoFixture;
using AutoFixture.Xunit2;
using CSharpFunctionalExtensions;
using Forestry.Flo.External.Web.Infrastructure;
using Forestry.Flo.External.Web.Models.UserAccount;
using Forestry.Flo.External.Web.Services;
using Forestry.Flo.Services.Applicants.Entities.Agent;
using Forestry.Flo.Services.Applicants.Entities.UserAccount;
using Forestry.Flo.Services.Applicants.Repositories;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.Notifications.Entities;
using Forestry.Flo.Services.Notifications.Models;
using Forestry.Flo.Services.Notifications.Services;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NodaTime;
using NodaTime.Testing;

namespace Forestry.Flo.External.Web.Tests.Services;

public class InviteAgentUserToOrganisationUseCaseTests
{
    private const int InviteTokenExpiryDays = 5;
    private InviteAgentToOrganisationUseCase _sut = null!;
    private readonly Mock<IAuditService<AgencyUserModel>> _mockAuditService;
    private readonly ExternalApplicant _externalApplicant;
    private readonly Mock<ISendNotifications> _sendNotifications;
    private readonly Mock<IOptions<UserInviteOptions>> _userInviteOptions;
    private readonly Mock<IUserAccountRepository> _userAccountRepository;
    private readonly Mock<IAgencyRepository> _agencyRepository;
    private readonly InvitedUserValidator _invitedUserValidator;
    private static readonly DateTime UtcNow = DateTime.UtcNow;
    private readonly IClock _fixedTimeClock;
    private readonly Agency _agency;
    private readonly Mock<IUnitOfWork> _unitOfWOrkMock;
    private readonly Fixture _fixture;

    public InviteAgentUserToOrganisationUseCaseTests()
    {
        _fixture = new Fixture();
        _mockAuditService = new Mock<IAuditService<AgencyUserModel>>();
        _sendNotifications = new Mock<ISendNotifications>();
        _fixedTimeClock = new FakeClock(Instant.FromDateTimeUtc(UtcNow));
        _userInviteOptions = new Mock<IOptions<UserInviteOptions>>();
        _userAccountRepository = new Mock<IUserAccountRepository>();
        _agencyRepository = new Mock<IAgencyRepository>();
        _invitedUserValidator = new InvitedUserValidator(new NullLogger<InvitedUserValidator>(), _fixedTimeClock);
        _unitOfWOrkMock = new Mock<IUnitOfWork>();
        _userInviteOptions.Setup(c => c.Value).Returns(new UserInviteOptions { InviteLinkExpiryDays = InviteTokenExpiryDays });
        
        _agency = _fixture.Build<Agency>()
            .With(wo => wo.Id, Guid.NewGuid)
            .Create();
        
        var user = UserFactory.CreateExternalApplicantIdentityProviderClaimsPrincipal(
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            _fixture.Create<Guid>(),
             accountTypeExternal:AccountTypeExternal.AgentAdministrator,
            agencyId: _agency.Id);
        _externalApplicant = new ExternalApplicant(user);

        _userAccountRepository.SetupGet(r => r.UnitOfWork).Returns(_unitOfWOrkMock.Object);
        _agencyRepository.Setup(r => r.GetAsync(_agency.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync( Result.Success<Agency, UserDbErrorReason>(_agency));
    }
    
    private InviteAgentToOrganisationUseCase CreateSut() =>
        new(_mockAuditService.Object,
            _userAccountRepository.Object,
            _agencyRepository.Object,
            new NullLogger<InviteAgentToOrganisationUseCase>(),
            _sendNotifications.Object,
            _fixedTimeClock,
            _userInviteOptions.Object,
            new RequestContext("test", new RequestUserModel(_externalApplicant.Principal)),
            _invitedUserValidator);

    [Theory]
    [InlineAutoData(AgencyUserRole.AgencyUser,AccountTypeExternal.Agent)]
    [InlineAutoData( AgencyUserRole.AgencyAdministrator ,AccountTypeExternal.AgentAdministrator)]
    public async Task ShouldRegisterNewInvitedAgentUser_GivenValidNewUserModel(AgencyUserRole agencyUserRole, AccountTypeExternal accountTypeExternal, AgencyUserModel model, string url)
    {
        //arrange
        model.AgencyId = _agency.Id;
        model.AgencyName = _agency.OrganisationName!;
        model.AgencyUserRole = agencyUserRole;
        model.Email = "valid@email.com";
        _sut = CreateSut();

        //act
        var result = await _sut.InviteAgentToOrganisationAsync(model, _externalApplicant, url, CancellationToken.None);

        //assert
        Assert.True(result.IsSuccess);
        _userAccountRepository.Verify(r => r.Add( It.Is<UserAccount>(u =>
            u.Email == model.Email
            && u.AgencyId == model.AgencyId
            && u.Status == UserAccountStatus.Invited
            && u.InviteTokenExpiry == UtcNow.AddDays(InviteTokenExpiryDays)
            && u.AccountType == accountTypeExternal
            && !string.IsNullOrEmpty(u.InviteToken.ToString()))), Times.Once());
        
        _unitOfWOrkMock.Verify(c => c.SaveEntitiesAsync(It.IsAny<CancellationToken>()));
        _mockAuditService.Verify(
            a => a.PublishAuditEventAsync(It.Is<AuditEvent>(e =>
                    e.EventName == AuditEvents.AgencyUserInvitationSent),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
    
    [Theory, AutoData]
    public async Task ShouldSendInvitationEmail_GivenValidNewAgentUserModel(AgencyUserModel model, string url)
    {
        //arrange
        model.AgencyId = _agency.Id;
        model.AgencyName = _agency.OrganisationName!;
        _sut = CreateSut();

        //act
        var result = await _sut.InviteAgentToOrganisationAsync(model, _externalApplicant, url, CancellationToken.None);

        //assert
        Assert.True(result.IsSuccess);
        _sendNotifications.Verify(s => s.SendNotificationAsync(
            It.Is<InviteAgentToOrganisationDataModel>(m => m.Name == model.Name
                                                           && m.InviteLink.Contains(url)), 
            NotificationType.InviteAgentUserToOrganisation, 
            It.Is<NotificationRecipient>(r => r.Address == model.Email),
            null, null, _externalApplicant.FullName, CancellationToken.None), Times.Once);
        _mockAuditService.Verify(
            a => a.PublishAuditEventAsync(It.Is<AuditEvent>(e =>
                    e.EventName == AuditEvents.AgencyUserInvitationSent),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
    
    [Theory, AutoData]
    public async Task ShouldReturnFailedResult_WhenNewAgentUserIsInvited_AndEmailSendingFailed(AgencyUserModel model, string url, string error)
    {
        //arrange
        model.AgencyId = _agency.Id;
        model.AgencyName = _agency.OrganisationName!;
        _sendNotifications.Setup(s => s.SendNotificationAsync(It.IsAny<InviteAgentToOrganisationDataModel>(),
            It.IsAny<NotificationType>(), It.IsAny<NotificationRecipient>(), It.IsAny<NotificationRecipient[]?>(),
            It.IsAny<NotificationAttachment[]?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(error));
        _sut = CreateSut();

        //act
        var result = await _sut.InviteAgentToOrganisationAsync(model, _externalApplicant, url, CancellationToken.None);

        //assert
        Assert.False(result.IsSuccess);
        Assert.Equal(error, result.Error.Message);
        _mockAuditService.Verify(
            a => a.PublishAuditEventAsync(It.Is<AuditEvent>(e =>
                    e.EventName == AuditEvents.AgencyUserInvitationFailure),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
    
    [Theory, AutoData]
    public async Task ShouldReturnAlreadyExistsError_WhenInvitedAgentUserExistsAndActive(AgencyUserModel model, string url, UserAccount userAccount)
    {
        //arrange
        model.AgencyId = _agency.Id;
        model.AgencyName = _agency.OrganisationName!;
        userAccount.Status = UserAccountStatus.Active;
        _sut = CreateSut();
        _userAccountRepository.Setup(r => r.GetByEmailAsync(model.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<UserAccount, UserDbErrorReason>(userAccount));
        _unitOfWOrkMock.Setup(r => r.SaveEntitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => UnitResult.Failure(UserDbErrorReason.NotUnique));

        //act
        var result = await _sut.InviteAgentToOrganisationAsync(model, _externalApplicant, url, CancellationToken.None);

        //assert
        Assert.False(result.IsSuccess);
        Assert.Equal(InviteUserErrorResult.UserAlreadyExists, result.Error.ErrorResult);
        _mockAuditService.Verify(
            a => a.PublishAuditEventAsync(It.Is<AuditEvent>(e =>
                    e.EventName == AuditEvents.AgencyUserInvitationFailure),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
    
    [Theory, AutoData]
    public async Task ShouldReturnAlreadyExistsError_WhenInvitedAgentUserExistsAndInvitedByAnotherWoodlandOwner(AgencyUserModel model, string url, UserAccount userAccount, Guid? anotherWoodlandOwner)
    {
        //arrange
        model.AgencyId = _agency.Id;
        model.AgencyName = _agency.OrganisationName!;
        userAccount.Status = UserAccountStatus.Invited;
        userAccount.WoodlandOwnerId = anotherWoodlandOwner;
        _sut = CreateSut();
        _userAccountRepository.Setup(r => r.GetByEmailAsync(model.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<UserAccount, UserDbErrorReason>(userAccount));
        _unitOfWOrkMock.Setup(r => r.SaveEntitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => UnitResult.Failure(UserDbErrorReason.NotUnique));

        //act
        var result = await _sut.InviteAgentToOrganisationAsync(model, _externalApplicant, url, CancellationToken.None);

        //assert
        Assert.False(result.IsSuccess);
        Assert.Equal(InviteUserErrorResult.UserAlreadyExists, result.Error.ErrorResult);
        _mockAuditService.Verify(
            a => a.PublishAuditEventAsync(It.Is<AuditEvent>(e =>
                    e.EventName == AuditEvents.AgencyUserInvitationFailure),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
    
    [Theory, AutoData]
    public async Task ShouldReturnAlreadyInvitedError_WhenInvitedAgentUserExistsAndInvited(AgencyUserModel model, string url, UserAccount userAccount)
    {
        //arrange
        model.AgencyId = _agency.Id;
        model.AgencyName = _agency.OrganisationName!;
        userAccount.Status = UserAccountStatus.Invited;
        userAccount.AgencyId = model.AgencyId;
        _sut = CreateSut();
        _userAccountRepository.Setup(r => r.GetByEmailAsync(model.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<UserAccount, UserDbErrorReason>(userAccount));
        _unitOfWOrkMock.Setup(r => r.SaveEntitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => UnitResult.Failure(UserDbErrorReason.NotUnique));

        //act
        var result = await _sut.InviteAgentToOrganisationAsync(model, _externalApplicant, url, CancellationToken.None);

        //assert
        Assert.False(result.IsSuccess);
        Assert.Equal(InviteUserErrorResult.UserAlreadyInvited, result.Error.ErrorResult);
        _mockAuditService.Verify(
            a => a.PublishAuditEventAsync(It.Is<AuditEvent>(e =>
                    e.EventName == AuditEvents.AgencyUserInvitationFailure),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory, AutoData]
    public async Task ShouldThrowException_WhenInviteNewAgentUser_GivenUserModelIsNull(string url)
    {
        //arrange
        _sut = CreateSut();
        
        //act
        Func<Task<Result<UserAccount, InviteUserErrorDetails>>> act = async () => 
            await _sut.InviteAgentToOrganisationAsync(null!, _externalApplicant, url, CancellationToken.None);

        //assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await act());
    }
    
    [Theory, AutoData]
    public async Task ShouldThrowException_WhenInviteNewAgentUser_GivenSystemUserIsNull(AgencyUserModel model, string url)
    {
        //arrange
        _sut = CreateSut();
        
        //act
        Func<Task<Result<UserAccount, InviteUserErrorDetails>>> act = async () =>
            await _sut.InviteAgentToOrganisationAsync(model, null!, url, CancellationToken.None);

        //assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await act());
    }
    
    [Theory, AutoData]
    public async Task ShouldThrowException_WhenInviteNewAgentUser_GivenEmailLinkIsNull(AgencyUserModel model)
    {
        //arrange
        _sut = CreateSut();
        
        //act
        Func<Task<Result<UserAccount, InviteUserErrorDetails>>> act = async () =>
            await _sut.InviteAgentToOrganisationAsync(model, _externalApplicant, null!, CancellationToken.None);

        //assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await act());
    }

    [Theory, AutoData]
    public async Task ShouldReInvitedAgentUser_GivenValidAlreadyInvitedUserModel(AgencyUserModel model,
        string url, UserAccount userAccount)
    {
        //arrange
        model.AgencyId = _agency.Id;
        model.AgencyName = _agency.OrganisationName!;
        model.AgencyUserRole = AgencyUserRole.AgencyAdministrator;
        model.Email = "valid@email.com";
        userAccount.Agency = _agency;
        userAccount.Status = UserAccountStatus.Invited;
        userAccount.AgencyId = model.AgencyId;
        userAccount.Email = "valid@email.com";
        userAccount.InviteTokenExpiry = _fixedTimeClock.GetCurrentInstant().ToDateTimeUtc().AddHours(1);
        _userAccountRepository.Setup(r => r.GetByEmailAsync(model.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<UserAccount, UserDbErrorReason>(userAccount));
        _sut = CreateSut();

        //act
        var result = await _sut.ReInviteAgentToOrganisationAsync(model, _externalApplicant, url, CancellationToken.None);

        //assert
        Assert.True(result.IsSuccess);
        _userAccountRepository.Verify(r => r.Update( It.Is<UserAccount>(u =>
            u.Email == model.Email
            && u.AgencyId == model.AgencyId
            && u.Status == UserAccountStatus.Invited
            && u.InviteTokenExpiry == UtcNow.AddDays(InviteTokenExpiryDays)
            && !string.IsNullOrEmpty(u.InviteToken.ToString()))),Times.Once());
        _unitOfWOrkMock.Verify(c => c.SaveEntitiesAsync(It.IsAny<CancellationToken>()));
        _mockAuditService.Verify(
            a => a.PublishAuditEventAsync(It.Is<AuditEvent>(e =>
                    e.EventName == AuditEvents.AgencyUserInvitationSent),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
    
    [Theory, AutoData]
    public async Task ShouldResendInvitationEmail_GivenValidAlreadyInvitedUserModel(
        AgencyUserModel model, string url, UserAccount userAccount)
    {
        //arrange
        model.AgencyId = _agency.Id;
        model.AgencyName = _agency.OrganisationName!;
        userAccount.Agency = _agency;
        userAccount.Status = UserAccountStatus.Invited;
        userAccount.AgencyId = model.AgencyId;
        userAccount.Email = model.Email;
        userAccount.InviteTokenExpiry = _fixedTimeClock.GetCurrentInstant().ToDateTimeUtc().AddHours(1);
        _userAccountRepository.Setup(r => r.GetByEmailAsync(model.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<UserAccount, UserDbErrorReason>(userAccount));
        _sut = CreateSut();

        //act
        var result = await _sut.ReInviteAgentToOrganisationAsync(model, _externalApplicant, url, CancellationToken.None);

        //assert
        Assert.True(result.IsSuccess);
        _sendNotifications.Verify(s => s.SendNotificationAsync(
            It.Is<InviteAgentToOrganisationDataModel>(m => m.Name == model.Name
                                                           && m.InviteLink.Contains(url)), 
            NotificationType.InviteAgentUserToOrganisation, 
            It.Is<NotificationRecipient>(r => r.Address == model.Email),
            null, null, _externalApplicant.FullName, CancellationToken.None), Times.Once);
        _mockAuditService.Verify(
            a => a.PublishAuditEventAsync(It.Is<AuditEvent>(e =>
                    e.EventName == AuditEvents.AgencyUserInvitationSent),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory, AutoData]
    public async Task ShouldReturnFailedResult_WhenResendInvitationEmail_GivenNotExistingUserModel(AgencyUserModel model, string url)
    {
        //arrange
        model.AgencyId = _agency.Id;
        model.AgencyName = _agency.OrganisationName!;
        _userAccountRepository.Setup(r => r.GetByEmailAsync(model.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<UserAccount, UserDbErrorReason>(UserDbErrorReason.NotFound));
        _sut = CreateSut();

        //act
        var result = await _sut.ReInviteAgentToOrganisationAsync(model, _externalApplicant, url, CancellationToken.None);

        //assert
        Assert.False(result.IsSuccess);
        _mockAuditService.Verify(
            a => a.PublishAuditEventAsync(It.Is<AuditEvent>(e =>
                    e.EventName == AuditEvents.AgencyUserInvitationFailure),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
    
    [Theory, AutoData]
    public async Task ShouldReturnFailedResult_WhenResendInvitationEmail_AndUserUpdatedFailed(
        AgencyUserModel model, string url, UserAccount userAccount)
    {
        //arrange
        model.AgencyId = _agency.Id;
        model.AgencyName = _agency.OrganisationName!;
        userAccount.Status = UserAccountStatus.Invited;
        userAccount.Agency = _agency;
        userAccount.AgencyId = model.AgencyId;
        userAccount.Email = model.Email;
        userAccount.InviteTokenExpiry = _fixedTimeClock.GetCurrentInstant().ToDateTimeUtc().AddHours(1);
        _userAccountRepository.Setup(r => r.GetByEmailAsync(model.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<UserAccount, UserDbErrorReason>(userAccount));
        _unitOfWOrkMock.Setup(c => c.SaveEntitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => UnitResult.Failure(UserDbErrorReason.General));
        _sut = CreateSut();

        //act
        var result = await _sut.ReInviteAgentToOrganisationAsync(model, _externalApplicant, url, CancellationToken.None);

        //assert
        Assert.False(result.IsSuccess);
        _mockAuditService.Verify(
            a => a.PublishAuditEventAsync(It.Is<AuditEvent>(e =>
                    e.EventName == AuditEvents.AgencyUserInvitationFailure),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
    
    [Theory, AutoData]
    public async Task ShouldVerifyInvitedAgentUserSuccessfully_GivenValidNewUserDetails(AgencyUserModel model, UserAccount userAccount)
    {
        //arrange
        model.AgencyId = _agency.Id;
        model.AgencyName = _agency.OrganisationName!;
        userAccount.AccountType = AccountTypeExternal.Agent;
        userAccount.Status = UserAccountStatus.Invited;
        userAccount.Agency = _agency;
        userAccount.AgencyId = model.AgencyId;
        userAccount.Email = model.Email;
        userAccount.InviteTokenExpiry = _fixedTimeClock.GetCurrentInstant().ToDateTimeUtc().AddHours(1);
        _userAccountRepository.Setup(r => r.GetByEmailAsync(model.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<UserAccount, UserDbErrorReason>(userAccount));
        _sut = CreateSut();
        
        //act
        var result = await _sut.VerifyInvitedUserAccountAsync(model.Email, userAccount.InviteToken.ToString()!);

        //assert
        Assert.True(result.IsSuccess);
        Assert.Equal(userAccount.Email, result.Value.UserEmail);
        Assert.Equal(userAccount.Agency.OrganisationName, result.Value.OrganisationName);
        Assert.Equal(userAccount.InviteToken.ToString(), result.Value.InviteToken);
    }
    
    [Theory, AutoData]
    public async Task ShouldVerifyInvitedAgentUserWithFailure_GivenUserDetailsWithExpiredToken(AgencyUserModel model, UserAccount invitedUser)
    {
        //arrange
        model.AgencyId = _agency.Id;
        model.AgencyName = _agency.OrganisationName!;
        invitedUser.Email = model.Email;
        invitedUser.Status = UserAccountStatus.Invited;
        invitedUser.Agency = _agency;
        invitedUser.AgencyId = model.AgencyId;
        invitedUser.InviteTokenExpiry = _fixedTimeClock.GetCurrentInstant().ToDateTimeUtc().AddHours(-1);
        _userAccountRepository.Setup(r => r.GetByEmailAsync(model.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<UserAccount, UserDbErrorReason>(invitedUser));
        _sut = CreateSut();
        
        //act
        var result = await _sut.VerifyInvitedUserAccountAsync(model.Email, invitedUser.InviteToken.ToString()!);

        //assert
        Assert.False(result.IsSuccess);
    }
    
    [Theory, AutoData]
    public async Task ShouldVerifyInvitedAgentUserWithFailure_GivenUserDetailsWithInvalidToken(AgencyUserModel model, UserAccount invitedUser)
    {
        //arrange
        model.AgencyId = _agency.Id;
        model.AgencyName = _agency.OrganisationName!;
        invitedUser.Email = model.Email;
        invitedUser.Status = UserAccountStatus.Invited;
        invitedUser.Agency = _agency;
        invitedUser.AgencyId = model.AgencyId;
        invitedUser.InviteTokenExpiry = _fixedTimeClock.GetCurrentInstant().ToDateTimeUtc().AddHours(1);
        _userAccountRepository.Setup(r => r.GetByEmailAsync(model.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<UserAccount, UserDbErrorReason>(invitedUser));
        _sut = CreateSut();
         
        //act
        var result = await _sut.VerifyInvitedUserAccountAsync(model.Email, Guid.NewGuid().ToString());

        //assert
        Assert.False(result.IsSuccess);
    }

    [Theory, AutoData]
    public async Task ShouldVerifyInvitedAgentUserWithFailure_GivenNotExistingUserDetails(AgencyUserModel model)
    {
        //arrange
        model.AgencyId = _agency.Id;
        model.AgencyName = _agency.OrganisationName!;
        _userAccountRepository.Setup(r => r.GetByEmailAsync(model.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<UserAccount, UserDbErrorReason>(UserDbErrorReason.NotFound));
        _sut = CreateSut();
        
        //act
        var result = await _sut.VerifyInvitedUserAccountAsync(model.Email, Guid.NewGuid().ToString());

        //assert
        Assert.False(result.IsSuccess);
    }
    
    [Fact]
    public async Task ShouldRetrieveAgencyOrganisation_GivenSystemUserAgencyId()
    {
        //arrange
        _sut = CreateSut();

        //act
        var result = await _sut.RetrieveUserAgencyAsync(_externalApplicant,CancellationToken.None);

        //assert
        Assert.True(result.IsSuccess);
        Assert.Equal(_agency.Id, result.Value.Id);
    }
    
    [Fact]
    public async Task ShouldFailedResult_GivenSystemUserAgentUserIsNotAdmin()
    {
        //arrange
        var agency = _fixture.Build<Agency>()
            .Create();
        _agencyRepository.Setup(r => r.GetAsync(agency.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync( Result.Success<Agency, UserDbErrorReason>(agency));
        
        var user = UserFactory.CreateExternalApplicantIdentityProviderClaimsPrincipal(
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            _fixture.Create<Guid>(),
             agencyId: agency.Id,
            // ReSharper disable once RedundantArgumentDefaultValue
            accountTypeExternal: AccountTypeExternal.Agent);
        var externalApplicant = new ExternalApplicant(user);

        _sut = CreateSut();

        //act
        var result = await _sut.RetrieveUserAgencyAsync(externalApplicant,CancellationToken.None);

        //assert
        Assert.False(result.IsSuccess);
    }
    
    [Fact]
    public async Task ShouldFailedResult_GivenSystemUserIsNotLinkedToAgency()
    {
        //arrange
        var user = UserFactory.CreateExternalApplicantIdentityProviderClaimsPrincipal(
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            _fixture.Create<Guid>(),
            agencyId: null,
            accountTypeExternal: AccountTypeExternal.AgentAdministrator);
        var externalApplicant = new ExternalApplicant(user);

        _sut = CreateSut();

        //act
        var result = await _sut.RetrieveUserAgencyAsync(externalApplicant,CancellationToken.None);

        //assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task ShouldUseContactName_GivenSystemUserAgencyOrganisationDoesNotHaveName()
    {
        //arrange
        var agency = _fixture.Build<Agency>()
            .With(wo => wo.OrganisationName, string.Empty)
            .Create();
        _agencyRepository.Setup(r => r.GetAsync(agency.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<Agency, UserDbErrorReason>(agency));
        
        var user = UserFactory.CreateExternalApplicantIdentityProviderClaimsPrincipal(
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            _fixture.Create<Guid>(),
            agencyId: agency.Id,
            accountTypeExternal: AccountTypeExternal.AgentAdministrator);
        var externalApplicant = new ExternalApplicant(user);

        _sut = CreateSut();

        //act
        var result = await _sut.RetrieveUserAgencyAsync(externalApplicant,CancellationToken.None);

        //assert
        Assert.True(result.IsSuccess);
        Assert.Equal(agency.ContactName, result.Value.Name);
    }
}