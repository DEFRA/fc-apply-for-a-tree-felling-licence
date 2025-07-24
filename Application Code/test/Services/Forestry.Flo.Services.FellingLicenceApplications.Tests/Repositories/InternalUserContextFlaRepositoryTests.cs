using FluentAssertions;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Tests.Common;
using LinqKit;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Forestry.Flo.Services.FellingLicenceApplications.Tests.Repositories;

public class InternalUserContextFlaRepositoryTests
{
    private readonly InternalUserContextFlaRepository _sut;
    private readonly FellingLicenceApplicationsContext _fellingLicenceApplicationsContext;

    public InternalUserContextFlaRepositoryTests()
    {
        _fellingLicenceApplicationsContext = TestFellingLicenceApplicationsDatabaseFactory.CreateDefaultTestContext();
        _sut = new InternalUserContextFlaRepository(_fellingLicenceApplicationsContext);
    }

    [Theory, AutoMoqData]
    public async Task ShouldIncludeDependentObjectsInResults_WhenGetApplication_GivenApplicationId(
        FellingLicenceApplication licenceApplication)
    {
        //arrange
        _fellingLicenceApplicationsContext.Add(licenceApplication);
        await _fellingLicenceApplicationsContext.SaveChangesAsync();

        //act
        var result = await _sut.GetAsync(licenceApplication.Id, CancellationToken.None);

        //assert
        result.HasValue.Should().BeTrue();
        result.Value.StatusHistories.Should().NotBeEmpty();
        result.Value.Documents.Should().NotBeEmpty();
        result.Value.CaseNotes.Should().NotBeEmpty();
        result.Value.AssigneeHistories.Should().NotBeEmpty();
        result.Value.LinkedPropertyProfile.Should().NotBeNull();
        var proposedRestockingDetails = result.Value.LinkedPropertyProfile!.ProposedFellingDetails!.SelectMany(p => p.ProposedRestockingDetails!).ToList();
        proposedRestockingDetails.Should().NotBeEmpty();
        proposedRestockingDetails!.First().RestockingSpecies.Should().NotBeEmpty();
        result.Value.LinkedPropertyProfile!.ProposedFellingDetails.Should().NotBeEmpty();
        result.Value.LinkedPropertyProfile!.ProposedFellingDetails!.First().FellingSpecies.Should().NotBeEmpty();
        result.Value.SubmittedFlaPropertyDetail.Should().NotBeNull();
    }

    [Theory, AutoMoqData]
    public async Task ShouldAddAssigneeHistoryWhenNoneExist(
        FellingLicenceApplication fellingLicenceApplication,
        AssignedUserRole role,
        Guid assignedUserId,
        DateTime timeStamp)
    {
        //arrange
        fellingLicenceApplication.AssigneeHistories = new List<AssigneeHistory>(0);
        await _fellingLicenceApplicationsContext.FellingLicenceApplications.AddAsync(fellingLicenceApplication,
            CancellationToken.None);
        await _fellingLicenceApplicationsContext.SaveEntitiesAsync(CancellationToken.None);

        //act
        var result = await _sut.AssignFellingLicenceApplicationToStaffMemberAsync(fellingLicenceApplication.Id,
            assignedUserId, role, timeStamp, CancellationToken.None);

        //assert
        Assert.True(result.UserUnassigned.HasNoValue);
        Assert.False(result.UserAlreadyAssigned);

        var storedFla =
            await _fellingLicenceApplicationsContext.FellingLicenceApplications.FindAsync(fellingLicenceApplication.Id);
        Assert.Equal(1, storedFla.AssigneeHistories.Count);
        Assert.Contains(storedFla.AssigneeHistories, x =>
            x.AssignedUserId == assignedUserId
            && x.Role == role
            && x.TimestampAssigned == timeStamp
            && x.TimestampUnassigned == null);
    }

    [Theory, AutoMoqData]
    public async Task ShouldUpdateApproverId(
        FellingLicenceApplication fellingLicenceApplication,
        Guid approverId)
    {
        //arrange
        fellingLicenceApplication.AssigneeHistories = new List<AssigneeHistory>(0);
        await _fellingLicenceApplicationsContext.FellingLicenceApplications.AddAsync(fellingLicenceApplication,
            CancellationToken.None);
        await _fellingLicenceApplicationsContext.SaveEntitiesAsync(CancellationToken.None);

        //act
        var result = await _sut.SetApplicationApproverAsync(fellingLicenceApplication.Id, approverId, CancellationToken.None);

        //assert

        var storedFla =
            await _fellingLicenceApplicationsContext.FellingLicenceApplications.FindAsync(fellingLicenceApplication.Id);

        Assert.Equal(approverId, storedFla.ApproverId);
    }

    [Theory, AutoMoqData]
    public async Task ShouldRevertApproverIdToNull(FellingLicenceApplication fellingLicenceApplication)
    {
        //arrange
        fellingLicenceApplication.AssigneeHistories = new List<AssigneeHistory>(0);
        await _fellingLicenceApplicationsContext.FellingLicenceApplications.AddAsync(fellingLicenceApplication,
            CancellationToken.None);
        await _fellingLicenceApplicationsContext.SaveEntitiesAsync(CancellationToken.None);

        //act
        var result = await _sut.SetApplicationApproverAsync(fellingLicenceApplication.Id, null, CancellationToken.None);

        //assert

        var storedFla =
            await _fellingLicenceApplicationsContext.FellingLicenceApplications.FindAsync(fellingLicenceApplication.Id);

        Assert.Null(storedFla.ApproverId);
    }

    [Theory, AutoMoqData]
    public async Task ShouldNotAddAssigneeHistoryWhenUserIsAlreadyInRole(
        FellingLicenceApplication fellingLicenceApplication,
        AssignedUserRole role,
        Guid assignedUserId,
        DateTime timeStamp)
    {

        var originalTimestamp = DateTime.Today.ToUniversalTime();

        //arrange
        fellingLicenceApplication.AssigneeHistories = new List<AssigneeHistory>
        {
            new AssigneeHistory
            {
                AssignedUserId = assignedUserId,
                Role = role,
                FellingLicenceApplication = fellingLicenceApplication,
                TimestampAssigned = originalTimestamp
            }
        };
        await _fellingLicenceApplicationsContext.FellingLicenceApplications.AddAsync(fellingLicenceApplication,
            CancellationToken.None);
        await _fellingLicenceApplicationsContext.SaveEntitiesAsync(CancellationToken.None);

        //act
        var result = await _sut.AssignFellingLicenceApplicationToStaffMemberAsync(fellingLicenceApplication.Id,
            assignedUserId, role, timeStamp, CancellationToken.None);

        //assert
        Assert.True(result.UserUnassigned.HasNoValue);
        Assert.True(result.UserAlreadyAssigned);

        var storedFla =
            await _fellingLicenceApplicationsContext.FellingLicenceApplications.FindAsync(fellingLicenceApplication.Id);
        Assert.Equal(1, storedFla.AssigneeHistories.Count);
        Assert.Contains(storedFla.AssigneeHistories, x =>
            x.AssignedUserId == assignedUserId
            && x.Role == role
            && x.TimestampAssigned == originalTimestamp
            && x.TimestampUnassigned == null);
    }

    [Theory, AutoMoqData]
    public async Task ShouldAddAssigneeHistoryWhenNoneExistWithSameRole(
        FellingLicenceApplication fellingLicenceApplication,
        Guid assignedUserId,
        Guid existingUserId,
        DateTime existingTimestamp,
        DateTime newTimeStamp)
    {
        //arrange
        var assignedRole = AssignedUserRole.WoodlandOfficer;
        var existingRole = AssignedUserRole.AdminOfficer;
        fellingLicenceApplication.AssigneeHistories = new List<AssigneeHistory>
        {
            new()
            {
                Role = existingRole,
                AssignedUserId = existingUserId,
                TimestampAssigned = existingTimestamp
            }
        };
        await _fellingLicenceApplicationsContext.FellingLicenceApplications.AddAsync(fellingLicenceApplication,
            CancellationToken.None);
        await _fellingLicenceApplicationsContext.SaveEntitiesAsync(CancellationToken.None);

        //act
        var result = await _sut.AssignFellingLicenceApplicationToStaffMemberAsync(fellingLicenceApplication.Id,
            assignedUserId, assignedRole, newTimeStamp, CancellationToken.None);

        //assert
        Assert.True(result.UserUnassigned.HasNoValue);
        Assert.False(result.UserAlreadyAssigned);

        var storedFla =
            await _fellingLicenceApplicationsContext.FellingLicenceApplications.FindAsync(fellingLicenceApplication.Id);
        Assert.Equal(2, storedFla.AssigneeHistories.Count);
        Assert.Contains(storedFla.AssigneeHistories, x =>
            x.AssignedUserId == existingUserId
            && x.Role == existingRole
            && x.TimestampAssigned == existingTimestamp
            && x.TimestampUnassigned == null);
        Assert.Contains(storedFla.AssigneeHistories, x =>
            x.AssignedUserId == assignedUserId
            && x.Role == assignedRole
            && x.TimestampAssigned == newTimeStamp
            && x.TimestampUnassigned == null);
    }

    [Theory, AutoMoqData]
    public async Task ShouldAddAssigneeHistoryWhenOneExistsWithSameRole(
        FellingLicenceApplication fellingLicenceApplication,
        Guid assignedUserId,
        Guid existingUserId,
        AssignedUserRole assignedRole,
        DateTime existingTimestamp,
        DateTime newTimeStamp)
    {
        //arrange
        fellingLicenceApplication.AssigneeHistories = new List<AssigneeHistory>
        {
            new()
            {
                Role = assignedRole,
                AssignedUserId = existingUserId,
                TimestampAssigned = existingTimestamp
            }
        };
        await _fellingLicenceApplicationsContext.FellingLicenceApplications.AddAsync(fellingLicenceApplication,
            CancellationToken.None);
        await _fellingLicenceApplicationsContext.SaveEntitiesAsync(CancellationToken.None);

        //act
        var result = await _sut.AssignFellingLicenceApplicationToStaffMemberAsync(fellingLicenceApplication.Id,
            assignedUserId, assignedRole, newTimeStamp, CancellationToken.None);

        //assert
        Assert.True(result.UserUnassigned.HasValue);
        Assert.Equal(existingUserId, result.UserUnassigned.Value);
        Assert.False(result.UserAlreadyAssigned);

        var storedFla =
            await _fellingLicenceApplicationsContext.FellingLicenceApplications.FindAsync(fellingLicenceApplication.Id);
        Assert.Equal(2, storedFla.AssigneeHistories.Count);
        Assert.Contains(storedFla.AssigneeHistories, x =>
            x.AssignedUserId == existingUserId
            && x.Role == assignedRole
            && x.TimestampAssigned == existingTimestamp
            && x.TimestampUnassigned == newTimeStamp);
        Assert.Contains(storedFla.AssigneeHistories, x =>
            x.AssignedUserId == assignedUserId
            && x.Role == assignedRole
            && x.TimestampAssigned == newTimeStamp
            && x.TimestampUnassigned == null);
    }

    [Theory, AutoMoqData]
    public async Task CanRemoveAssigneeHistoryEntriesByRole(
        FellingLicenceApplication fellingLicenceApplication,
        DateTime timeStamp)
    {
        //arrange
        foreach (var assigneeHistory in fellingLicenceApplication.AssigneeHistories)
        {
            assigneeHistory.TimestampUnassigned = null;
        }

        await _fellingLicenceApplicationsContext.FellingLicenceApplications.AddAsync(fellingLicenceApplication, CancellationToken.None);
        await _fellingLicenceApplicationsContext.SaveEntitiesAsync(CancellationToken.None);

        var assignedRoles = fellingLicenceApplication.AssigneeHistories.Select(x => x.Role).Distinct().ToArray();

        //act
        var result = await _sut.RemoveAssignedRolesFromApplicationAsync(fellingLicenceApplication.Id, assignedRoles,
            timeStamp, CancellationToken.None);

        //assert
        Assert.True(result.IsSuccess);

        var storedFla =
            await _fellingLicenceApplicationsContext.FellingLicenceApplications.FindAsync(fellingLicenceApplication.Id);

        Assert.All(storedFla.AssigneeHistories, x => Assert.Equal(timeStamp, x.TimestampUnassigned));
    }

    [Theory, AutoMoqData]
    public async Task ShouldAdd_ExternalAccessLink_WithSuccess(ExternalAccessLink accessLink)
    {
        //arrange
        //act
        var result = await _sut.AddExternalAccessLinkAsync(accessLink, CancellationToken.None);

        //assert
        var link = _fellingLicenceApplicationsContext.ExternalAccessLinks
            .FirstOrDefault(l => l.FellingLicenceApplicationId == accessLink.FellingLicenceApplicationId
                                 && l.ContactEmail == accessLink.ContactEmail
                                 && l.Purpose == accessLink.Purpose);
        result.IsSuccess.Should().BeTrue();
        link.Should().NotBeNull();
        link!.AccessCode.Should().Be(accessLink.AccessCode);
    }

    [Theory, AutoMoqData]
    public async Task ShouldUpdate_ExternalAccessLink_WithSuccess(
        ExternalAccessLink accessLink,
        Guid newAccessCode)
    {
        //arrange
        _fellingLicenceApplicationsContext.ExternalAccessLinks.Add(accessLink);
        await _fellingLicenceApplicationsContext.SaveChangesAsync();
        accessLink.AccessCode = newAccessCode;

        //act
        var result = await _sut.UpdateExternalAccessLinkAsync(accessLink, CancellationToken.None);

        //assert
        var link = _fellingLicenceApplicationsContext.ExternalAccessLinks
            .FirstOrDefault(l => l.FellingLicenceApplicationId == accessLink.FellingLicenceApplicationId
                                 && l.ContactEmail == accessLink.ContactEmail
                                 && l.Purpose == accessLink.Purpose);
        result.IsSuccess.Should().BeTrue();
        link.Should().NotBeNull();
        link!.AccessCode.Should().Be(accessLink.AccessCode);
    }

    [Theory, AutoMoqData]
    public async Task ShouldReturnNotFound_WhenUpdateExternalNotExistingAccessLink(ExternalAccessLink accessLink,
        string newAccessCode)
    {
        //arrange
        //act
        var result = await _sut.UpdateExternalAccessLinkAsync(accessLink, CancellationToken.None);

        //assert
        var link = _fellingLicenceApplicationsContext.ExternalAccessLinks
            .FirstOrDefault(l => l.FellingLicenceApplicationId == accessLink.FellingLicenceApplicationId
                                 && l.ContactEmail == accessLink.ContactEmail
                                 && l.Purpose == accessLink.Purpose);
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(UserDbErrorReason.NotFound);
    }

    [Theory, AutoMoqData]
    public async Task ShouldGetExternalAccessLinks_GivenLinkDetailsToFilterBy(ExternalAccessLink accessLink)
    {
        //arrange
        _fellingLicenceApplicationsContext.ExternalAccessLinks.Add(accessLink);
        await _fellingLicenceApplicationsContext.SaveChangesAsync();

        //act
        var link = (await _sut.GetUserExternalAccessLinksByApplicationIdAndPurposeAsync(
                accessLink.FellingLicenceApplicationId, accessLink.Name, accessLink.ContactEmail, accessLink.Purpose,
                CancellationToken.None))
            .FirstOrDefault();

        //assert
        link.Should().NotBeNull();
        link!.AccessCode.Should().Be(accessLink.AccessCode);
    }

    [Theory, AutoMoqData]
    public async Task ShouldRetrieveProposedFellingAndRestockingDetails(
        FellingLicenceApplication fellingLicenceApplicationToFind,
        FellingLicenceApplication anotherFellingLicenceApplication)
    {
        //arrange
        _fellingLicenceApplicationsContext.FellingLicenceApplications.Add(fellingLicenceApplicationToFind);
        _fellingLicenceApplicationsContext.FellingLicenceApplications.Add(anotherFellingLicenceApplication);
        await _fellingLicenceApplicationsContext.SaveChangesAsync(CancellationToken.None);

        var result = await _sut.GetProposedFellingAndRestockingDetailsForApplicationAsync(
            fellingLicenceApplicationToFind.Id,
            CancellationToken.None);

        //assert
        Assert.True(result.IsSuccess);

        var felling = fellingLicenceApplicationToFind!
            .LinkedPropertyProfile!
            .ProposedFellingDetails;

        var restocking = fellingLicenceApplicationToFind
            .LinkedPropertyProfile!
            .ProposedFellingDetails!
            .SelectMany(x => x.ProposedRestockingDetails);

        Assert.Equal(felling.Count(), result.Value.Item1.Count);
        Assert.Equal(restocking.Count(), result.Value.Item2.Count);

        felling.ForEach(expected =>
        {
            Assert.Contains(result.Value.Item1, x =>
                x.Id == expected.Id
                && x.OperationType == expected.OperationType
                && x.AreaToBeFelled == expected.AreaToBeFelled
                && x.NumberOfTrees == expected.NumberOfTrees
                && x.TreeMarking == expected.TreeMarking
                && x.IsPartOfTreePreservationOrder == expected.IsPartOfTreePreservationOrder
                && x.IsWithinConservationArea == expected.IsWithinConservationArea
                && x.FellingSpecies.All(s => expected.FellingSpecies.Any(y => y.Id == s.Id)));
        });

        restocking.ForEach(expected =>
        {
            Assert.Contains(result.Value.Item2, x =>
                x.Id == expected.Id
                && x.RestockingProposal == expected.RestockingProposal
                && x.Area == expected.Area
                && x.PercentageOfRestockArea == expected.PercentageOfRestockArea
                && x.RestockingDensity == expected.RestockingDensity
                && x.RestockingSpecies.All(s => expected.RestockingSpecies.Any(y => y.Id == s.Id)));
        });
    }

    [Theory, AutoMoqData]
    public async Task ShouldRetrieveConfirmedFellingAndRestockingDetails(
        FellingLicenceApplication fellingLicenceApplicationToFind,
        FellingLicenceApplication anotherFellingLicenceApplication)
    {
        //arrange
        _fellingLicenceApplicationsContext.FellingLicenceApplications.Add(fellingLicenceApplicationToFind);
        _fellingLicenceApplicationsContext.FellingLicenceApplications.Add(anotherFellingLicenceApplication);
        await _fellingLicenceApplicationsContext.SaveChangesAsync(CancellationToken.None);

        var result =
            await _sut.GetConfirmedFellingAndRestockingDetailsForApplicationAsync(fellingLicenceApplicationToFind.Id,
                CancellationToken.None);

        //assert
        Assert.True(result.IsSuccess);

        var felling = fellingLicenceApplicationToFind.SubmittedFlaPropertyDetail
            .SubmittedFlaPropertyCompartments.SelectMany(x => x.ConfirmedFellingDetails);
        var restocking = fellingLicenceApplicationToFind.SubmittedFlaPropertyDetail
            .SubmittedFlaPropertyCompartments
            .SelectMany(x => x.ConfirmedFellingDetails)
            .SelectMany(x => x.ConfirmedRestockingDetails);

        Assert.Equal(felling.Count(), result.Value.Item1.Count);
        Assert.Equal(restocking.Count(), result.Value.Item2.Count);

        felling.ForEach(expected =>
        {
            Assert.Contains(result.Value.Item1, x =>
                x.Id == expected.Id
                && x.OperationType == expected.OperationType
                && x.AreaToBeFelled == expected.AreaToBeFelled
                && x.NumberOfTrees == expected.NumberOfTrees
                && x.TreeMarking == expected.TreeMarking
                && x.IsPartOfTreePreservationOrder == expected.IsPartOfTreePreservationOrder
                && x.IsWithinConservationArea == expected.IsWithinConservationArea
                && x.ConfirmedFellingSpecies.All(s => expected.ConfirmedFellingSpecies.Any(y => y.Id == s.Id)));
        });

        restocking.ForEach(expected =>
        {
            Assert.Contains(result.Value.Item2, x =>
                x.Id == expected.Id
                && x.RestockingProposal == expected.RestockingProposal
                && x.Area == expected.Area
                && x.PercentageOfRestockArea == expected.PercentageOfRestockArea
                && x.RestockingDensity == expected.RestockingDensity
                && x.ConfirmedRestockingSpecies.All(s => expected.ConfirmedRestockingSpecies.Any(y => y.Id == s.Id)));
        });
    }

    [Theory, AutoMoqData]
    public async Task ShouldReturnEmptyListsWhenFlaHasNoConfirmedFellingAndRestockingDetails(
        FellingLicenceApplication fellingLicenceApplication)
    {
        //arrange
        foreach (var submittedFlaPropertyCompartment in fellingLicenceApplication.SubmittedFlaPropertyDetail
                     .SubmittedFlaPropertyCompartments)
        {
            submittedFlaPropertyCompartment.ConfirmedFellingDetails.Clear();
        }

        _fellingLicenceApplicationsContext.FellingLicenceApplications.Add(fellingLicenceApplication);
        await _fellingLicenceApplicationsContext.SaveChangesAsync(CancellationToken.None);

        var result =
            await _sut.GetConfirmedFellingAndRestockingDetailsForApplicationAsync(fellingLicenceApplication.Id,
                CancellationToken.None);

        //assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Item1);
        Assert.Empty(result.Value.Item2);
    }

    [Theory, AutoMoqData]
    public async Task ShouldReturnFailureWhenFlaNotFound(
        FellingLicenceApplication fellingLicenceApplication)
    {
        //arrange
        _fellingLicenceApplicationsContext.FellingLicenceApplications.Add(fellingLicenceApplication);
        await _fellingLicenceApplicationsContext.SaveChangesAsync(CancellationToken.None);

        var result =
            await _sut.GetConfirmedFellingAndRestockingDetailsForApplicationAsync(Guid.NewGuid(),
                CancellationToken.None);

        //assert
        Assert.True(result.IsFailure);
    }

    [Theory, AutoMoqData]
    public async Task ShouldBeAbleToLocateExistingExternalAccessLink(
        FellingLicenceApplication application,
        ExternalAccessLink accessLink)
    {
        application.ExternalAccessLinks = new List<ExternalAccessLink> { accessLink };
        _fellingLicenceApplicationsContext.FellingLicenceApplications.Add(application);
        await _fellingLicenceApplicationsContext.SaveChangesAsync(CancellationToken.None);

        var result = await _sut.GetValidExternalAccessLinkAsync(
            application.Id,
            accessLink.AccessCode,
            accessLink.ContactEmail,
            accessLink.ExpiresTimeStamp.AddDays(-1),
            CancellationToken.None);

        Assert.True(result.HasValue);
        Assert.Equal(accessLink, result.Value);
    }

    [Theory, AutoMoqData]
    public async Task CannotLocateExternalAccessLinkWithWrongApplicationId(
        FellingLicenceApplication application,
        ExternalAccessLink accessLink)
    {
        application.ExternalAccessLinks = new List<ExternalAccessLink> { accessLink };
        _fellingLicenceApplicationsContext.FellingLicenceApplications.Add(application);
        await _fellingLicenceApplicationsContext.SaveChangesAsync(CancellationToken.None);

        var result = await _sut.GetValidExternalAccessLinkAsync(
            Guid.NewGuid(),
            accessLink.AccessCode,
            accessLink.ContactEmail,
            accessLink.ExpiresTimeStamp.AddDays(-1),
            CancellationToken.None);

        Assert.True(result.HasNoValue);
    }

    [Theory, AutoMoqData]
    public async Task CannotLocateExternalAccessLinkWithWrongAccessCode(
        FellingLicenceApplication application,
        ExternalAccessLink accessLink)
    {
        application.ExternalAccessLinks = new List<ExternalAccessLink> { accessLink };
        _fellingLicenceApplicationsContext.FellingLicenceApplications.Add(application);
        await _fellingLicenceApplicationsContext.SaveChangesAsync(CancellationToken.None);

        var result = await _sut.GetValidExternalAccessLinkAsync(
            application.Id,
            Guid.NewGuid(),
            accessLink.ContactEmail,
            accessLink.ExpiresTimeStamp.AddDays(-1),
            CancellationToken.None);

        Assert.True(result.HasNoValue);
    }

    [Theory, AutoMoqData]
    public async Task CannotLocateExternalAccessLinkWithWrongEmailAddress(
        FellingLicenceApplication application,
        ExternalAccessLink accessLink,
        string wrongEmailAddress)
    {
        application.ExternalAccessLinks = new List<ExternalAccessLink> { accessLink };
        _fellingLicenceApplicationsContext.FellingLicenceApplications.Add(application);
        await _fellingLicenceApplicationsContext.SaveChangesAsync(CancellationToken.None);

        var result = await _sut.GetValidExternalAccessLinkAsync(
            application.Id,
            accessLink.AccessCode,
            wrongEmailAddress,
            accessLink.ExpiresTimeStamp.AddDays(-1),
            CancellationToken.None);

        Assert.True(result.HasNoValue);
    }

    [Theory, AutoMoqData]
    public async Task CannotRetrieveExpiredExternalAccessLink(
        FellingLicenceApplication application,
        ExternalAccessLink accessLink)
    {
        application.ExternalAccessLinks = new List<ExternalAccessLink> { accessLink };
        _fellingLicenceApplicationsContext.FellingLicenceApplications.Add(application);
        await _fellingLicenceApplicationsContext.SaveChangesAsync(CancellationToken.None);

        var result = await _sut.GetValidExternalAccessLinkAsync(
            application.Id,
            accessLink.AccessCode,
            accessLink.ContactEmail,
            accessLink.ExpiresTimeStamp.AddDays(1),
            CancellationToken.None);

        Assert.True(result.HasNoValue);
    }

    [Theory, AutoMoqData]
    public async Task CanAddConsulteeCommentToApplication(
        FellingLicenceApplication application,
        string authorName,
        string contactEmail,
        ApplicationSection section,
        string comment,
        DateTime timeStamp)
    {
        application.ConsulteeComments = new List<ConsulteeComment>(0);
        _fellingLicenceApplicationsContext.FellingLicenceApplications.Add(application);
        await _fellingLicenceApplicationsContext.SaveEntitiesAsync(CancellationToken.None);

        var newComment = new ConsulteeComment
        {
            ApplicableToSection = section,
            AuthorContactEmail = contactEmail,
            AuthorName = authorName,
            CreatedTimestamp = timeStamp,
            Comment = comment,
            FellingLicenceApplicationId = application.Id
        };

        var result = await _sut.AddConsulteeCommentAsync(newComment, CancellationToken.None);

        Assert.True(result.IsSuccess);

        var storedComment =
            await _fellingLicenceApplicationsContext.ConsulteeComments.SingleAsync(CancellationToken.None);

        Assert.Equal(application.Id, storedComment.FellingLicenceApplicationId);
        Assert.Equal(authorName, storedComment.AuthorName);
        Assert.Equal(contactEmail, storedComment.AuthorContactEmail);
        Assert.Equal(section, storedComment.ApplicableToSection);
        Assert.Equal(comment, storedComment.Comment);
        Assert.Equal(timeStamp, storedComment.CreatedTimestamp);
    }

    [Theory, AutoMoqData]
    public async Task CanGetAllConsulteeCommentsForApplication(
        FellingLicenceApplication application,
        List<ConsulteeComment> comments)
    {
        comments.ForEach(x => x.FellingLicenceApplicationId = application.Id);
        application.ConsulteeComments = comments;

        _fellingLicenceApplicationsContext.FellingLicenceApplications.Add(application);
        await _fellingLicenceApplicationsContext.SaveEntitiesAsync(CancellationToken.None);

        var result = await _sut.GetConsulteeCommentsAsync(application.Id, null, CancellationToken.None);

        Assert.Equal(comments.Count, result.Count);

        comments.ForEach(x => Assert.Contains(x, result));
    }

    [Theory, AutoMoqData]
    public async Task CanGetConsulteeCommentsForApplicationByAuthorEmail(
        FellingLicenceApplication application,
        List<ConsulteeComment> comments)
    {
        comments.ForEach(x => x.FellingLicenceApplicationId = application.Id);
        application.ConsulteeComments = comments;

        _fellingLicenceApplicationsContext.FellingLicenceApplications.Add(application);
        await _fellingLicenceApplicationsContext.SaveEntitiesAsync(CancellationToken.None);

        var result = await _sut.GetConsulteeCommentsAsync(application.Id, comments.First().AuthorContactEmail,
            CancellationToken.None);

        Assert.Equal(1, result.Count);

        Assert.Contains(comments.First(), result);
    }

    [Theory, AutoMoqData]
    public async Task RetrievesAllApplicationsThatHaveSurpassedFinalActionDate_WhenNoneExtended(
        List<FellingLicenceApplication> applications)
    {
        foreach (var application in applications)
        {
            application.FinalActionDate = DateTime.UtcNow.AddDays(-1);
            application.StatusHistories = new List<StatusHistory>()
            {
                new()
                {
                    Created = DateTime.Now,
                    FellingLicenceApplication = application,
                    Status = FellingLicenceStatus.Submitted
                }
            };
            _fellingLicenceApplicationsContext.FellingLicenceApplications.Add(application);
        }

        await _fellingLicenceApplicationsContext.SaveEntitiesAsync(CancellationToken.None);

        var result = await _sut.GetApplicationsThatAreWithinThresholdOfFinalActionDateAsync(
            DateTime.UtcNow,
            new TimeSpan(0, 0, 0, 0),
            CancellationToken.None);

        Assert.Equal(applications.Count, result.Count);
    }

    [Theory, AutoMoqData]
    public async Task RetrievesAllApplicationsWithinThresholdOfFinalActionDate_WhenNoneExtended(
        List<FellingLicenceApplication> applications)
    {
        var threshold = new TimeSpan(10, 0, 0, 0);

        foreach (var application in applications)
        {
            application.FinalActionDate = DateTime.UtcNow.AddDays(9);
            application.StatusHistories = new List<StatusHistory>()
            {
                new()
                {
                    Created = DateTime.Now,
                    FellingLicenceApplication = application,
                    Status = FellingLicenceStatus.Submitted
                }
            };
            _fellingLicenceApplicationsContext.FellingLicenceApplications.Add(application);
        }

        await _fellingLicenceApplicationsContext.SaveEntitiesAsync(CancellationToken.None);

        var result = await _sut.GetApplicationsThatAreWithinThresholdOfFinalActionDateAsync(
            DateTime.UtcNow,
            threshold,
            CancellationToken.None);

        Assert.Equal(applications.Count, result.Count);
    }

    [Theory, AutoMoqData]
    public async Task RetrievesNoApplicationsThatHaveSurpassedFinalActionDate_WhenAlreadyProcessed(
        List<FellingLicenceApplication> applications)
    {
        foreach (var application in applications)
        {
            application.FinalActionDate = DateTime.UtcNow.AddDays(-1);
            application.StatusHistories = new List<StatusHistory>()
            {
                new()
                {
                    Created = DateTime.Now.AddDays(-5),
                    FellingLicenceApplication = application,
                    Status = FellingLicenceStatus.Submitted
                },
                new()
                {
                    Created = DateTime.Now,
                    FellingLicenceApplication = application,
                    Status = FellingLicenceStatus.Approved
                }
            };
            _fellingLicenceApplicationsContext.FellingLicenceApplications.Add(application);
        }

        await _fellingLicenceApplicationsContext.SaveEntitiesAsync(CancellationToken.None);

        var result = await _sut.GetApplicationsThatAreWithinThresholdOfFinalActionDateAsync(
            DateTime.UtcNow,
            new TimeSpan(0, 0, 0, 0),
            CancellationToken.None);

        Assert.Empty(result);
    }

    [Theory, AutoMoqData]
    public async Task RetrievesAllApplicationsPriorVoluntaryWithdrawalNotificationThreshold_WhenNonePreviouslyNotified(
        List<FellingLicenceApplication> applications)
    {
        var status = FellingLicenceStatus.WithApplicant;
        foreach (var application in applications)
        {
            application.VoluntaryWithdrawalNotificationTimeStamp = null;
            application.StatusHistories = new List<StatusHistory>
            {
                new()
                {
                    Created = DateTime.UtcNow.AddDays(-15),
                    FellingLicenceApplication = application,
                    Status = status
                }
            };
            _fellingLicenceApplicationsContext.FellingLicenceApplications.Add(application);
            
            status = status == FellingLicenceStatus.WithApplicant
                ? FellingLicenceStatus.ReturnedToApplicant
                : FellingLicenceStatus.WithApplicant;
        }

        await _fellingLicenceApplicationsContext.SaveEntitiesAsync(CancellationToken.None);

        var result = await _sut.GetApplicationsThatAreWithinThresholdOfWithdrawalNotificationDateAsync(
            DateTime.UtcNow,
            new TimeSpan(14, 0, 0, 0),
            CancellationToken.None);

        Assert.Equal(applications.Count, result.Count);
    }

    [Theory, AutoMoqData]
    public async Task
        RetrieveAllApplicationsSubsequentVoluntaryWithdrawalNotificationThreshold_WhenAllPreviouslyNotifiedOnDifferentStatus(
            List<FellingLicenceApplication> applications)
    {
        var status = FellingLicenceStatus.WithApplicant;
        foreach (var application in applications)
        {
            application.VoluntaryWithdrawalNotificationTimeStamp = DateTime.UtcNow.AddDays(-16);
            application.StatusHistories = new List<StatusHistory>
            {
                new()
                {
                    Created = DateTime.UtcNow.AddDays(-15),
                    FellingLicenceApplication = application,
                    Status = status
                }
            };
            _fellingLicenceApplicationsContext.FellingLicenceApplications.Add(application);

            status = status == FellingLicenceStatus.WithApplicant
                ? FellingLicenceStatus.ReturnedToApplicant
                : FellingLicenceStatus.WithApplicant;
        }

        await _fellingLicenceApplicationsContext.SaveEntitiesAsync(CancellationToken.None);

        var result = await _sut.GetApplicationsThatAreWithinThresholdOfWithdrawalNotificationDateAsync(
            DateTime.UtcNow,
            new TimeSpan(14, 0, 0, 0),
            CancellationToken.None);

        Assert.Equal(applications.Count, result.Count);
    }

    [Theory, AutoMoqData]
    public async Task
        RetrievesSomeApplicationsSubsequentVoluntaryWithdrawalNotificationThreshold_WhenSomePreviouslyNotified(
            List<FellingLicenceApplication> applications)
    {
        var status = FellingLicenceStatus.WithApplicant;
        foreach (var application in applications)
        {
            application.VoluntaryWithdrawalNotificationTimeStamp = null;
            application.StatusHistories = new List<StatusHistory>
            {
                new()
                {
                    Created = DateTime.UtcNow.AddDays(-15),
                    FellingLicenceApplication = application,
                    Status = status
                }
            };
            _fellingLicenceApplicationsContext.FellingLicenceApplications.Add(application);
            
            status = status == FellingLicenceStatus.WithApplicant
                ? FellingLicenceStatus.ReturnedToApplicant
                : FellingLicenceStatus.WithApplicant;
        }

        applications[0].VoluntaryWithdrawalNotificationTimeStamp = DateTime.UtcNow.AddDays(-5);
        applications[1].VoluntaryWithdrawalNotificationTimeStamp = DateTime.UtcNow.AddDays(-2);

        await _fellingLicenceApplicationsContext.SaveEntitiesAsync(CancellationToken.None);

        var result = await _sut.GetApplicationsThatAreWithinThresholdOfWithdrawalNotificationDateAsync(
            DateTime.UtcNow,
            new TimeSpan(14, 0, 0, 0),
            CancellationToken.None);

        Assert.Equal(applications.Count(x => x.VoluntaryWithdrawalNotificationTimeStamp == null), result.Count);
    }

    [Theory, AutoMoqData]
    public async Task
        RetrievesSomeApplicationsSubsequentVoluntaryWithdrawalNotificationThreshold_WhenRestAreNotWithApplicant(
            List<FellingLicenceApplication> applications)
    {
        var status = FellingLicenceStatus.WithApplicant;
        foreach (var application in applications)
        {
            application.VoluntaryWithdrawalNotificationTimeStamp = null;
            application.StatusHistories = new List<StatusHistory>
            {
                new()
                {
                    Created = DateTime.UtcNow.AddDays(-15),
                    FellingLicenceApplication = application,
                    Status = status
                }
            };
            _fellingLicenceApplicationsContext.FellingLicenceApplications.Add(application);

            status = status == FellingLicenceStatus.WithApplicant
                ? FellingLicenceStatus.ReturnedToApplicant
                : FellingLicenceStatus.WithApplicant;
        }

        applications[1].StatusHistories.Add(new StatusHistory()
        {
            Created = DateTime.UtcNow.AddDays(-3),
            FellingLicenceApplication = applications[0],
            Status = FellingLicenceStatus.Withdrawn
        });
        applications[2].StatusHistories.Add(new StatusHistory()
        {
            Created = DateTime.UtcNow.AddDays(-1),
            FellingLicenceApplication = applications[0],
            Status = FellingLicenceStatus.Submitted
        });

        await _fellingLicenceApplicationsContext.SaveEntitiesAsync(CancellationToken.None);

        var result = await _sut.GetApplicationsThatAreWithinThresholdOfWithdrawalNotificationDateAsync(
            DateTime.UtcNow,
            new TimeSpan(14, 0, 0, 0),
            CancellationToken.None);

        Assert.NotEqual(applications.Count, result.Count);
        Assert.NotEqual(0, result.Count);
    }

    [Theory, AutoMoqData]
    public async Task RetrieveNoApplicationsSubsequentVoluntaryWithdrawalNotificationThreshold_WhenNonePriorThreshold(
        List<FellingLicenceApplication> applications)
    {
        var status = FellingLicenceStatus.WithApplicant;
        foreach (var application in applications)
        {
            application.VoluntaryWithdrawalNotificationTimeStamp = null;
            application.StatusHistories = new List<StatusHistory>
            {
                new()
                {
                    Created = DateTime.UtcNow.AddDays(-1),
                    FellingLicenceApplication = application,
                    Status = status
                }
            };
            _fellingLicenceApplicationsContext.FellingLicenceApplications.Add(application);

            status = status == FellingLicenceStatus.WithApplicant
                ? FellingLicenceStatus.ReturnedToApplicant
                : FellingLicenceStatus.WithApplicant;
        }

        await _fellingLicenceApplicationsContext.SaveEntitiesAsync(CancellationToken.None);

        var result = await _sut.GetApplicationsThatAreWithinThresholdOfWithdrawalNotificationDateAsync(
            DateTime.UtcNow,
            new TimeSpan(14, 0, 0, 0),
            CancellationToken.None);

        Assert.Equal(0, result.Count);
    }

    [Theory, AutoMoqData]
    public async Task
        RetrieveNoApplicationsSubsequentVoluntaryWithdrawalNotificationThreshold_WhenAllPreviouslyNotified(
            List<FellingLicenceApplication> applications)
    {
        var status = FellingLicenceStatus.WithApplicant;
        foreach (var application in applications)
        {
            application.VoluntaryWithdrawalNotificationTimeStamp = DateTime.UtcNow.AddDays(-1);
            application.StatusHistories = new List<StatusHistory>
            {
                new()
                {
                    Created = DateTime.UtcNow.AddDays(-15),
                    FellingLicenceApplication = application,
                    Status = status
                }
            };
            _fellingLicenceApplicationsContext.FellingLicenceApplications.Add(application);

            status = status == FellingLicenceStatus.WithApplicant
                ? FellingLicenceStatus.ReturnedToApplicant
                : FellingLicenceStatus.WithApplicant;
        }

        await _fellingLicenceApplicationsContext.SaveEntitiesAsync(CancellationToken.None);

        var result = await _sut.GetApplicationsThatAreWithinThresholdOfWithdrawalNotificationDateAsync(
            DateTime.UtcNow,
            new TimeSpan(14, 0, 0, 0),
            CancellationToken.None);

        Assert.Equal(0, result.Count);
    }

    [Theory, AutoMoqData]
    public async Task When_UpdateAreaCode_ShouldUpdateFlaAreaCodeAndApplicationReferenceWhenFound(FellingLicenceApplication application,
        string expectedAreaCode,
        string expectedAdminRegionName)
    {
        // Arrange
        var fixAreaCode = Guid.NewGuid().ToString();
        application.ApplicationReference = fixAreaCode + "/001/2023";
        var expectedApplicationReference = application.ApplicationReference.Replace(fixAreaCode, expectedAreaCode);
        _fellingLicenceApplicationsContext.FellingLicenceApplications.Add(application);

        await _fellingLicenceApplicationsContext.SaveEntitiesAsync(default);
        var result = await _sut.UpdateAreaCodeAsync(application.Id, expectedAreaCode, expectedAdminRegionName, default);

        // Act
        var actual = _fellingLicenceApplicationsContext.FellingLicenceApplications.Single(x => x.Id == application.Id);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(expectedAreaCode == actual.AreaCode);
        Assert.True(expectedAdminRegionName == actual.AdministrativeRegion);
        Assert.True(expectedApplicationReference == actual.ApplicationReference);
    }

    [Fact]
    public async Task When_UpdateAreaCode_AssertThrowsWhenCannotFindApplicationById()
    {
        // Arrange

        // Act
        var result = await _sut.UpdateAreaCodeAsync(Guid.NewGuid(), "something", "something-else", default);

        // Assert
        Assert.True(result.IsFailure);
        Assert.True(result.Error == UserDbErrorReason.NotFound);
    }

    [Theory, AutoMoqData]
    public async Task When_UpdateAreaCode_AssertThrowsIfNewAreaCodeIsNullOrEmpty(FellingLicenceApplication application)
    {
        // Arrange
        application.ApplicationReference = "wrong-format-somehow";
        _fellingLicenceApplicationsContext.FellingLicenceApplications.Add(application);

        await _fellingLicenceApplicationsContext.SaveEntitiesAsync(default);

        // Act
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _sut.UpdateAreaCodeAsync(application.Id, string.Empty, "adminHub", default));

        // Act
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _sut.UpdateAreaCodeAsync(application.Id, null, "adminHub", default));
    }

    [Theory, AutoMoqData]
    public async Task When_UpdateAreaCode_AssertThrowsIfReferenceFormatIsWrong(
        FellingLicenceApplication application,
        string expectedAreaCode,
        string expectedAdminHub)
    {
        // Arrange

        application.ApplicationReference = "wrong-format-somehow";
        _fellingLicenceApplicationsContext.FellingLicenceApplications.Add(application);

        await _fellingLicenceApplicationsContext.SaveEntitiesAsync(default);

        // Act
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _sut.UpdateAreaCodeAsync(application.Id, expectedAreaCode, expectedAdminHub, default));
    }

    [Theory, AutoMoqData]
    public async Task ExpireDecisionPublicRegisterEntry_SetsExpirationValueAsync(FellingLicenceApplication application)
    {
        //Arrange
        application.PublicRegister = new PublicRegister {
            DecisionPublicRegisterRemovedTimestamp = null,
            DecisionPublicRegisterExpiryTimestamp = null
        };

        var now = DateTime.UtcNow;
        _fellingLicenceApplicationsContext.FellingLicenceApplications.Add(application);
        await _fellingLicenceApplicationsContext.SaveEntitiesAsync();

        // Act
        var result = await _sut.ExpireDecisionPublicRegisterEntryAsync(application.Id, now , default);
        
        //Assert
        Assert.True(result.IsSuccess);
        Assert.True(application.PublicRegister!.DecisionPublicRegisterRemovedTimestamp == now);
    }

    [Theory, AutoMoqData]
    public async Task ExpireDecisionPublicRegisterEntry_ReturnsFailureWhenNoPublicRegisterEntity(FellingLicenceApplication application)
    {
        //Arrange
        application.PublicRegister = null;

        var now = DateTime.UtcNow;
        _fellingLicenceApplicationsContext.FellingLicenceApplications.Add(application);
        await _fellingLicenceApplicationsContext.SaveEntitiesAsync();

        // Act
        var result = await _sut.ExpireDecisionPublicRegisterEntryAsync(application.Id, now, default);

        //Assert
        Assert.True(result.IsFailure);
        Assert.True(result.Error == UserDbErrorReason.NotFound);
    }

    [Theory, AutoMoqData]
    public async Task AddDecisionPublicRegisterDetails_Success(FellingLicenceApplication application)
    {
        //Arrange
        var publishedDate = DateTime.UtcNow;
        var expiryDate = publishedDate.AddDays(27);
        _fellingLicenceApplicationsContext.FellingLicenceApplications.Add(application);
        await _fellingLicenceApplicationsContext.SaveEntitiesAsync();

        // Act
        var result = await _sut.AddDecisionPublicRegisterDetailsAsync(application.Id, publishedDate, expiryDate, default);

        //Assert
        Assert.True(result.IsSuccess);
        Assert.True(application.PublicRegister!.DecisionPublicRegisterPublicationTimestamp == publishedDate);
        Assert.True(application.PublicRegister!.DecisionPublicRegisterExpiryTimestamp == expiryDate);
    }

    [Theory, AutoMoqData]
    public async Task AddDecisionPublicRegisterDetails_ReturnsFailureWhenNoPublicRegisterEntity(FellingLicenceApplication application)
    {
        //Arrange
        application.PublicRegister = null;
        _fellingLicenceApplicationsContext.FellingLicenceApplications.Add(application);
        await _fellingLicenceApplicationsContext.SaveEntitiesAsync();

        // Act
        var result = await _sut.AddDecisionPublicRegisterDetailsAsync(application.Id, DateTime.UtcNow, DateTime.UtcNow, default);

        //Assert
        Assert.True(result.IsFailure);
        Assert.True(result.Error == UserDbErrorReason.NotFound);
    }

    [Theory, AutoMoqData]
    public async Task GetFinalisedApplicationsForPublicRegisterExpiry_FindsExpectedSet(
        FellingLicenceApplication applicationFound,
        FellingLicenceApplication applicationFound2,
        FellingLicenceApplication applicationNotToBeFoundAsRemovedAlready,
        FellingLicenceApplication applicationNotToBeFoundAsExpiryIsInFuture)
    {
        //Arrange
        var now = DateTime.UtcNow.AddDays(-1);

        applicationFound.PublicRegister = new PublicRegister
        {
            DecisionPublicRegisterExpiryTimestamp = now,
            DecisionPublicRegisterRemovedTimestamp  = null
        };
        applicationFound2.PublicRegister = new PublicRegister
        {
            DecisionPublicRegisterExpiryTimestamp = now,
            DecisionPublicRegisterRemovedTimestamp = null
        };

        applicationNotToBeFoundAsRemovedAlready.PublicRegister = new PublicRegister
        {
            DecisionPublicRegisterExpiryTimestamp = now,
            DecisionPublicRegisterRemovedTimestamp = now
        };

        applicationNotToBeFoundAsExpiryIsInFuture.PublicRegister = new PublicRegister
        {
            DecisionPublicRegisterExpiryTimestamp = now.AddDays(2),
            DecisionPublicRegisterRemovedTimestamp = null
        };

        _fellingLicenceApplicationsContext.FellingLicenceApplications.Add(applicationFound);
        _fellingLicenceApplicationsContext.FellingLicenceApplications.Add(applicationFound2);
        _fellingLicenceApplicationsContext.FellingLicenceApplications.Add(applicationNotToBeFoundAsRemovedAlready);
        _fellingLicenceApplicationsContext.FellingLicenceApplications.Add(applicationNotToBeFoundAsExpiryIsInFuture);

        await _fellingLicenceApplicationsContext.SaveEntitiesAsync();

        // Act
        var result = await _sut.GetFinalisedApplicationsWithExpiredDecisionPublicRegisterPeriodsAsync(
            now, default);

        //Assert
        result.Count.Should().Be(2);
        applicationFound.Id.Should().Be(result.First().Id);
        applicationFound2.Id.Should().Be(result.Last().Id);
    }

    [Theory, AutoMoqData] public async Task GetFinalisedApplicationsForPublicRegisterExpiry_AllHistoric_AllPreviouslyRemovedSet_FindsNone(
        FellingLicenceApplication application)
    {
        //Arrange
        var now = DateTime.UtcNow.AddDays(-1);

        application.PublicRegister = new PublicRegister
        {
            DecisionPublicRegisterExpiryTimestamp = now,
            DecisionPublicRegisterRemovedTimestamp = now
        };

        _fellingLicenceApplicationsContext.FellingLicenceApplications.Add(application);
        await _fellingLicenceApplicationsContext.SaveEntitiesAsync();

        // Act
        var result = await _sut.
            GetFinalisedApplicationsWithExpiredDecisionPublicRegisterPeriodsAsync(now, default);

        //Assert
        result.Count.Should().Be(0);
    }

    [Theory, AutoMoqData]
    public async Task GetFinalisedApplicationsForPublicRegisterExpiry_AllFutureExpiryDate_FindsNone(
        FellingLicenceApplication application)
    {
        //Arrange
        var now = DateTime.UtcNow.AddDays(-1);

        application.PublicRegister = new PublicRegister
        {
            DecisionPublicRegisterExpiryTimestamp = now.AddDays(2),
        };

        _fellingLicenceApplicationsContext.FellingLicenceApplications.Add(application);
        await _fellingLicenceApplicationsContext.SaveEntitiesAsync();

        // Act
        var result = await _sut.
            GetFinalisedApplicationsWithExpiredDecisionPublicRegisterPeriodsAsync(now, default);

        //Assert
        result.Count.Should().Be(0);
    }

    [Theory, AutoMoqData]
    public async Task GetFinalisedApplicationsForPublicRegisterExpiry_HasNoPublicRegisterEntity(
        FellingLicenceApplication application)
    {
        //Arrange
        application.PublicRegister = null;
        var now = DateTime.UtcNow.AddDays(-1);


        _fellingLicenceApplicationsContext.FellingLicenceApplications.Add(application);
        await _fellingLicenceApplicationsContext.SaveEntitiesAsync();

        // Act
        var result = await _sut.
            GetFinalisedApplicationsWithExpiredDecisionPublicRegisterPeriodsAsync(now, default);

        //Assert
        result.Count.Should().Be(0);
    }

    [Theory, AutoMoqData]
    public async Task RemoveAssignedFellingLicenceApplicationStaffMemberAsync_UnassignsAllActiveAssigneeHistoriesForUser(
        FellingLicenceApplication application,
        Guid assignedUserId,
        DateTime timestamp)
    {
        // Arrange
        // Add multiple active assignee histories for the same user
        const AssignedUserRole role1 = AssignedUserRole.AdminOfficer;
        const AssignedUserRole role2 = AssignedUserRole.WoodlandOfficer;
        const AssignedUserRole role3 = AssignedUserRole.FieldManager;

        var assigneeHistories = new List<AssigneeHistory>
        {
            new()
            {
                FellingLicenceApplication = application,
                FellingLicenceApplicationId = application.Id,
                AssignedUserId = assignedUserId,
                Role = role1,
                TimestampAssigned = timestamp.AddDays(-2),
                TimestampUnassigned = null
            },
            new()
            {
                FellingLicenceApplication = application,
                FellingLicenceApplicationId = application.Id,
                AssignedUserId = assignedUserId,
                Role = role2,
                TimestampAssigned = timestamp.AddDays(-1),
                TimestampUnassigned = null
            },
            // Already unassigned, should not be updated
            new()
            {
                FellingLicenceApplication = application,
                FellingLicenceApplicationId = application.Id,
                AssignedUserId = assignedUserId,
                Role = role3,
                TimestampAssigned = timestamp.AddDays(-3),
                TimestampUnassigned = timestamp.AddDays(-1)
            }
        };

        application.AssigneeHistories = assigneeHistories;
        _fellingLicenceApplicationsContext.FellingLicenceApplications.Add(application);
        await _fellingLicenceApplicationsContext.SaveEntitiesAsync(CancellationToken.None);

        // Act
        var result = await _sut.RemoveAssignedFellingLicenceApplicationStaffMemberAsync(
            application.Id, assignedUserId, timestamp, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);

        var storedFla = await _fellingLicenceApplicationsContext.FellingLicenceApplications.Include(x => x.AssigneeHistories).FirstAsync(x => x.Id == application.Id);
        var updatedHistories = storedFla.AssigneeHistories.Where(x => x.AssignedUserId == assignedUserId).ToList();

        // All active (not previously unassigned) histories should now be unassigned with the given timestamp
        Assert.All(updatedHistories.Where(x => x.TimestampUnassigned != timestamp.AddDays(-1)),
            x => Assert.Equal(timestamp, x.TimestampUnassigned));

        // The already unassigned entry should remain unchanged
        var alreadyUnassigned = updatedHistories.First(x => x.Role == role3);
        Assert.Equal(timestamp.AddDays(-1), alreadyUnassigned.TimestampUnassigned);
    }

    [Theory, AutoMoqData]
    public async Task RemoveAssignedFellingLicenceApplicationStaffMemberAsync_UnassignsSingleActiveAssigneeHistoryForUser(
        FellingLicenceApplication application,
        Guid assignedUserId,
        DateTime timestamp)
    {
        var assigneeHistories = new List<AssigneeHistory>
        {
            new()
            {
                FellingLicenceApplication = application,
                FellingLicenceApplicationId = application.Id,
                AssignedUserId = assignedUserId,
                Role = AssignedUserRole.AdminOfficer,
                TimestampAssigned = timestamp.AddDays(-2),
            }
        };

        application.AssigneeHistories = assigneeHistories;
        _fellingLicenceApplicationsContext.FellingLicenceApplications.Add(application);
        await _fellingLicenceApplicationsContext.SaveEntitiesAsync(CancellationToken.None);

        // Act
        var result = await _sut.RemoveAssignedFellingLicenceApplicationStaffMemberAsync(
            application.Id, assignedUserId, timestamp, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);

        var storedFla = await _fellingLicenceApplicationsContext.FellingLicenceApplications.Include(x => x.AssigneeHistories).FirstAsync(x => x.Id == application.Id);
        var updatedHistories = storedFla.AssigneeHistories.Where(x => x.AssignedUserId == assignedUserId).ToList();

        // All active (not previously unassigned) histories should now be unassigned with the given timestamp
        Assert.All(updatedHistories.Where(x => x.TimestampUnassigned != timestamp.AddDays(-1)),
            x => Assert.Equal(timestamp, x.TimestampUnassigned));

        Assert.Equal(timestamp, assigneeHistories.First().TimestampUnassigned);
    }

    [Theory, AutoMoqData]
    public async Task RemoveAssignedFellingLicenceApplicationStaffMemberAsync_ReturnsNotFound_WhenNoActiveAssigneeHistories(
        FellingLicenceApplication application,
        Guid assignedUserId,
        DateTime timestamp)
    {
        // Arrange
        application.AssigneeHistories = new List<AssigneeHistory>
        {
            new()
            {
                FellingLicenceApplication = application,
                FellingLicenceApplicationId = application.Id,
                AssignedUserId = assignedUserId,
                Role = AssignedUserRole.AdminOfficer,
                TimestampAssigned = timestamp.AddDays(-2),
                TimestampUnassigned = timestamp.AddDays(-1)
            }
        };
        _fellingLicenceApplicationsContext.FellingLicenceApplications.Add(application);
        await _fellingLicenceApplicationsContext.SaveEntitiesAsync(CancellationToken.None);

        // Act
        var result = await _sut.RemoveAssignedFellingLicenceApplicationStaffMemberAsync(
            application.Id, assignedUserId, timestamp, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(UserDbErrorReason.NotFound, result.Error);
    }

    [Theory, AutoMoqData]
    public async Task RemoveAssignedFellingLicenceApplicationStaffMemberAsync_ReturnsNotFound_WhenNoAssigneeHistoriesForUser(
        FellingLicenceApplication application,
        Guid assignedUserId,
        DateTime timestamp)
    {
        // Arrange
        application.AssigneeHistories = new List<AssigneeHistory>
        {
            new()
            {
                FellingLicenceApplication = application,
                FellingLicenceApplicationId = application.Id,
                AssignedUserId = Guid.NewGuid(),
                Role = AssignedUserRole.AdminOfficer,
                TimestampAssigned = timestamp.AddDays(-2),
            }
        };
        _fellingLicenceApplicationsContext.FellingLicenceApplications.Add(application);
        await _fellingLicenceApplicationsContext.SaveEntitiesAsync(CancellationToken.None);

        // Act
        var result = await _sut.RemoveAssignedFellingLicenceApplicationStaffMemberAsync(
            application.Id, assignedUserId, timestamp, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(UserDbErrorReason.NotFound, result.Error);
    }
    [Theory, AutoMoqData]
    public async Task GetSubmittedFlaPropertyCompartmentsByApplicationIdAsync_ReturnsCompartments_WhenDetailExists(
        FellingLicenceApplication application,
        SubmittedFlaPropertyDetail propertyDetail,
        List<SubmittedFlaPropertyCompartment> compartments)
    {
        // Arrange
        propertyDetail.FellingLicenceApplicationId = application.Id;
        propertyDetail.SubmittedFlaPropertyCompartments = compartments;
        application.SubmittedFlaPropertyDetail = propertyDetail;
        foreach (var compartment in compartments)
        {
            compartment.SubmittedFlaPropertyDetailId = propertyDetail.FellingLicenceApplicationId;
            compartment.SubmittedFlaPropertyDetail = propertyDetail;
        }
        _fellingLicenceApplicationsContext.FellingLicenceApplications.Add(application);
        _fellingLicenceApplicationsContext.SubmittedFlaPropertyDetails.Add(propertyDetail);
        _fellingLicenceApplicationsContext.SubmittedFlaPropertyCompartments.AddRange(compartments);
        await _fellingLicenceApplicationsContext.SaveChangesAsync();

        // Act
        var result = await _sut.GetSubmittedFlaPropertyCompartmentsByApplicationIdAsync(application.Id, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(compartments.Count, result.Value.Count);
        foreach (var compartment in compartments)
        {
            Assert.Contains(result.Value, c => c == compartment);
        }
    }

    [Theory, AutoMoqData]
    public async Task GetSubmittedFlaPropertyCompartmentsByApplicationIdAsync_ReturnsEmptyList_WhenNoCompartments(
        FellingLicenceApplication application,
        SubmittedFlaPropertyDetail propertyDetail)
    {
        // Arrange
        propertyDetail.FellingLicenceApplicationId = application.Id;
        propertyDetail.SubmittedFlaPropertyCompartments = new List<SubmittedFlaPropertyCompartment>();
        application.SubmittedFlaPropertyDetail = propertyDetail;
        _fellingLicenceApplicationsContext.FellingLicenceApplications.Add(application);
        _fellingLicenceApplicationsContext.SubmittedFlaPropertyDetails.Add(propertyDetail);
        await _fellingLicenceApplicationsContext.SaveChangesAsync();

        // Act
        var result = await _sut.GetSubmittedFlaPropertyCompartmentsByApplicationIdAsync(application.Id, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task GetSubmittedFlaPropertyCompartmentsByApplicationIdAsync_ReturnsFailure_WhenDetailNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _sut.GetSubmittedFlaPropertyCompartmentsByApplicationIdAsync(nonExistentId, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("not found", result.Error);
    }
}