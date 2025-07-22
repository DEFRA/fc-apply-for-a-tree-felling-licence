using AutoFixture;
using Forestry.Flo.External.Web.Models;
using Forestry.Flo.External.Web.Models.Compartment;
using Forestry.Flo.External.Web.Models.PropertyProfile;
using Forestry.Flo.External.Web.Services;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FileStorage.Model;
using Forestry.Flo.Services.PropertyProfiles.Entities;
using Forestry.Flo.Tests.Common;
using Microsoft.AspNetCore.Http;

namespace Forestry.Flo.External.Web.Tests.Services;

public class ModelMappingTests
{
    private static readonly Fixture FixtureInstance = new();
    private FormFileCollection _formFileCollection = new();

    [Theory, AutoMoqData]
    public void ShouldMapPropertyProfileModel_ToPropertyProfileEntity(PropertyProfileModel propertyProfileModel)
    {
        //Arrange
        //Act
        var result = ModelMapping.ToPropertyProfile(propertyProfileModel);

        //Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(propertyProfileModel.Id);
        result.Name.Should().Be(propertyProfileModel.Name);
        result.WoodlandOwnerId.Should().Be(propertyProfileModel.WoodlandOwnerId);
        result.NearestTown.Should().Be(propertyProfileModel.NearestTown);
        result.NameOfWood.Should().Be(propertyProfileModel.NameOfWood);
        result.HasWoodlandManagementPlan.Should().Be(propertyProfileModel.HasWoodlandManagementPlan!.Value);
        result.IsWoodlandCertificationScheme.Should().Be(propertyProfileModel.IsWoodlandCertificationScheme!.Value);
        result.OSGridReference.Should().Be(propertyProfileModel.OSGridReference);
        result.WoodlandManagementPlanReference.Should().Be(propertyProfileModel.WoodlandManagementPlanReference);
    }
    
    [Theory, AutoMoqData]
    public void ShouldMapPropertyProfileEntity_ToPropertyProfileModel(PropertyProfile propertyProfile)
    {
        //Arrange
        //Act
        var result = ModelMapping.ToPropertyProfileModel(propertyProfile);

        //Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(propertyProfile.Id);
        result.Name.Should().Be(propertyProfile.Name);
        result.WoodlandOwnerId.Should().Be(propertyProfile.WoodlandOwnerId);
        result.NearestTown.Should().Be(propertyProfile.NearestTown);
        result.NameOfWood.Should().Be(propertyProfile.NameOfWood);
        result.HasWoodlandManagementPlan.Should().Be(propertyProfile.HasWoodlandManagementPlan);
        result.IsWoodlandCertificationScheme.Should().Be(propertyProfile.IsWoodlandCertificationScheme);
        result.OSGridReference.Should().Be(propertyProfile.OSGridReference);
        result.WoodlandManagementPlanReference.Should().Be(propertyProfile.WoodlandManagementPlanReference);
    }
    
    [Theory, AutoMoqData]
    public void ShouldMapCompartmentModel_ToCompartmentEntity(CompartmentModel compartmentModel)
    {
        //Act
        var result = ModelMapping.ToCompartment(compartmentModel);

        //Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(compartmentModel.Id);
        result.Designation.Should().Be(compartmentModel.Designation);
        result.CompartmentNumber.Should().Be(compartmentModel.CompartmentNumber);
        result.TotalHectares.Should().Be(compartmentModel.TotalHectares);
        result.SubCompartmentName.Should().Be(compartmentModel.SubCompartmentName);
        result.GISData.Should().Be(compartmentModel.GISData);
    }
    
    [Theory, AutoMoqData]
    public void ShouldMapCompartmentEntity_ToCompartmentModel(TestCompartment compartment, PropertyProfile propertyProfile)
    {
        //Arrange
        compartment.SetProperty(propertyProfile);
        //Act
        var result = ModelMapping.ToCompartmentModel(compartment);

        //Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(compartment.Id);
        result.Designation.Should().Be(compartment.Designation);
        result.CompartmentNumber.Should().Be(compartment.CompartmentNumber);
        result.TotalHectares.Should().Be(compartment.TotalHectares);
        result.SubCompartmentName.Should().Be(compartment.SubCompartmentName);
        result.GISData.Should().Be(compartment.GISData);
        result.PropertyProfileName.Should().Be(compartment.PropertyProfile.Name);
    }

    [Theory, AutoMoqData]
    public void ShouldMapProposedFellingDetail_ToProposedFellingDetailModel(ProposedFellingDetail proposedFellingDetail, double totalHectares)
    {
        //arrange
        var speciesList = TreeSpeciesFactory.SpeciesDictionary.Values.ToArray();
        for (var i = 0; i < proposedFellingDetail.FellingSpecies?.Count; i++)
        {
            proposedFellingDetail.FellingSpecies[i].Species = speciesList[i].Code;
        }

        var index = 0;

        for (var j = 0; j < proposedFellingDetail.ProposedRestockingDetails.Count; j++)
        {
            for (var k = 0; k < proposedFellingDetail.ProposedRestockingDetails[j].RestockingSpecies.Count; k++)
            {
                proposedFellingDetail.ProposedRestockingDetails[j].RestockingSpecies[k].Species = speciesList[index].Code;
                index++;
            }
        }

        //act
        var result = ModelMapping.ToProposedFellingDetailModel(proposedFellingDetail, totalHectares);

        //assert
        result.Id.Should().Be(proposedFellingDetail.Id);
        result.Species[speciesList.First().Code].Species.Should().Be(speciesList.First().Code);
        result.Species[speciesList.First().Code].SpeciesName.Should().Be(speciesList.First().Name);
        result.ProposedRestockingDetails.First().Species[speciesList.First().Code].SpeciesName.Should().Be(speciesList.First().Name);
        result.OperationType.Should().Be(proposedFellingDetail.OperationType);
        result.TreeMarking.Should().Be(proposedFellingDetail.TreeMarking);
        result.NumberOfTrees.Should().Be(proposedFellingDetail.NumberOfTrees);
        result.AreaToBeFelled.Should().Be(proposedFellingDetail.AreaToBeFelled);
        result.IsWithinConservationArea.Should().Be(proposedFellingDetail.IsWithinConservationArea);
        result.IsPartOfTreePreservationOrder.Should().Be(proposedFellingDetail.IsPartOfTreePreservationOrder);
        result.CompartmentTotalHectares.Should().Be(totalHectares);
    }
    
    [Theory, AutoMoqData]
    public void ShouldMapProposedRestockingDetail_ToProposedFellingDetailModel(ProposedRestockingDetail proposedRestockingDetail, double totalHectares)
    {
        //arrange
        var speciesList = TreeSpeciesFactory.SpeciesDictionary.Values.ToArray();
        for (var i = 0; i < proposedRestockingDetail.RestockingSpecies?.Count; i++)
        {
            proposedRestockingDetail.RestockingSpecies[i].Species = speciesList[i].Code;
        } 
        
        //act
        var result = ModelMapping.ToProposedRestockingDetailModel(proposedRestockingDetail, totalHectares);

        //assert
        result.Id.Should().Be(proposedRestockingDetail.Id);
        result.Species[speciesList.First().Code].Species.Should().Be(speciesList.First().Code);
        result.Species[speciesList.First().Code].SpeciesName.Should().Be(speciesList.First().Name);
        result.Area.Should().Be(proposedRestockingDetail.Area);
        result.RestockingDensity.Should().Be(proposedRestockingDetail.RestockingDensity);
        result.RestockingProposal.Should().Be(proposedRestockingDetail.RestockingProposal);
        result.PercentageOfRestockArea.Should().Be(proposedRestockingDetail.PercentageOfRestockArea);
        result.CompartmentTotalHectares.Should().Be(totalHectares);
    }
    
    [Theory, AutoMoqData]
    public void ShouldMapAddressModel_ToAddressEntity(Address address)
    {
        //arrange
        //act
        var result = ModelMapping.ToAddressEntity(address);

        //assert
        result.PostalCode.Should().Be(address.PostalCode);
        result.Line1.Should().Be(address.Line1);
        result.Line2.Should().Be(address.Line2);
        result.Line3.Should().Be(address.Line3);
        result.Line4.Should().Be(address.Line4);
    }

    [Theory, AutoMoqData]
    public void ShouldMapDocumentEntity_ToDocumentsModelForApplicantView(List<Document> documentEntities)
    {
        //ensure at least one is visible
        documentEntities.First().VisibleToApplicant = true;

        //Arrange
        var userDocumentEntities = documentEntities
            .Where(x => x.VisibleToApplicant).ToList();

        //Act
        var result = ModelMapping.ToDocumentsModelForApplicantView(documentEntities);

        //Assert
        result.Should().NotBeNull();
        result.Count().Should().Be(userDocumentEntities.Count);

        var orderedModelList = result.OrderBy(x=>x.Id).ToList();
        var orderedEntityList = documentEntities.OrderBy(x => x.Id).ToList();

        for (var i = 0; i < userDocumentEntities.Count; i++)
        {
            orderedModelList[i].Id.Should().Be(orderedEntityList[i].Id);
            orderedModelList[i].CreatedTimestamp.Should().Be(orderedEntityList[i].CreatedTimestamp);
            orderedModelList[i].MimeType.Should().Be(orderedEntityList[i].MimeType);
            orderedModelList[i].FileSize.Should().Be(orderedEntityList[i].FileSize);
            orderedModelList[i].FileName.Should().Be(orderedEntityList[i].FileName);
            orderedModelList[i].FileType.Should().Be(orderedEntityList[i].FileType);
            orderedModelList[i].VisibleToApplicant.Should().Be(orderedEntityList[i].VisibleToApplicant);
            orderedModelList[i].VisibleToConsultee.Should().Be(orderedEntityList[i].VisibleToConsultee);
            orderedModelList[i].DocumentPurpose.Should().Be(orderedEntityList[i].Purpose);
            orderedModelList[i].VisibleToApplicant.Should().BeTrue();
        }
    }
    
    public void ShouldMapPropertyProfile_ToSubmittedFlaPropertyDetail(SubmittedFlaPropertyDetail proposedRestockingDetail, PropertyProfile propertyProfile)
    {
        //arrange


        //act
        var result = ModelMapping.ToSubmittedFlaPropertyDetail(propertyProfile);

        //assert

        // Testing only configured PropertyProfileId mapping

        result.PropertyProfileId.Should().Be(propertyProfile.Id);
    }

    [Theory, AutoMoqData]
    public void ShouldMapCompartment_ToSubmittedFlaPropertyCompartment(Compartment compartment)
    {
        //arrange

        var compartmentList = new List<Compartment> { compartment };

        //act
        var result = ModelMapping.ToSubmittedFlaPropertyCompartmentList(compartmentList);

        //assert

        // Testing only configured CompartmentId mapping

        Assert.True(result.All(x => compartmentList.Any(y => y.Id == x.CompartmentId)));
    }

    [Fact]
    public void ShouldMapFormFileCollectionToFilesToStoreModels()
    {
        //arrange (easier to seed using the model to create test data for ease of asserting, as handily has the fields to assert)
        var testData = FixtureInstance.CreateMany<FileToStoreModel>(3).ToList();
        foreach (var fileToStoreModel in testData)
        {
            AddFileToFormCollection(fileToStoreModel.FileName, fileToStoreModel.FileBytes, fileToStoreModel.ContentType);
        }
        
        //act
        var result = ModelMapping.ToFileToStoreModel(_formFileCollection);

        //assert
        Assert.IsAssignableFrom<IReadOnlyCollection<FileToStoreModel>>(result);
        Assert.Equal(3, result.Count);

        for (var i = 0; i < result.Count; i++)
        {
                Assert.Equal(testData[i].FileName, result[i].FileName);
                Assert.Equal(testData[i].ContentType, result[i].ContentType);
                Assert.Equal(testData[i].FileBytes, result[i].FileBytes);
        }
    }

    [Fact]
    public void HandlesEmptyFormFileCollection_WhenMappingToFilesToStoreModels()
    {
        //arrange
        //act
        var result = ModelMapping.ToFileToStoreModel(new FormFileCollection());

        //assert
        Assert.IsAssignableFrom<IReadOnlyCollection<FileToStoreModel>>(result);
        Assert.Empty(result);
    }

    [Fact]
    public void HandlesEmptyFormFileInFormFileCollection_WhenMappingToFilesToStoreModels()
    {
        //arrange (easier to seed using the model to create test data for ease of asserting, as handily has the fields to assert)
        var testData = FixtureInstance.Create<FileToStoreModel>();
        testData.FileBytes = Array.Empty<byte>();
        AddFileToFormCollection(testData.FileName, testData.FileBytes, testData.ContentType);
            
        //act
        var result = ModelMapping.ToFileToStoreModel(_formFileCollection);

        //assert
        Assert.IsAssignableFrom<IReadOnlyCollection<FileToStoreModel>>(result);
        Assert.Single(result);
        Assert.Equal(testData.FileName, result[0].FileName);
        Assert.Equal(testData.ContentType, result[0].ContentType);
        Assert.Equal(testData.FileBytes, result[0].FileBytes);
    }

    public class TestCompartment : Compartment
    {
        private readonly Fixture _fixture = new();
        
        public void SetProperty(PropertyProfile propertyProfile)
        {
            PropertyProfile = propertyProfile;
        }
        public void SetId(Guid id)
        {
            Id = id;
        }

        public TestCompartment(Guid id)
        {
            Id = id;
            CompartmentNumber = _fixture.Create<int>().ToString();
            SubCompartmentName = _fixture.Create<string>();
        }
    }

    private void AddFileToFormCollection(string fileName, byte[] fileBytes, string fileContentType)
    {
        var f = new FormFile(new MemoryStream(fileBytes), 0, fileBytes.Length, "test", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = fileContentType
        };

        _formFileCollection.Add(f);
    }
}