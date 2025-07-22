using FluentAssertions;
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
        result.Should().Be(SourceEntityType.PropertyProfile);
    }
    
    [Fact]
    public void ShouldReturnPropertyProfileEntityType_GivenCreatePropertyProfileFailureEvent()
    {
        //arrange
        //act
        var result = AuditEvents.GetEventSourceEntityType(AuditEvents.CreatePropertyProfileFailureEvent);

        //assert
        result.Should().Be(SourceEntityType.PropertyProfile);
    }
    
    [Fact]
    public void ShouldReturnPropertyProfileEntityType_GivenUpdatePropertyProfileEvent()
    {
        //arrange
        //act
        var result = AuditEvents.GetEventSourceEntityType(AuditEvents.UpdatePropertyProfileEvent);

        //assert
        result.Should().Be(SourceEntityType.PropertyProfile);
    }
    
    [Fact]
    public void ShouldReturnPropertyProfileEntityType_GivenUpdatePropertyProfileFailureEvent()
    {
        //arrange
        //act
        var result = AuditEvents.GetEventSourceEntityType(AuditEvents.UpdatePropertyProfileFailureEvent);

        //assert
        result.Should().Be(SourceEntityType.PropertyProfile);
    }
    
    [Fact]
    public void ShouldReturnCompartmentEntityType_GivenCreateCompartmentEvent()
    {
        //arrange
        //act
        var result = AuditEvents.GetEventSourceEntityType(AuditEvents.CreateCompartmentEvent);

        //assert
        result.Should().Be(SourceEntityType.Compartment);
    }
    
    [Fact]
    public void ShouldReturnCompartmentEntityType_GivenCreateCompartmentFailureEvent()
    {
        //arrange
        //act
        var result = AuditEvents.GetEventSourceEntityType(AuditEvents.CreateCompartmentFailureEvent);

        //assert
        result.Should().Be(SourceEntityType.Compartment);
    }
    
    [Fact]
    public void ShouldReturnWoodlandOwnerEntityType_GivenUpdateWoodlandOwnerEvent()
    {
        //arrange
        //act
        var result = AuditEvents.GetEventSourceEntityType(AuditEvents.UpdateWoodlandOwnerEvent);

        //assert
        result.Should().Be(SourceEntityType.WoodlandOwner);
    }
    
    [Fact]
    public void ShouldReturnWoodlandOwnerEntityType_GivenUpdateWoodlandOwnerFailureEvent()
    {
        //arrange
        //act
        var result = AuditEvents.GetEventSourceEntityType(AuditEvents.UpdateWoodlandOwnerFailureEvent);

        //assert
        result.Should().Be(SourceEntityType.WoodlandOwner);
    }
    
    [Fact]
    public void ShouldReturnUserAccountEntityType_GivenRegisterAuditEvent()
    {
        //arrange
        //act
        var result = AuditEvents.GetEventSourceEntityType(AuditEvents.RegisterAuditEvent);

        //assert
        result.Should().Be(SourceEntityType.UserAccount);
    }
    
    [Fact]
    public void ShouldReturnUserAccountEntityType_GivenRegisterFailureEvent()
    {
        //arrange
        //act
        var result = AuditEvents.GetEventSourceEntityType(AuditEvents.RegisterFailureEvent);

        //assert
        result.Should().Be(SourceEntityType.UserAccount);
    }
    
    [Fact]
    public void ShouldReturnUserAccountEntityType_GivenUpdateAccountEvent()
    {
        //arrange
        //act
        var result = AuditEvents.GetEventSourceEntityType(AuditEvents.UpdateAccountEvent);

        //assert
        result.Should().Be(SourceEntityType.UserAccount);
    }
    
    [Fact]
    public void ShouldReturnUserAccountEntityType_GivenUpdateAccountFailureEvent()
    {
        //arrange
        //act
        var result = AuditEvents.GetEventSourceEntityType(AuditEvents.UpdateAccountFailureEvent);

        //assert
        result.Should().Be(SourceEntityType.UserAccount);
    }
    
    [Fact]
    public void ShouldReturnUserAccountEntityType_GivenWoodlandOwnerUserInvitationSentEvent()
    {
        //arrange
        //act
        var result = AuditEvents.GetEventSourceEntityType(AuditEvents.WoodlandOwnerUserInvitationSent);

        //assert
        result.Should().Be(SourceEntityType.UserAccount);
    }
    
    [Fact]
    public void ShouldReturnUserAccountEntityType_GivenWoodlandOwnerUserInvitationFailureEvent()
    {
        //arrange
        //act
        var result = AuditEvents.GetEventSourceEntityType(AuditEvents.WoodlandOwnerUserInvitationFailure);

        //assert
        result.Should().Be(SourceEntityType.UserAccount);
    }
    
    [Fact]
    public void ShouldReturnUserAccountEntityType_GivenAcceptInvitationSuccessEvent()
    {
        //arrange
        //act
        var result = AuditEvents.GetEventSourceEntityType(AuditEvents.AcceptInvitationSuccess);

        //assert
        result.Should().Be(SourceEntityType.UserAccount);
    }
    
    [Fact]
    public void ShouldReturnUserAccountEntityType_GivenAcceptInvitationFailureEvent()
    {
        //arrange
        //act
        var result = AuditEvents.GetEventSourceEntityType(AuditEvents.AcceptInvitationFailure);

        //assert
        result.Should().Be(SourceEntityType.UserAccount);
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
        result.Should().Be(SourceEntityType.FellingLicenceApplication);
    }
}