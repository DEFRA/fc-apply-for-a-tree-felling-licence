using Forestry.Flo.Services.Applicants.Entities.UserAccount;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using NodaTime;
using NodaTime.Testing;

namespace Forestry.Flo.Services.Applicants.Tests.Services;

public class InvitedUserValidatorTests
{
    private readonly InvitedUserValidator _sut;
    private static readonly DateTime UtcNow = DateTime.UtcNow;
    private readonly IClock _fixedTimeClock;

    public InvitedUserValidatorTests()
    {
        _fixedTimeClock = new FakeClock(Instant.FromDateTimeUtc(UtcNow));
        _sut = new InvitedUserValidator(new NullLogger<InvitedUserValidator>(), _fixedTimeClock);
    }

    [Theory, AutoMoqData]
    public void ShouldValidateInvitedUserSuccessfully_GivenInvitedUser_AndValidToken(UserAccount account)
    {
        //arrange
        account.InviteTokenExpiry = _fixedTimeClock.GetCurrentInstant().ToDateTimeUtc().AddHours(1);
        account.Status = UserAccountStatus.Invited;
        
        //act
        var result = _sut.VerifyInvitedUser(account.InviteToken.ToString()!, account);

        //assert
        result.IsSuccess.Should().BeTrue();
    }
    
    [Theory, AutoMoqData]
    public void ShouldValidateInvitedUserWithError_GivenInvitedUser_AndInvalidToken(UserAccount account, Guid invalidToken)
    {
        //arrange
        account.InviteTokenExpiry = _fixedTimeClock.GetCurrentInstant().ToDateTimeUtc().AddHours(1);
        account.Status = UserAccountStatus.Invited;
        
        //act
        var result = _sut.VerifyInvitedUser(invalidToken.ToString(), account);

        //assert
        result.IsSuccess.Should().BeFalse();
    }
    
    [Theory, AutoMoqData]
    public void ShouldValidateInvitedUserWithError_GivenInvitedUser_AndExpiredToken(UserAccount account)
    {
        //arrange
        account.InviteTokenExpiry = _fixedTimeClock.GetCurrentInstant().ToDateTimeUtc().AddHours(-1);
        account.Status = UserAccountStatus.Invited;
        
        //act
        var result = _sut.VerifyInvitedUser(account.InviteToken.ToString()!, account);

        //assert
        result.IsSuccess.Should().BeFalse();
    }
    
    [Theory, AutoMoqData]
    public void ShouldValidateInvitedUserWithError_GivenInvitedUserAsActive(UserAccount account)
    {
        //arrange
        account.InviteTokenExpiry = _fixedTimeClock.GetCurrentInstant().ToDateTimeUtc().AddHours(-1);
        account.Status = UserAccountStatus.Active;
        
        //act
        var result = _sut.VerifyInvitedUser(account.InviteToken.ToString()!, account);

        //assert
        result.IsSuccess.Should().BeFalse();
    }
}