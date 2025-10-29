using AutoFixture;
using Forestry.Flo.Internal.Web.Services;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Tests.Common;
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
        Assert.Equal(proposedFellingDetail.Id, result.Id);
        Assert.Equal(speciesList.First().Code, result.Species[speciesList.First().Code].Species);
        Assert.Equal(speciesList.First().Name, result.Species[speciesList.First().Code].SpeciesName);
        Assert.Equal(proposedFellingDetail.OperationType, result.OperationType);
        Assert.Equal(proposedFellingDetail.TreeMarking, result.TreeMarking);
        Assert.Equal(proposedFellingDetail.NumberOfTrees, result.NumberOfTrees);
        Assert.Equal(proposedFellingDetail.AreaToBeFelled, result.AreaToBeFelled);
        Assert.Equal(proposedFellingDetail.IsWithinConservationArea, result.IsWithinConservationArea);
        Assert.Equal(proposedFellingDetail.IsPartOfTreePreservationOrder, result.IsPartOfTreePreservationOrder);
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
        Assert.Equal(proposedRestockingDetail.Id, result.Id);
        Assert.Equal(speciesList.First().Code, result.Species[speciesList.First().Code].Species);
        Assert.Equal(speciesList.First().Name, result.Species[speciesList.First().Code].SpeciesName);
        Assert.Equal(proposedRestockingDetail.Area, result.Area);
        Assert.Equal(proposedRestockingDetail.RestockingDensity, result.RestockingDensity);
        Assert.Equal(proposedRestockingDetail.RestockingProposal, result.RestockingProposal);
        Assert.Equal((int?)proposedRestockingDetail.PercentageOfRestockArea, result.PercentageOfRestockArea);
    }

    [Theory, AutoMoqData]
    public void ShouldMapDocumentEntityList_ToDocumentModelList(List<Document> documentEntities)
    {
        //Arrange
        //Act
        var result = ModelMapping.ToDocumentModelList(documentEntities).ToList();

        //Assert
        Assert.NotNull(result);
        Assert.Equal(documentEntities.Count, result.Count);

        var orderedModelList = result.OrderBy(x => x.Id).ToList();
        var orderedEntityList = documentEntities.OrderBy(x => x.Id).ToList();

        for (var i = 0; i < documentEntities.Count; i++)
        {
            Assert.Equal(orderedEntityList[i].Id, orderedModelList[i].Id);
            Assert.Equal(orderedEntityList[i].CreatedTimestamp, orderedModelList[i].CreatedTimestamp);
            Assert.Equal(orderedEntityList[i].MimeType, orderedModelList[i].MimeType);
            Assert.Equal(orderedEntityList[i].FileSize, orderedModelList[i].FileSize);
            Assert.Equal(orderedEntityList[i].FileName, orderedModelList[i].FileName);
            Assert.Equal(orderedEntityList[i].FileType, orderedModelList[i].FileType);
        }
    }
    
    [Theory, AutoMoqData]
    public void ShouldMapStatusHistoryEntityList_ToStatusHistoryModelList(List<StatusHistory> statusHistories)
    {
        //Arrange
        //Act
        var result = ModelMapping.ToStatusHistoryModelList(statusHistories).ToList();

        //Assert
        Assert.NotNull(result);
        Assert.Equal(statusHistories.Count, result.Count);

        var orderedModelList = result.OrderBy(x => x.Id).ToList();
        var orderedEntityList = statusHistories.OrderBy(x => x.Id).ToList();

        for (var i = 0; i < statusHistories.Count; i++)
        {
            Assert.Equal(orderedEntityList[i].Id, orderedModelList[i].Id);
            Assert.Equal(orderedEntityList[i].Created, orderedModelList[i].Created);
            Assert.Equal(orderedEntityList[i].Status, orderedModelList[i].Status);
            Assert.Equal(orderedEntityList[i].FellingLicenceApplicationId, orderedModelList[i].FellingLicenceApplicationId);
        }
    }
    
    [Theory, AutoMoqData]
    public void ShouldMapCaseNoteEntityList_ToCaseNoteModelList(List<CaseNote> caseNoteEntities)
    {
        //Arrange
        //Act
        var result = ModelMapping.ToCaseNoteModelList(caseNoteEntities).ToList();

        //Assert
        Assert.NotNull(result);
        Assert.Equal(caseNoteEntities.Count, result.Count);

        var orderedModelList = result.OrderBy(x => x.Id).ToList();
        var orderedEntityList = caseNoteEntities.OrderBy(x => x.Id).ToList();

        for (var i = 0; i < caseNoteEntities.Count; i++)
        {
            Assert.Equal(orderedEntityList[i].Id, orderedModelList[i].Id);
            Assert.Equal(orderedEntityList[i].CreatedTimestamp, orderedModelList[i].CreatedTimestamp);
            Assert.Equal(orderedEntityList[i].Text, orderedModelList[i].Text);
            Assert.Equal(orderedEntityList[i].Type, orderedModelList[i].Type);
            Assert.Equal(orderedEntityList[i].CreatedByUserId, orderedModelList[i].CreatedByUserId);
            Assert.Equal(orderedEntityList[i].VisibleToApplicant, orderedModelList[i].VisibleToApplicant);
            Assert.Equal(orderedEntityList[i].VisibleToConsultee, orderedModelList[i].VisibleToConsultee);
        }
    }
    
    [Theory, AutoMoqData]
    public void ShouldMapExternalAccessLinkList_ToExternalInviteLinkList_NoCommentsProvided(List<ExternalAccessLink> accessLinks)
    {
        //Arrange
        //Act
        var result = ModelMapping.ToExternalInviteLinkList(accessLinks, []).ToList();

        //Assert
        Assert.NotNull(result);
        Assert.Equal(accessLinks.Count, result.Count);

        var orderedModelList = result.OrderBy(x => x.Id).ToList();
        var orderedEntityList = accessLinks.OrderBy(x => x.Id).ToList();

        for (var i = 0; i < accessLinks.Count; i++)
        {
            Assert.Equal(orderedEntityList[i].Id, orderedModelList[i].Id);
            Assert.Equal(orderedEntityList[i].Name, orderedModelList[i].Name);
            Assert.Equal(orderedEntityList[i].Purpose, orderedModelList[i].Purpose);
            Assert.Equal(orderedEntityList[i].ContactEmail, orderedModelList[i].ContactEmail);
            Assert.Equal(orderedEntityList[i].CreatedTimeStamp, orderedModelList[i].CreatedTimeStamp);
            Assert.Equal(orderedEntityList[i].ExpiresTimeStamp, orderedModelList[i].ExpiresTimeStamp);
            Assert.Equal(orderedEntityList[i].LinkType, orderedModelList[i].LinkType);
            Assert.False(orderedModelList[i].HasResponded);
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
        Assert.NotNull(result);
        Assert.Equal(accessLinks.Count, result.Count);

        var orderedModelList = result.OrderBy(x => x.Id).ToList();
        var orderedEntityList = accessLinks.OrderBy(x => x.Id).ToList();

        for (var i = 0; i < accessLinks.Count; i++)
        {
            Assert.Equal(orderedEntityList[i].Id, orderedModelList[i].Id);
            Assert.Equal(orderedEntityList[i].Name, orderedModelList[i].Name);
            Assert.Equal(orderedEntityList[i].Purpose, orderedModelList[i].Purpose);
            Assert.Equal(orderedEntityList[i].ContactEmail, orderedModelList[i].ContactEmail);
            Assert.Equal(orderedEntityList[i].CreatedTimeStamp, orderedModelList[i].CreatedTimeStamp);
            Assert.Equal(orderedEntityList[i].ExpiresTimeStamp, orderedModelList[i].ExpiresTimeStamp);
            Assert.Equal(orderedEntityList[i].LinkType, orderedModelList[i].LinkType);
            Assert.True(orderedModelList[i].HasResponded);
        }
    }

    [Theory, AutoMoqData]
    public void ShouldMapWoodlandOwnerEntity_ToWoodlandOwnerModel(WoodlandOwner woodlandOwner)
    {
        //Arrange
        //Act
        var result = ModelMapping.ToWoodlandOwnerModel(woodlandOwner);

        //Assert
        Assert.NotNull(result);
        Assert.Equivalent(woodlandOwner.ContactAddress, result.ContactAddress);
        Assert.Equivalent(woodlandOwner.OrganisationAddress, result.OrganisationAddress);
        Assert.Equal(woodlandOwner.ContactEmail, result.ContactEmail);
        Assert.Equal(woodlandOwner.ContactName, result.ContactName);
        Assert.Equal(woodlandOwner.IsOrganisation, result.IsOrganisation);
        Assert.Equal(woodlandOwner.OrganisationName, result.OrganisationName);
        Assert.Equal(woodlandOwner.ContactAddress == woodlandOwner.OrganisationAddress, result.ContactAddressMatchesOrganisationAddress);
    }

    [Fact]
    public void ShouldMapFellingOperationTypeForReport_To_EntityEnum()
    {
        Assert.Equal(FellingOperationType.None, FellingOperationTypeForReporting.None.ToFellingOperationType());
        Assert.Equal(FellingOperationType.FellingOfCoppice, FellingOperationTypeForReporting.FellingOfCoppice.ToFellingOperationType());
        Assert.Equal(FellingOperationType.RegenerationFelling, FellingOperationTypeForReporting.RegenerationFelling.ToFellingOperationType());
    Assert.Equal(FellingOperationType.Thinning, FellingOperationTypeForReporting.Thinning.ToFellingOperationType());
        Assert.Equal(FellingOperationType.FellingIndividualTrees, FellingOperationTypeForReporting.FellingIndividualTrees.ToFellingOperationType());
        Assert.Equal(FellingOperationType.ClearFelling, FellingOperationTypeForReporting.ClearFelling.ToFellingOperationType());
    }

    [Fact]
    public void ShouldMapDateRangeTypeForReporting_To_ServiceReportEnum()
    {
        Assert.Equal(ReportDateRangeType.Approved, DateRangeTypeForReporting.ApprovedDate.ToReportDateRangeType());
        Assert.Equal(ReportDateRangeType.Submitted, DateRangeTypeForReporting.SubmittedDate.ToReportDateRangeType());
        Assert.Equal(ReportDateRangeType.OnPublicRegister, DateRangeTypeForReporting.OnPublicRegister.ToReportDateRangeType());
        Assert.Equal(ReportDateRangeType.OffPublicRegister, DateRangeTypeForReporting.OffPublicRegister.ToReportDateRangeType());
        Assert.Equal(ReportDateRangeType.PublicRegisterExpiry, DateRangeTypeForReporting.PublicRegisterExpiry.ToReportDateRangeType());
        Assert.Equal(ReportDateRangeType.CitizensCharter, DateRangeTypeForReporting.CitizensCharter.ToReportDateRangeType());
        Assert.Equal(ReportDateRangeType.FinalAction, DateRangeTypeForReporting.FinalAction.ToReportDateRangeType());
    }

    [Fact]
    public void ShouldMapFellingLicenceApplicationStatusesForReporting_To_EntityEnum()
    {
        Assert.Equal(FellingLicenceStatus.Draft, FellingLicenceApplicationStatusesForReporting.Draft.ToEntityFellingLicenceStatus());
        Assert.Equal(FellingLicenceStatus.Submitted, FellingLicenceApplicationStatusesForReporting.Submitted.ToEntityFellingLicenceStatus());
        Assert.Equal(FellingLicenceStatus.Received, FellingLicenceApplicationStatusesForReporting.Received.ToEntityFellingLicenceStatus());
        Assert.Equal(FellingLicenceStatus.WoodlandOfficerReview, FellingLicenceApplicationStatusesForReporting.WoodlandOfficerReview.ToEntityFellingLicenceStatus());
        Assert.Equal(FellingLicenceStatus.WithApplicant, FellingLicenceApplicationStatusesForReporting.WithApplicant.ToEntityFellingLicenceStatus());
        Assert.Equal(FellingLicenceStatus.ReturnedToApplicant, FellingLicenceApplicationStatusesForReporting.ReturnedToApplicant.ToEntityFellingLicenceStatus());
        Assert.Equal(FellingLicenceStatus.Approved, FellingLicenceApplicationStatusesForReporting.Approved.ToEntityFellingLicenceStatus());
        Assert.Equal(FellingLicenceStatus.Refused, FellingLicenceApplicationStatusesForReporting.Refused.ToEntityFellingLicenceStatus());
        Assert.Equal(FellingLicenceStatus.SentForApproval, FellingLicenceApplicationStatusesForReporting.SentForApproval.ToEntityFellingLicenceStatus());
        Assert.Equal(FellingLicenceStatus.Withdrawn, FellingLicenceApplicationStatusesForReporting.Withdrawn.ToEntityFellingLicenceStatus());
    }
}