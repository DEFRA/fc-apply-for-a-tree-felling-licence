using AutoFixture;
using Forestry.Flo.Internal.Web.Services;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Tests.Common;
using FluentAssertions;
using Forestry.Flo.Internal.Web.Models.Reports;
using Forestry.Flo.Services.Applicants.Entities.WoodlandOwner;
using Forestry.Flo.Services.FellingLicenceApplications.Models.Reports;

namespace Forestry.Flo.Internal.Web.Tests.Services;

public class ModelMappingTests
{
    [Theory, AutoMoqData]
    public void ShouldMapProposedFellingDetail_ToProposedFellingDetailModel(ProposedFellingDetail proposedFellingDetail)
    {
        //arrange
        var speciesList = TreeSpeciesFactory.SpeciesDictionary.Values.ToArray();
        for (var i = 0; i < proposedFellingDetail.FellingSpecies?.Count; i++)
        {
            proposedFellingDetail.FellingSpecies[i].Species = speciesList[i].Code;
        }

        //act
        var result = ModelMapping.ToProposedFellingDetailModel(proposedFellingDetail);

        //assert
        result.Id.Should().Be(proposedFellingDetail.Id);
        result.Species[speciesList.First().Code].Species.Should().Be(speciesList.First().Code);
        result.Species[speciesList.First().Code].SpeciesName.Should().Be(speciesList.First().Name);
        result.OperationType.Should().Be(proposedFellingDetail.OperationType);
        result.TreeMarking.Should().Be(proposedFellingDetail.TreeMarking);
        result.NumberOfTrees.Should().Be(proposedFellingDetail.NumberOfTrees);
        result.AreaToBeFelled.Should().Be(proposedFellingDetail.AreaToBeFelled);
        result.IsWithinConservationArea.Should().Be(proposedFellingDetail.IsWithinConservationArea);
        result.IsPartOfTreePreservationOrder.Should().Be(proposedFellingDetail.IsPartOfTreePreservationOrder);
    }

    [Theory, AutoMoqData]
    public void ShouldMapProposedRestockingDetail_ToProposedFellingDetailModel(ProposedRestockingDetail proposedRestockingDetail)
    {
        //arrange
        var speciesList = TreeSpeciesFactory.SpeciesDictionary.Values.ToArray();
        for (var i = 0; i < proposedRestockingDetail.RestockingSpecies?.Count; i++)
        {
            proposedRestockingDetail.RestockingSpecies[i].Species = speciesList[i].Code;
        }

        //act
        var result = ModelMapping.ToProposedRestockingDetailModel(proposedRestockingDetail);

        //assert
        result.Id.Should().Be(proposedRestockingDetail.Id);
        result.Species[speciesList.First().Code].Species.Should().Be(speciesList.First().Code);
        result.Species[speciesList.First().Code].SpeciesName.Should().Be(speciesList.First().Name);
        result.Area.Should().Be(proposedRestockingDetail.Area);
        result.RestockingDensity.Should().Be(proposedRestockingDetail.RestockingDensity);
        result.RestockingProposal.Should().Be(proposedRestockingDetail.RestockingProposal);
        result.PercentageOfRestockArea.Should().Be((int?)proposedRestockingDetail.PercentageOfRestockArea);
    }

    [Theory, AutoMoqData]
    public void ShouldMapDocumentEntityList_ToDocumentModelList(List<Document> documentEntities)
    {
        //Arrange
        //Act
        var result = ModelMapping.ToDocumentModelList(documentEntities).ToList();

        //Assert
        result.Should().NotBeNull();
        result.Count.Should().Be(documentEntities.Count);

        var orderedModelList = result.OrderBy(x => x.Id).ToList();
        var orderedEntityList = documentEntities.OrderBy(x => x.Id).ToList();

        for (var i = 0; i < documentEntities.Count; i++)
        {
            orderedModelList[i].Id.Should().Be(orderedEntityList[i].Id);
            orderedModelList[i].CreatedTimestamp.Should().Be(orderedEntityList[i].CreatedTimestamp);
            orderedModelList[i].MimeType.Should().Be(orderedEntityList[i].MimeType);
            orderedModelList[i].FileSize.Should().Be(orderedEntityList[i].FileSize);
            orderedModelList[i].FileName.Should().Be(orderedEntityList[i].FileName);
            orderedModelList[i].FileType.Should().Be(orderedEntityList[i].FileType);
        }
    }
    
    [Theory, AutoMoqData]
    public void ShouldMapStatusHistoryEntityList_ToStatusHistoryModelList(List<StatusHistory> statusHistories)
    {
        //Arrange
        //Act
        var result = ModelMapping.ToStatusHistoryModelList(statusHistories).ToList();

        //Assert
        result.Should().NotBeNull();
        result.Count.Should().Be(statusHistories.Count);

        var orderedModelList = result.OrderBy(x => x.Id).ToList();
        var orderedEntityList = statusHistories.OrderBy(x => x.Id).ToList();

        for (var i = 0; i < statusHistories.Count; i++)
        {
            orderedModelList[i].Id.Should().Be(orderedEntityList[i].Id);
            orderedModelList[i].Created.Should().Be(orderedEntityList[i].Created);
            orderedModelList[i].Status.Should().Be(orderedEntityList[i].Status);
            orderedModelList[i].FellingLicenceApplicationId.Should().Be(orderedEntityList[i].FellingLicenceApplicationId);
        }
    }
    
    [Theory, AutoMoqData]
    public void ShouldMapCaseNoteEntityList_ToCaseNoteModelList(List<CaseNote> caseNoteEntities)
    {
        //Arrange
        //Act
        var result = ModelMapping.ToCaseNoteModelList(caseNoteEntities).ToList();

        //Assert
        result.Should().NotBeNull();
        result.Count.Should().Be(caseNoteEntities.Count);

        var orderedModelList = result.OrderBy(x => x.Id).ToList();
        var orderedEntityList = caseNoteEntities.OrderBy(x => x.Id).ToList();

        for (var i = 0; i < caseNoteEntities.Count; i++)
        {
            orderedModelList[i].Id.Should().Be(orderedEntityList[i].Id);
            orderedModelList[i].CreatedTimestamp.Should().Be(orderedEntityList[i].CreatedTimestamp);
            orderedModelList[i].Text.Should().Be(orderedEntityList[i].Text);
            orderedModelList[i].Type.Should().Be(orderedEntityList[i].Type);
            orderedModelList[i].CreatedByUserId.Should().Be(orderedEntityList[i].CreatedByUserId);
            orderedModelList[i].VisibleToApplicant.Should().Be(orderedEntityList[i].VisibleToApplicant);
            orderedModelList[i].VisibleToConsultee.Should().Be(orderedEntityList[i].VisibleToConsultee);
        }
    }
    
    [Theory, AutoMoqData]
    public void ShouldMapExternalAccessLinkList_ToExternalInviteLinkList_NoCommentsProvided(List<ExternalAccessLink> accessLinks)
    {
        //Arrange
        //Act
        var result = ModelMapping.ToExternalInviteLinkList(accessLinks, []).ToList();

        //Assert
        result.Should().NotBeNull();
        result.Count.Should().Be(accessLinks.Count);

        var orderedModelList = result.OrderBy(x => x.Id).ToList();
        var orderedEntityList = accessLinks.OrderBy(x => x.Id).ToList();

        for (var i = 0; i < accessLinks.Count; i++)
        {
            orderedModelList[i].Id.Should().Be(orderedEntityList[i].Id);
            orderedModelList[i].Name.Should().Be(orderedEntityList[i].Name);
            orderedModelList[i].Purpose.Should().Be(orderedEntityList[i].Purpose);
            orderedModelList[i].ContactEmail.Should().Be(orderedEntityList[i].ContactEmail);
            orderedModelList[i].CreatedTimeStamp.Should().Be(orderedEntityList[i].CreatedTimeStamp);
            orderedModelList[i].ExpiresTimeStamp.Should().Be(orderedEntityList[i].ExpiresTimeStamp);
            orderedModelList[i].LinkType.Should().Be(orderedEntityList[i].LinkType);
            orderedModelList[i].HasResponded.Should().BeFalse();
        }
    }

    [Theory, AutoMoqData]
    public void ShouldMapExternalAccessLinkList_ToExternalInviteLinkList_CommentsProvided(List<ExternalAccessLink> accessLinks)
    {
        //Arrange

        var fixture = new Fixture();

        var comments = new List<ConsulteeComment>();
        foreach (var accessLink in accessLinks)
        {
            var numberOfComments = fixture.Create<int>() % 5 + 1;
            for (var i = 0; i < numberOfComments; i++)
            {
                comments.Add(fixture.Build<ConsulteeComment>()
                    .With(x => x.AccessCode, accessLink.AccessCode)
                    .Create());
            }
        }

        //Act
        var result = ModelMapping.ToExternalInviteLinkList(accessLinks, comments).ToList();

        //Assert
        result.Should().NotBeNull();
        result.Count.Should().Be(accessLinks.Count);

        var orderedModelList = result.OrderBy(x => x.Id).ToList();
        var orderedEntityList = accessLinks.OrderBy(x => x.Id).ToList();

        for (var i = 0; i < accessLinks.Count; i++)
        {
            orderedModelList[i].Id.Should().Be(orderedEntityList[i].Id);
            orderedModelList[i].Name.Should().Be(orderedEntityList[i].Name);
            orderedModelList[i].Purpose.Should().Be(orderedEntityList[i].Purpose);
            orderedModelList[i].ContactEmail.Should().Be(orderedEntityList[i].ContactEmail);
            orderedModelList[i].CreatedTimeStamp.Should().Be(orderedEntityList[i].CreatedTimeStamp);
            orderedModelList[i].ExpiresTimeStamp.Should().Be(orderedEntityList[i].ExpiresTimeStamp);
            orderedModelList[i].LinkType.Should().Be(orderedEntityList[i].LinkType);
            orderedModelList[i].HasResponded.Should().BeTrue();
        }
    }

    [Theory, AutoMoqData]
    public void ShouldMapWoodlandOwnerEntity_ToWoodlandOwnerModel(WoodlandOwner woodlandOwner)
    {
        //Arrange
        //Act
        var result = ModelMapping.ToWoodlandOwnerModel(woodlandOwner);

        //Assert
        result.Should().NotBeNull();
        result.ContactAddress.Should().BeEquivalentTo(woodlandOwner.ContactAddress);
        result.OrganisationAddress.Should().BeEquivalentTo(woodlandOwner.OrganisationAddress);
        result.ContactEmail.Should().Be(woodlandOwner.ContactEmail);
        result.ContactName.Should().Be(woodlandOwner.ContactName);
        result.IsOrganisation.Should().Be(woodlandOwner.IsOrganisation);
        result.OrganisationName.Should().Be(woodlandOwner.OrganisationName);
        result.ContactAddressMatchesOrganisationAddress.Should().Be(woodlandOwner.ContactAddress == woodlandOwner.OrganisationAddress);
    }

    [Fact]
    public void ShouldMapFellingOperationTypeForReport_To_EntityEnum()
    {
        FellingOperationTypeForReporting.None.ToFellingOperationType().Should().Be(FellingOperationType.None);
        FellingOperationTypeForReporting.FellingOfCoppice.ToFellingOperationType().Should().Be(FellingOperationType.FellingOfCoppice);
        FellingOperationTypeForReporting.RegenerationFelling.ToFellingOperationType().Should().Be(FellingOperationType.RegenerationFelling);
        FellingOperationTypeForReporting.Thinning.ToFellingOperationType().Should().Be(FellingOperationType.Thinning);
        FellingOperationTypeForReporting.FellingIndividualTrees.ToFellingOperationType().Should().Be(FellingOperationType.FellingIndividualTrees);
        FellingOperationTypeForReporting.ClearFelling.ToFellingOperationType().Should().Be(FellingOperationType.ClearFelling);
    }

    [Fact]
    public void ShouldMapDateRangeTypeForReporting_To_ServiceReportEnum()
    {
        DateRangeTypeForReporting.ApprovedDate.ToReportDateRangeType().Should().Be(ReportDateRangeType.Approved);
        DateRangeTypeForReporting.SubmittedDate.ToReportDateRangeType().Should().Be(ReportDateRangeType.Submitted);
        DateRangeTypeForReporting.OnPublicRegister.ToReportDateRangeType().Should().Be(ReportDateRangeType.OnPublicRegister);
        DateRangeTypeForReporting.OffPublicRegister.ToReportDateRangeType().Should().Be(ReportDateRangeType.OffPublicRegister);
        DateRangeTypeForReporting.PublicRegisterExpiry.ToReportDateRangeType().Should().Be(ReportDateRangeType.PublicRegisterExpiry);
        DateRangeTypeForReporting.CitizensCharter.ToReportDateRangeType().Should().Be(ReportDateRangeType.CitizensCharter);
        DateRangeTypeForReporting.FinalAction.ToReportDateRangeType().Should().Be(ReportDateRangeType.FinalAction);
    }

    [Fact]
    public void ShouldMapFellingLicenceApplicationStatusesForReporting_To_EntityEnum()
    {
        FellingLicenceApplicationStatusesForReporting.Draft.ToEntityFellingLicenceStatus().Should().Be(FellingLicenceStatus.Draft);
        FellingLicenceApplicationStatusesForReporting.Submitted.ToEntityFellingLicenceStatus().Should().Be(FellingLicenceStatus.Submitted);
        FellingLicenceApplicationStatusesForReporting.Received.ToEntityFellingLicenceStatus().Should().Be(FellingLicenceStatus.Received);
        FellingLicenceApplicationStatusesForReporting.WoodlandOfficerReview.ToEntityFellingLicenceStatus().Should().Be(FellingLicenceStatus.WoodlandOfficerReview);
        FellingLicenceApplicationStatusesForReporting.WithApplicant.ToEntityFellingLicenceStatus().Should().Be(FellingLicenceStatus.WithApplicant);
        FellingLicenceApplicationStatusesForReporting.ReturnedToApplicant.ToEntityFellingLicenceStatus().Should().Be(FellingLicenceStatus.ReturnedToApplicant);
        FellingLicenceApplicationStatusesForReporting.Approved.ToEntityFellingLicenceStatus().Should().Be(FellingLicenceStatus.Approved);
        FellingLicenceApplicationStatusesForReporting.Refused.ToEntityFellingLicenceStatus().Should().Be(FellingLicenceStatus.Refused);
        FellingLicenceApplicationStatusesForReporting.SentForApproval.ToEntityFellingLicenceStatus().Should().Be(FellingLicenceStatus.SentForApproval);
        FellingLicenceApplicationStatusesForReporting.Withdrawn.ToEntityFellingLicenceStatus().Should().Be(FellingLicenceStatus.Withdrawn);
    }
}