using Forestry.Flo.Services.Common.Auditing;
using Xunit;

namespace Forestry.Flo.Services.Common.Tests;

public class AuditEventsTests
{
    [Fact]
    public void ShouldReturnPropertyProfileEntityType_GivenCreatePropertyProfileEvent()
    {
        //arrange
        //act
        var result = AuditEvents.GetEventSourceEntityType(AuditEvents.CreatePropertyProfileEvent);

        //assert
        Assert.Equal(SourceEntityType.PropertyProfile, result);
    }
    
    [Fact]
    public void ShouldReturnPropertyProfileEntityType_GivenCreatePropertyProfileFailureEvent()
    {
        //arrange
        //act
        var result = AuditEvents.GetEventSourceEntityType(AuditEvents.CreatePropertyProfileFailureEvent);

        //assert
        Assert.Equal(SourceEntityType.PropertyProfile, result);
    }
    
    [Fact]
    public void ShouldReturnPropertyProfileEntityType_GivenUpdatePropertyProfileEvent()
    {
        //arrange
        //act
        var result = AuditEvents.GetEventSourceEntityType(AuditEvents.UpdatePropertyProfileEvent);

        //assert
        Assert.Equal(SourceEntityType.PropertyProfile, result);
    }
    
    [Fact]
    public void ShouldReturnPropertyProfileEntityType_GivenUpdatePropertyProfileFailureEvent()
    {
        //arrange
        //act
        var result = AuditEvents.GetEventSourceEntityType(AuditEvents.UpdatePropertyProfileFailureEvent);

        //assert
        Assert.Equal(SourceEntityType.PropertyProfile, result);
    }
    
    [Fact]
    public void ShouldReturnCompartmentEntityType_GivenCreateCompartmentEvent()
    {
        //arrange
        //act
        var result = AuditEvents.GetEventSourceEntityType(AuditEvents.CreateCompartmentEvent);

        //assert
        Assert.Equal(SourceEntityType.Compartment, result);
    }
    
    [Fact]
    public void ShouldReturnCompartmentEntityType_GivenCreateCompartmentFailureEvent()
    {
        //arrange
        //act
        var result = AuditEvents.GetEventSourceEntityType(AuditEvents.CreateCompartmentFailureEvent);

        //assert
        Assert.Equal(SourceEntityType.Compartment, result);
    }
    
    [Fact]
    public void ShouldReturnWoodlandOwnerEntityType_GivenUpdateWoodlandOwnerEvent()
    {
        //arrange
        //act
        var result = AuditEvents.GetEventSourceEntityType(AuditEvents.UpdateWoodlandOwnerEvent);

        //assert
        Assert.Equal(SourceEntityType.WoodlandOwner, result);
    }
    
    [Fact]
    public void ShouldReturnWoodlandOwnerEntityType_GivenUpdateWoodlandOwnerFailureEvent()
    {
        //arrange
        //act
        var result = AuditEvents.GetEventSourceEntityType(AuditEvents.UpdateWoodlandOwnerFailureEvent);

        //assert
        Assert.Equal(SourceEntityType.WoodlandOwner, result);
    }
    
    [Fact]
    public void ShouldReturnUserAccountEntityType_GivenRegisterAuditEvent()
    {
        //arrange
        //act
        var result = AuditEvents.GetEventSourceEntityType(AuditEvents.RegisterAuditEvent);

        //assert
        Assert.Equal(SourceEntityType.UserAccount, result);
    }
    
    [Fact]
    public void ShouldReturnUserAccountEntityType_GivenRegisterFailureEvent()
    {
        //arrange
        //act
        var result = AuditEvents.GetEventSourceEntityType(AuditEvents.RegisterFailureEvent);

        //assert
        Assert.Equal(SourceEntityType.UserAccount, result);
    }
    
    [Fact]
    public void ShouldReturnUserAccountEntityType_GivenUpdateAccountEvent()
    {
        //arrange
        //act
        var result = AuditEvents.GetEventSourceEntityType(AuditEvents.UpdateAccountEvent);

        //assert
        Assert.Equal(SourceEntityType.UserAccount, result);
    }
    
    [Fact]
    public void ShouldReturnUserAccountEntityType_GivenUpdateAccountFailureEvent()
    {
        //arrange
        //act
        var result = AuditEvents.GetEventSourceEntityType(AuditEvents.UpdateAccountFailureEvent);

        //assert
        Assert.Equal(SourceEntityType.UserAccount, result);
    }
    
    [Fact]
    public void ShouldReturnUserAccountEntityType_GivenWoodlandOwnerUserInvitationSentEvent()
    {
        //arrange
        //act
        var result = AuditEvents.GetEventSourceEntityType(AuditEvents.WoodlandOwnerUserInvitationSent);

        //assert
        Assert.Equal(SourceEntityType.UserAccount, result);
    }
    
    [Fact]
    public void ShouldReturnUserAccountEntityType_GivenWoodlandOwnerUserInvitationFailureEvent()
    {
        //arrange
        //act
        var result = AuditEvents.GetEventSourceEntityType(AuditEvents.WoodlandOwnerUserInvitationFailure);

        //assert
        Assert.Equal(SourceEntityType.UserAccount, result);
    }
    
    [Fact]
    public void ShouldReturnUserAccountEntityType_GivenAcceptInvitationSuccessEvent()
    {
        //arrange
        //act
        var result = AuditEvents.GetEventSourceEntityType(AuditEvents.AcceptInvitationSuccess);

        //assert
        Assert.Equal(SourceEntityType.UserAccount, result);
    }
    
    [Fact]
    public void ShouldReturnUserAccountEntityType_GivenAcceptInvitationFailureEvent()
    {
        //arrange
        //act
        var result = AuditEvents.GetEventSourceEntityType(AuditEvents.AcceptInvitationFailure);

        //assert
        Assert.Equal(SourceEntityType.UserAccount, result);
    }

    [Theory]
    [InlineData(AuditEvents.PropertyProfileSnapshotCreated)]
    [InlineData(AuditEvents.FellingLicenceApplicationAssignedToAdminHub)]
    [InlineData(AuditEvents.FellingLicenceApplicationFinalActionDateUpdated)]
    [InlineData(AuditEvents.FellingLicenceApplicationSubmissionNotificationSent)]
    [InlineData(AuditEvents.FellingLicenceApplicationSubmissionComplete)]
    [InlineData(AuditEvents.FellingLicenceApplicationSubmissionFailure)]
    public void ShouldReturnFellingLicenceApplicationEntityTypeGivenFLASubmissionEvent(string eventName)
    {
        //arrange
        //act
        var result = AuditEvents.GetEventSourceEntityType(eventName);

        //assert
        Assert.Equal(SourceEntityType.FellingLicenceApplication, result);
    }
}