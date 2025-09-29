using AutoFixture;
using AutoFixture.Xunit2;
using CSharpFunctionalExtensions;
using Forestry.Flo.External.Web.Infrastructure;
using Forestry.Flo.External.Web.Models.UserAccount;
using Forestry.Flo.External.Web.Services;
using Forestry.Flo.Services.Applicants.Entities.UserAccount;
using Forestry.Flo.Services.Applicants.Entities.WoodlandOwner;
using Forestry.Flo.Services.Applicants.Repositories;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.Notifications;
using Forestry.Flo.Services.Notifications.Entities;
using Forestry.Flo.Services.Notifications.Models;
using Forestry.Flo.Services.Notifications.Services;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NodaTime;
using NodaTime.Testing;

namespace Forestry.Flo.External.Web.Tests.Services;

public class InviteWoodlandOwnerToOrganisationUseCaseTests
{
    private const int InviteTokenExpiryDays = 5;
    private InviteWoodlandOwnerToOrganisationUseCase _sut = null!;
    private readonly Fixture _fixture;
    private readonly Mock<IAuditService<OrganisationWoodlandOwnerUserModel>> _mockAuditService;
    private readonly ExternalApplicant _externalApplicant;
    private readonly Mock<ISendNotifications> _sendNotifications;
    private readonly Mock<IOptions<UserInviteOptions>> _userInviteOptions;
    private readonly Mock<IUserAccountRepository> _userAccountRepository;
    private readonly Mock<IWoodlandOwnerRepository> _woodlandOwnerRepository;
    private readonly InvitedUserValidator _invitedUserValidator;
    private static readonly DateTime UtcNow = DateTime.UtcNow;
    private readonly IClock _fixedTimeClock;
    private readonly WoodlandOwner _woodlandOwner;
    private readonly Mock<IUnitOfWork> _unitOfWOrkMock;

    public InviteWoodlandOwnerToOrganisationUseCaseTests()
    {
        _fixture = new Fixture();
        _mockAuditService = new Mock<IAuditService<OrganisationWoodlandOwnerUserModel>>();
        _sendNotifications = new Mock<ISendNotifications>();
        _fixedTimeClock = new FakeClock(Instant.FromDateTimeUtc(UtcNow));
        _userInviteOptions = new Mock<IOptions<UserInviteOptions>>();
        _woodlandOwnerRepository = new Mock<IWoodlandOwnerRepository>();
        _userAccountRepository = new Mock<IUserAccountRepository>();
        _invitedUserValidator = new InvitedUserValidator(new NullLogger<InvitedUserValidator>(), _fixedTimeClock);
        _unitOfWOrkMock = new Mock<IUnitOfWork>();
        _userInviteOptions.Setup(c => c.Value).Returns(new UserInviteOptions { InviteLinkExpiryDays = InviteTokenExpiryDays });
        
        _woodlandOwner = _fixture.Build<WoodlandOwner>()
            .With(wo => wo.IsOrganisation, true)
            .With(wo => wo.Id, Guid.NewGuid)
            .Create();
        
        var user = UserFactory.CreateExternalApplicantIdentityProviderClaimsPrincipal(
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            _fixture.Create<Guid>(),
            _woodlandOwner.Id,
            AccountTypeExternal.WoodlandOwnerAdministrator);
        _externalApplicant = new ExternalApplicant(user);

        _userAccountRepository.SetupGet(r => r.UnitOfWork).Returns(_unitOfWOrkMock.Object);
        _woodlandOwnerRepository.Setup(r => r.GetAsync(_woodlandOwner.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync( Result.Success<WoodlandOwner, UserDbErrorReason>(_woodlandOwner));
    }
    
    private InviteWoodlandOwnerToOrganisationUseCase CreateSut()
    {
        return new InviteWoodlandOwnerToOrganisationUseCase(_mockAuditService.Object,
            _userAccountRepository.Object,
            _woodlandOwnerRepository.Object,
            new NullLogger<InviteWoodlandOwnerToOrganisationUseCase>(),
            _sendNotifications.Object,
            _fixedTimeClock,
            _userInviteOptions.Object,
            new RequestContext("test", new RequestUserModel(_externalApplicant.Principal)),
            _invitedUserValidator);
    }
    
    [Theory]
    [InlineAutoData(WoodlandOwnerUserRole.WoodlandOwnerUser,AccountTypeExternal.WoodlandOwner)]
    [InlineAutoData( WoodlandOwnerUserRole.WoodlandOwnerAdministrator ,AccountTypeExternal.WoodlandOwnerAdministrator)]
    public async Task ShouldRegisterNewInvitedUser_GivenValidNewUserModel(WoodlandOwnerUserRole woodlandOwnerUserRole, AccountTypeExternal accountTypeExternal, OrganisationWoodlandOwnerUserModel model, string url)
    {
        //arrange
        model.WoodlandOwnerId = _woodlandOwner.Id;
        model.WoodlandOwnerName = _woodlandOwner.OrganisationName!;
        model.WoodlandOwnerUserRole = woodlandOwnerUserRole;
        model.Email = "valid@email.com";
        _sut = CreateSut();

        //act
        var result = await _sut.InviteWoodlandOwnerToOrganisationAsync(model, _externalApplicant, url, CancellationToken.None);

        //assert
        Assert.True(result.IsSuccess);
        _userAccountRepository.Verify(r => r.Add( It.Is<UserAccount>(u =>
            u.Email == model.Email
            && u.WoodlandOwnerId == model.WoodlandOwnerId
            && u.Status == UserAccountStatus.Invited
            && u.InviteTokenExpiry == UtcNow.AddDays(InviteTokenExpiryDays)
            && u.AccountType == accountTypeExternal
            && !string.IsNullOrEmpty(u.InviteToken.ToString()))), Times.Once());
        
        _unitOfWOrkMock.Verify(c => c.SaveEntitiesAsync(It.IsAny<CancellationToken>()));
        _mockAuditService.Verify(
            a => a.PublishAuditEventAsync(It.Is<AuditEvent>(e =>
                    e.EventName == AuditEvents.WoodlandOwnerUserInvitationSent),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
    
    [Theory, AutoData]
    public async Task ShouldSendInvitationEmail_GivenValidNewUserModel(OrganisationWoodlandOwnerUserModel model, string url)
    {
        //arrange
        model.WoodlandOwnerId = _woodlandOwner.Id;
        model.WoodlandOwnerName = _woodlandOwner.OrganisationName!;
        _sut = CreateSut();

        //act
        var result = await _sut.InviteWoodlandOwnerToOrganisationAsync(model, _externalApplicant, url, CancellationToken.None);

        //assert
        Assert.True(result.IsSuccess);
        _sendNotifications.Verify(s => s.SendNotificationAsync(
            It.Is<InviteWoodlandOwnerToOrganisationDataModel>(m => m.Name == model.Name
                                                                   && m.InviteLink.Contains(url)), 
            NotificationType.InviteWoodlandOwnerUserToOrganisation, 
            It.Is<NotificationRecipient>(r => r.Address == model.Email),
            null, null, It.IsAny<string?>(), CancellationToken.None), Times.Once);
        _mockAuditService.Verify(
            a => a.PublishAuditEventAsync(It.Is<AuditEvent>(e =>
                    e.EventName == AuditEvents.WoodlandOwnerUserInvitationSent),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
    
    [Theory, AutoData]
    public async Task ShouldReturnFailedResult_WhenNewUserIsInvited_AndEmailSendingFailed(OrganisationWoodlandOwnerUserModel model, string url, string error)
    {
        //arrange
        model.WoodlandOwnerId = _woodlandOwner.Id;
        model.WoodlandOwnerName = _woodlandOwner.OrganisationName!;
        _sendNotifications.Setup(s => s.SendNotificationAsync(It.IsAny<InviteWoodlandOwnerToOrganisationDataModel>(),
            It.IsAny<NotificationType>(), It.IsAny<NotificationRecipient>(), It.IsAny<NotificationRecipient[]?>(),
              It.IsAny<NotificationAttachment[]?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(error));
        _sut = CreateSut();

        //act
        var result = await _sut.InviteWoodlandOwnerToOrganisationAsync(model, _externalApplicant, url, CancellationToken.None);

        //assert
        Assert.False(result.IsSuccess);
        Assert.Equal(error, result.Error.Message);

        _mockAuditService.Verify(
            a => a.PublishAuditEventAsync(It.Is<AuditEvent>(e =>
                    e.EventName == AuditEvents.WoodlandOwnerUserInvitationFailure),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
    
    [Theory, AutoData]
    public async Task ShouldReturnAlreadyExistsError_WhenInvitedUserExistsAndActive(OrganisationWoodlandOwnerUserModel model, string url, UserAccount userAccount)
    {
        //arrange
        model.WoodlandOwnerId = _woodlandOwner.Id;
        model.WoodlandOwnerName = _woodlandOwner.OrganisationName!;
        userAccount.Status = UserAccountStatus.Active;
        _sut = CreateSut();
        _userAccountRepository.Setup(r => r.GetByEmailAsync(model.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<UserAccount, UserDbErrorReason>(userAccount));
        _unitOfWOrkMock.Setup(r => r.SaveEntitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => UnitResult.Failure(UserDbErrorReason.NotUnique));

        //act
        var result = await _sut.InviteWoodlandOwnerToOrganisationAsync(model, _externalApplicant, url, CancellationToken.None);

        //assert
        Assert.False(result.IsSuccess);
        Assert.Equal(InviteUserErrorResult.UserAlreadyExists, result.Error.ErrorResult);
        _mockAuditService.Verify(
            a => a.PublishAuditEventAsync(It.Is<AuditEvent>(e =>
                    e.EventName == AuditEvents.WoodlandOwnerUserInvitationFailure),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
    
    [Theory, AutoData]
    public async Task ShouldReturnAlreadyExistsError_WhenInvitedUserExistsAndInvitedByAnotherWoodlandOwner(OrganisationWoodlandOwnerUserModel model, string url, UserAccount userAccount, Guid? anotherWoodlandOwner)
    {
        //arrange
        model.WoodlandOwnerId = _woodlandOwner.Id;
        model.WoodlandOwnerName = _woodlandOwner.OrganisationName!;
        userAccount.Status = UserAccountStatus.Invited;
        userAccount.WoodlandOwnerId = anotherWoodlandOwner;
        _sut = CreateSut();
        _userAccountRepository.Setup(r => r.GetByEmailAsync(model.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<UserAccount, UserDbErrorReason>(userAccount));
        _unitOfWOrkMock.Setup(r => r.SaveEntitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => UnitResult.Failure(UserDbErrorReason.NotUnique));

        //act
        var result = await _sut.InviteWoodlandOwnerToOrganisationAsync(model, _externalApplicant, url, CancellationToken.None);

        //assert
        Assert.False(result.IsSuccess);
        Assert.Equal(InviteUserErrorResult.UserAlreadyExists, result.Error.ErrorResult);
        _mockAuditService.Verify(
            a => a.PublishAuditEventAsync(It.Is<AuditEvent>(e =>
                    e.EventName == AuditEvents.WoodlandOwnerUserInvitationFailure),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
    
    [Theory, AutoData]
    public async Task ShouldReturnAlreadyInvitedError_WhenInvitedUserExistsAndInvited(OrganisationWoodlandOwnerUserModel model, string url, UserAccount userAccount)
    {
        //arrange
        model.WoodlandOwnerId = _woodlandOwner.Id;
        model.WoodlandOwnerName = _woodlandOwner.OrganisationName!;
        userAccount.Status = UserAccountStatus.Invited;
        userAccount.WoodlandOwnerId = model.WoodlandOwnerId;
        _sut = CreateSut();
        _userAccountRepository.Setup(r => r.GetByEmailAsync(model.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<UserAccount, UserDbErrorReason>(userAccount));
        _unitOfWOrkMock.Setup(r => r.SaveEntitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => UnitResult.Failure(UserDbErrorReason.NotUnique));

        //act
        var result = await _sut.InviteWoodlandOwnerToOrganisationAsync(model, _externalApplicant, url, CancellationToken.None);

        //assert
        Assert.False(result.IsSuccess);
        Assert.Equal(InviteUserErrorResult.UserAlreadyInvited, result.Error.ErrorResult);
        _mockAuditService.Verify(
            a => a.PublishAuditEventAsync(It.Is<AuditEvent>(e =>
                    e.EventName == AuditEvents.WoodlandOwnerUserInvitationFailure),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory, AutoData]
    public async Task ShouldThrowException_WhenInviteNewUser_GivenUserModelIsNull(string url)
    {
        //arrange
        _sut = CreateSut();
        
        //act
        Func<Task<Result<UserAccount, InviteUserErrorDetails>>> act = async () => 
            await _sut.InviteWoodlandOwnerToOrganisationAsync(null!, _externalApplicant, url, CancellationToken.None);

        //assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await act());
    }
    
    [Theory, AutoData]
    public async Task ShouldThrowException_WhenInviteNewUser_GivenSystemUserIsNull(OrganisationWoodlandOwnerUserModel model, string url)
    {
        //arrange
        _sut = CreateSut();
        
        //act
        Func<Task<Result<UserAccount, InviteUserErrorDetails>>> act = async () =>
            await _sut.InviteWoodlandOwnerToOrganisationAsync(model, null!, url, CancellationToken.None);

        //assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await act());
    }
    
    [Theory, AutoData]
    public async Task ShouldThrowException_WhenInviteNewUser_GivenEmailLinkIsNull(OrganisationWoodlandOwnerUserModel model)
    {
        //arrange
        _sut = CreateSut();
        
        //act
        Func<Task<Result<UserAccount, InviteUserErrorDetails>>> act = async () =>
            await _sut.InviteWoodlandOwnerToOrganisationAsync(model, _externalApplicant, null!, CancellationToken.None);

        //assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await act());
    }
    
    [Fact]
    public async Task ShouldRetrieveWoodlandOwnerOrganisation_GivenSystemUserWoodlandOwnerId()
    {
        //arrange
        _sut = CreateSut();

        //act
        var result = await _sut.RetrieveUserWoodlandOwnerOrganisationAsync(_externalApplicant,CancellationToken.None);

        //assert
        Assert.True(result.IsSuccess);
        Assert.Equal(_woodlandOwner.Id, result.Value.Id);
    }
    
    [Fact]
    public async Task ShouldFailedResult_GivenSystemUserWoodlandOwnerUserIsNotAdmin()
    {
        //arrange
        var woodlandOwner = _fixture.Build<WoodlandOwner>()
            .With(wo => wo.IsOrganisation, true)
            .Create();
        _woodlandOwnerRepository.Setup(r => r.GetAsync(woodlandOwner.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync( Result.Success<WoodlandOwner, UserDbErrorReason>(woodlandOwner));
        
        var user = UserFactory.CreateExternalApplicantIdentityProviderClaimsPrincipal(
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            _fixture.Create<Guid>(),
            woodlandOwner.Id,
            // ReSharper disable once RedundantArgumentDefaultValue
            AccountTypeExternal.WoodlandOwner);
        var externalApplicant = new ExternalApplicant(user);

        _sut = CreateSut();

        //act
        var result = await _sut.RetrieveUserWoodlandOwnerOrganisationAsync(externalApplicant,CancellationToken.None);

        //assert
        Assert.False(result.IsSuccess);
    }
    
    [Fact]
    public async Task ShouldFailedResult_GivenSystemUserIsNotLinkedToWoodlandOwner()
    {
        //arrange
        var user = UserFactory.CreateExternalApplicantIdentityProviderClaimsPrincipal(
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            _fixture.Create<Guid>(),
            null,
            AccountTypeExternal.WoodlandOwnerAdministrator);
        var externalApplicant = new ExternalApplicant(user);

        _sut = CreateSut();

        //act
        var result = await _sut.RetrieveUserWoodlandOwnerOrganisationAsync(externalApplicant,CancellationToken.None);

        //assert
        Assert.False(result.IsSuccess);
    }

    [Theory, AutoData]
    public async Task ShouldReInvitedUser_GivenValidAlreadyInvitedUserModel(OrganisationWoodlandOwnerUserModel model,
        string url, UserAccount userAccount)
    {
        //arrange
        model.WoodlandOwnerId = _woodlandOwner.Id;
        model.WoodlandOwnerName = _woodlandOwner.OrganisationName!;
        model.WoodlandOwnerUserRole = WoodlandOwnerUserRole.WoodlandOwnerAdministrator;
        userAccount.WoodlandOwner = _woodlandOwner;
        userAccount.Status = UserAccountStatus.Invited;
        userAccount.WoodlandOwnerId = model.WoodlandOwnerId;
        model.Email = "valid@email.com";
        userAccount.Email = "valid@email.com";
        userAccount.InviteTokenExpiry = _fixedTimeClock.GetCurrentInstant().ToDateTimeUtc().AddHours(1);
        _userAccountRepository.Setup(r => r.GetByEmailAsync(model.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<UserAccount, UserDbErrorReason>(userAccount));
        _sut = CreateSut();

        //act
        var result = await _sut.ReInviteWoodlandOwnerToOrganisationAsync(model, _externalApplicant, url, CancellationToken.None);

        //assert
        Assert.True(result.IsSuccess);
        _userAccountRepository.Verify(r => r.Update( It.Is<UserAccount>(u =>
            u.Email == model.Email
            && u.WoodlandOwnerId == model.WoodlandOwnerId
            && u.Status == UserAccountStatus.Invited
            && u.InviteTokenExpiry == UtcNow.AddDays(InviteTokenExpiryDays)
            && !string.IsNullOrEmpty(u.InviteToken.ToString()))),Times.Once());
        _unitOfWOrkMock.Verify(c => c.SaveEntitiesAsync(It.IsAny<CancellationToken>()));
        _mockAuditService.Verify(
            a => a.PublishAuditEventAsync(It.Is<AuditEvent>(e =>
                    e.EventName == AuditEvents.WoodlandOwnerUserInvitationSent),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
    
    [Theory, AutoData]
    public async Task ShouldResendInvitationEmail_GivenValidAlreadyInvitedUserModel(
        OrganisationWoodlandOwnerUserModel model, string url, UserAccount userAccount)
    {
        //arrange
        model.WoodlandOwnerId = _woodlandOwner.Id;
        model.WoodlandOwnerName = _woodlandOwner.OrganisationName!;
        userAccount.WoodlandOwner = _woodlandOwner;
        userAccount.Status = UserAccountStatus.Invited;
        userAccount.WoodlandOwnerId = model.WoodlandOwnerId;
        userAccount.Email = model.Email;
        userAccount.InviteTokenExpiry = _fixedTimeClock.GetCurrentInstant().ToDateTimeUtc().AddHours(1);
        _userAccountRepository.Setup(r => r.GetByEmailAsync(model.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<UserAccount, UserDbErrorReason>(userAccount));
        _sut = CreateSut();

        //act
        var result = await _sut.ReInviteWoodlandOwnerToOrganisationAsync(model, _externalApplicant, url, CancellationToken.None);

        //assert
        Assert.True(result.IsSuccess);
        _sendNotifications.Verify(s => s.SendNotificationAsync(
            It.Is<InviteWoodlandOwnerToOrganisationDataModel>(m => m.Name == model.Name
                                                                   && m.InviteLink.Contains(url)), 
            NotificationType.InviteWoodlandOwnerUserToOrganisation, 
            It.Is<NotificationRecipient>(r => r.Address == model.Email),
            null, null, It.IsAny<string?>(), CancellationToken.None), Times.Once);
        _mockAuditService.Verify(
            a => a.PublishAuditEventAsync(It.Is<AuditEvent>(e =>
                    e.EventName == AuditEvents.WoodlandOwnerUserInvitationSent),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory, AutoData]
    public async Task ShouldReturnFailedResult_WhenResendInvitationEmail_GivenNotExistingUserModel(OrganisationWoodlandOwnerUserModel model, string url)
    {
        //arrange
        model.WoodlandOwnerId = _woodlandOwner.Id;
        model.WoodlandOwnerName = _woodlandOwner.OrganisationName!;
        _userAccountRepository.Setup(r => r.GetByEmailAsync(model.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<UserAccount, UserDbErrorReason>(UserDbErrorReason.NotFound));
        _sut = CreateSut();

        //act
        var result = await _sut.ReInviteWoodlandOwnerToOrganisationAsync(model, _externalApplicant, url, CancellationToken.None);

        //assert
        Assert.False(result.IsSuccess);
        _mockAuditService.Verify(
            a => a.PublishAuditEventAsync(It.Is<AuditEvent>(e =>
                    e.EventName == AuditEvents.WoodlandOwnerUserInvitationFailure),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
    
    [Theory, AutoData]
    public async Task ShouldReturnFailedResult_WhenResendInvitationEmail_AndUserUpdatedFailed(
        OrganisationWoodlandOwnerUserModel model, string url, UserAccount userAccount)
    {
        //arrange
        model.WoodlandOwnerId = _woodlandOwner.Id;
        model.WoodlandOwnerName = _woodlandOwner.OrganisationName!;
        userAccount.Status = UserAccountStatus.Invited;
        userAccount.WoodlandOwner = _woodlandOwner;
        userAccount.WoodlandOwnerId = model.WoodlandOwnerId;
        userAccount.Email = model.Email;
        userAccount.InviteTokenExpiry = _fixedTimeClock.GetCurrentInstant().ToDateTimeUtc().AddHours(1);
        _userAccountRepository.Setup(r => r.GetByEmailAsync(model.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<UserAccount, UserDbErrorReason>(userAccount));
        _unitOfWOrkMock.Setup(c => c.SaveEntitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => UnitResult.Failure(UserDbErrorReason.General));
        _sut = CreateSut();

        //act
        var result = await _sut.ReInviteWoodlandOwnerToOrganisationAsync(model, _externalApplicant, url, CancellationToken.None);

        //assert
        Assert.False(result.IsSuccess);
        _mockAuditService.Verify(
            a => a.PublishAuditEventAsync(It.Is<AuditEvent>(e =>
                    e.EventName == AuditEvents.WoodlandOwnerUserInvitationFailure),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
    
    [Theory, AutoData]
    public async Task ShouldVerifyInvitedUserSuccessfully_GivenValidNewUserDetails(OrganisationWoodlandOwnerUserModel model, UserAccount userAccount)
    {
        //arrange
        model.WoodlandOwnerId = _woodlandOwner.Id;
        model.WoodlandOwnerName = _woodlandOwner.OrganisationName!;
        userAccount.Status = UserAccountStatus.Invited;
        userAccount.WoodlandOwner = _woodlandOwner;
        userAccount.WoodlandOwnerId = model.WoodlandOwnerId;
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
        Assert.Equal(userAccount.WoodlandOwner.OrganisationName, result.Value.OrganisationName);
        Assert.Equal(userAccount.InviteToken.ToString(), result.Value.InviteToken);
    }
    
    [Theory, AutoData]
    public async Task ShouldVerifyInvitedUserWithFailure_GivenUserDetailsWithExpiredToken(OrganisationWoodlandOwnerUserModel model, UserAccount invitedUser)
    {
        //arrange
        model.WoodlandOwnerId = _woodlandOwner.Id;
        model.WoodlandOwnerName = _woodlandOwner.OrganisationName!;
        invitedUser.Email = model.Email;
        invitedUser.Status = UserAccountStatus.Invited;
        invitedUser.WoodlandOwner = _woodlandOwner;
        invitedUser.WoodlandOwnerId = model.WoodlandOwnerId;
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
    public async Task ShouldVerifyInvitedUserWithFailure_GivenUserDetailsWithInvalidToken(OrganisationWoodlandOwnerUserModel model, UserAccount invitedUser)
    {
        //arrange
        model.WoodlandOwnerId = _woodlandOwner.Id;
        model.WoodlandOwnerName = _woodlandOwner.OrganisationName!;
        invitedUser.Email = model.Email;
        invitedUser.Status = UserAccountStatus.Invited;
        invitedUser.WoodlandOwner = _woodlandOwner;
        invitedUser.WoodlandOwnerId = model.WoodlandOwnerId;
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
    public async Task ShouldVerifyInvitedUserWithFailure_GivenNotExistingUserDetails(OrganisationWoodlandOwnerUserModel model)
    {
        //arrange
        model.WoodlandOwnerId = _woodlandOwner.Id;
        model.WoodlandOwnerName = _woodlandOwner.OrganisationName!;
        _userAccountRepository.Setup(r => r.GetByEmailAsync(model.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<UserAccount, UserDbErrorReason>(UserDbErrorReason.NotFound));
        _sut = CreateSut();
        
        //act
        var result = await _sut.VerifyInvitedUserAccountAsync(model.Email, Guid.NewGuid().ToString());

        //assert
        Assert.False(result.IsSuccess);
    }
}