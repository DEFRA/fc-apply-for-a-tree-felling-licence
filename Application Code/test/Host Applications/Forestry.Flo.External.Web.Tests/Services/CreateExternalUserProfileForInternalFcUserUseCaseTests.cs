using System.Security.Claims;
using System.Text.Json;
using AutoFixture;
using CSharpFunctionalExtensions;
using Forestry.Flo.External.Web.Services;
using Forestry.Flo.Services.Applicants;
using Forestry.Flo.Services.Applicants.Configuration;
using Forestry.Flo.Services.Applicants.Entities;
using Forestry.Flo.Services.Applicants.Entities.UserAccount;
using Forestry.Flo.Services.Applicants.Repositories;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.MassTransit.Messages;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NodaTime;
using NodaTime.Testing;
using Agency = Forestry.Flo.Services.Applicants.Entities.Agent.Agency;

namespace Forestry.Flo.External.Web.Tests.Services;

public class CreateExternalUserProfileForInternalFcUserUseCaseTests
{
    private readonly Mock<IAuditService<CreateExternalUserProfileForInternalFcUserUseCase>> _auditService = new();
    private ApplicantsContext? _applicantsContext;
    private readonly Mock<IAgencyRepository>? _agencyRepositoryMock = new();
    private readonly ClaimsPrincipal _claimsPrincipal = new();
    private readonly Fixture _fixture = new();
    private Agency? _fcAgency;
    private FakeClock? _fixedFakeClock;
    private DateTime _now;
    private Guid _approverId;
    private string? _firstName;
    private string? _lastName;
    private string? _identityProviderId;
    private string? _emailAddress;

    private readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
    
    [Fact]
    public async Task WhenValidEventMessage_ThenCreatesNewExternalAccount()
    {
        // arrange
        var messageEvent = CreateEventMessage("test@qxlva.com");
       
        var sut = await CreateSut();

        // act
        var result = await sut.ProcessAsync(messageEvent, CancellationToken.None);

        // assert
        Assert.True(result.IsSuccess);

        var newFcExternalUserAccount = _applicantsContext!.UserAccounts.SingleOrDefault(x => x.IdentityProviderId == _identityProviderId)!;
        Assert.Equal(_emailAddress, newFcExternalUserAccount.Email);
        Assert.Equal(_firstName, newFcExternalUserAccount.FirstName);
        Assert.Equal(_lastName, newFcExternalUserAccount.LastName);
        Assert.Equal("0300 067 4000",newFcExternalUserAccount.ContactTelephone);
        Assert.Null(newFcExternalUserAccount.ContactMobileTelephone);

        Assert.Equal(_fcAgency!.Address!.Line1, newFcExternalUserAccount.ContactAddress!.Line1);
        Assert.Equal(_fcAgency.Address.Line2, newFcExternalUserAccount.ContactAddress.Line2);
        Assert.Equal(_fcAgency.Address.Line3, newFcExternalUserAccount.ContactAddress.Line3);
        Assert.Equal(_fcAgency.Address.Line4, newFcExternalUserAccount.ContactAddress.Line4);
        Assert.Equal(_fcAgency.Address.PostalCode, newFcExternalUserAccount.ContactAddress.PostalCode);
        Assert.Equal(UserAccountStatus.Active, newFcExternalUserAccount.Status);
        Assert.Equal(AccountTypeExternal.FcUser, newFcExternalUserAccount.AccountType);
        Assert.Equal(_fixedFakeClock!.GetCurrentInstant().ToDateTimeUtc(), newFcExternalUserAccount.DateAcceptedPrivacyPolicy);
        Assert.Equal(_fixedFakeClock.GetCurrentInstant().ToDateTimeUtc(), newFcExternalUserAccount.DateAcceptedTermsAndConditions);
        Assert.Equal(_fixedFakeClock.GetCurrentInstant().ToDateTimeUtc(), newFcExternalUserAccount.LastChanged);

        Assert.True(_fcAgency.IsFcAgency);
        Assert.Equal(_fcAgency.Id, newFcExternalUserAccount.AgencyId);
    
        _auditService.Verify(s =>
            s.PublishAuditEventAsync(It.Is<AuditEvent>(
                    e => e.EventName == AuditEvents.RegisterAccountDueToNewInternalFcUserAccountApprovalAuditEvent
                         && e.SourceEntityId == messageEvent.ApprovedByInternalFcUserId
                         && e.UserId == null
                         && JsonSerializer.Serialize(e.AuditData, _options) ==
                         JsonSerializer.Serialize(new
                         { 
                             id = newFcExternalUserAccount.Id.ToString(),
                             accountType = AccountTypeExternal.FcUser,
                             identityProviderId = _identityProviderId,
                             email = messageEvent.EmailAddress,
                             approvedByInternalFcUserId = messageEvent.ApprovedByInternalFcUserId,
                         }, _options)
                         )
                ,
                CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task WhenEmailDomainIsNotPermittedForFcAgency_ThenAccountIsNotCreated()
    {
        // arrange
        var eventMessage = CreateEventMessage("test@some-other-domain.com");

        var sut = await CreateSut();

        // act
        var result = await sut.ProcessAsync(eventMessage, CancellationToken.None);

        // assert
        Assert.True(result.IsFailure);
        AssertUserNotAdded(eventMessage);
        AssertRaisesFailureAudit(eventMessage,
            $"Could not be assigned to FC agency - as invalid email domain {eventMessage.EmailAddress}");
    }
    
    [Fact]
    public async Task WhenEmailAddressAlreadyExists_ThenAccountIsNotCreated()
    {
        // arrange
        var eventMessage = CreateEventMessage("test@qxlva.com");

        var sut = await CreateSut(emailAddressInUse:true);

        // act
        var result = await sut.ProcessAsync(eventMessage, CancellationToken.None);

        // assert
        Assert.True(result.IsFailure);
        AssertUserNotAdded(eventMessage, expectDupeEmail:true);
        AssertRaisesFailureAudit(eventMessage,
            $"User account already exists using the email address provided {eventMessage.EmailAddress}");
    }

    [Fact]
    public async Task WhenFcAgencyIsNotFound_ThenAccountIsNotCreated()
    {
        // arrange
        var eventMessage = CreateEventMessage("test@qxlva.com");

        var sut = await CreateSut(isFcAgencyFound: false);

        // act
        var result = await sut.ProcessAsync(eventMessage, CancellationToken.None);

        // assert
        Assert.True(result.IsFailure);
        AssertUserNotAdded(eventMessage);
        AssertRaisesFailureAudit(eventMessage,
            $"User account for email address {eventMessage.EmailAddress} could not be created as there is no FC agency to assign the user to");
    }

    private async Task<CreateExternalUserProfileForInternalFcUserUseCase> CreateSut(
        bool emailAddressInUse = false,
        bool isFcAgencyFound = true)
    {
        _auditService.Reset();
        _applicantsContext = TestApplicantsDatabaseFactory.CreateDefaultTestContext();

        if (isFcAgencyFound)
        {
            _fcAgency = _fixture.Create<Agency>();
            _fcAgency.IsFcAgency = true;
            _agencyRepositoryMock!.Setup(x => x.FindFcAgency(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Maybe<Agency>.From(_fcAgency));
        }
        else
        {
            _agencyRepositoryMock!.Setup(x => x.FindFcAgency(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Maybe<Agency>.None);
        }

        _now = DateTime.UtcNow;
        _fixedFakeClock = new FakeClock(Instant.FromDateTimeUtc(_now));

        if (emailAddressInUse)
        {
            var userAccount = new UserAccount
            {
                Email = "test@qxlva.com",
                Status = UserAccountStatus.Active,
                FirstName = "test",
                LastName = "test",
                AccountType = AccountTypeExternal.AgentAdministrator,
                ContactAddress = new Address("1","2","3","4","SW1 8QT"),
                LastChanged = _now,
                IdentityProviderId = Guid.NewGuid().ToString(),
                DateAcceptedPrivacyPolicy = _now,
                DateAcceptedTermsAndConditions = _now,
                PreferredContactMethod = PreferredContactMethod.Email,
                ContactTelephone = "0300 067 4000"
            };

            _applicantsContext.Add(userAccount);
            await _applicantsContext.SaveEntitiesAsync(CancellationToken.None);
        }

        var userAccountRepository = new UserAccountRepository(_applicantsContext);

        return new CreateExternalUserProfileForInternalFcUserUseCase(
            new AccountRegistrationService(
                userAccountRepository, 
                new NullLogger<AccountRegistrationService>(), 
                _fixedFakeClock),
            new RetrieveUserAccountsService(
                userAccountRepository, 
                _agencyRepositoryMock.Object, 
                new NullLogger<RetrieveUserAccountsService>()
                ), 
            _agencyRepositoryMock.Object,
            Options.Create(new FcAgencyOptions { PermittedEmailDomainsForFcAgent = new List<string>
            {
                "qxlva.com", 
                "forestrycommission.gov.uk"
            } }),
            new RequestContext("test", new RequestUserModel(_claimsPrincipal)),
            _auditService.Object,
            _fixedFakeClock,
            new NullLogger<CreateExternalUserProfileForInternalFcUserUseCase>()
        );
    }

    private InternalFcUserAccountApprovedEvent CreateEventMessage(string emailAddress = "test.user@qxlva.com")
    {
        _emailAddress = emailAddress;
        _approverId = Guid.NewGuid();
        _firstName = _fixture.Create<string>();
        _lastName = _fixture.Create<string>();
        _identityProviderId = Guid.NewGuid().ToString();
        return new InternalFcUserAccountApprovedEvent(_emailAddress, _firstName, _lastName, _identityProviderId, _approverId);
    }

    private void AssertRaisesFailureAudit(InternalFcUserAccountApprovedEvent messageEvent, string error)
    {
        _auditService.Verify(s =>
            s.PublishAuditEventAsync(It.Is<AuditEvent>(
                    e => e.EventName == AuditEvents.RegisterAccountDueToNewInternalFcUserAccountApprovalAuditEventFailure
                         && e.SourceEntityId == messageEvent.ApprovedByInternalFcUserId
                         && e.UserId == null
                         && JsonSerializer.Serialize(e.AuditData, _options) ==
                         JsonSerializer.Serialize(new
                         {
                             identityProviderId = messageEvent.IdentityProviderId,
                             email = messageEvent.EmailAddress,
                             approvedByInternalFcUserId = messageEvent.ApprovedByInternalFcUserId,
                             error
                         }, _options)
                )
                ,
                CancellationToken.None), Times.Once);
    }

    private void AssertUserNotAdded(InternalFcUserAccountApprovedEvent messageEvent, bool expectDupeEmail = false)
    {
        if (!expectDupeEmail)
        {
            Assert.Empty(_applicantsContext!.UserAccounts.Where(x => x.Email == messageEvent.EmailAddress));
        }
        Assert.Empty(_applicantsContext!.UserAccounts.Where(x => x.IdentityProviderId == _identityProviderId));
    }
}