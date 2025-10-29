using AutoFixture;
using Forestry.Flo.External.Web.Models;
using Forestry.Flo.External.Web.Models.Compartment;
using Forestry.Flo.External.Web.Models.PropertyProfile;
using Forestry.Flo.External.Web.Services;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FileStorage.Model;
using Forestry.Flo.Services.Gis.Models.Internal.MapObjects;
using Forestry.Flo.Services.PropertyProfiles.Entities;
using Forestry.Flo.Tests.Common;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

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
        Assert.NotNull(result);
        Assert.Equal(propertyProfileModel.Id, result.Id);
        Assert.Equal(propertyProfileModel.Name, result.Name);
        Assert.Equal(propertyProfileModel.WoodlandOwnerId, result.WoodlandOwnerId);
        Assert.Equal(propertyProfileModel.NearestTown, result.NearestTown);
        Assert.Equal(propertyProfileModel.NameOfWood, result.NameOfWood);
        Assert.Equal(propertyProfileModel.HasWoodlandManagementPlan!.Value, result.HasWoodlandManagementPlan);
        Assert.Equal(propertyProfileModel.IsWoodlandCertificationScheme!.Value, result.IsWoodlandCertificationScheme);
        Assert.Equal(propertyProfileModel.OSGridReference, result.OSGridReference);
        Assert.Equal(propertyProfileModel.WoodlandManagementPlanReference, result.WoodlandManagementPlanReference);
    }
    
    [Theory, AutoMoqData]
    public void ShouldMapPropertyProfileEntity_ToPropertyProfileModel(PropertyProfile propertyProfile)
    {
        //Arrange
        //Act
        var result = ModelMapping.ToPropertyProfileModel(propertyProfile);

        //Assert
        Assert.NotNull(result);
        Assert.Equal(propertyProfile.Id, result.Id);
        Assert.Equal(propertyProfile.Name, result.Name);
        Assert.Equal(propertyProfile.WoodlandOwnerId, result.WoodlandOwnerId);
        Assert.Equal(propertyProfile.NearestTown, result.NearestTown);
        Assert.Equal(propertyProfile.NameOfWood, result.NameOfWood);
        Assert.Equal(propertyProfile.HasWoodlandManagementPlan, result.HasWoodlandManagementPlan);
        Assert.Equal(propertyProfile.IsWoodlandCertificationScheme, result.IsWoodlandCertificationScheme);
        Assert.Equal(propertyProfile.OSGridReference, result.OSGridReference);
        Assert.Equal(propertyProfile.WoodlandManagementPlanReference, result.WoodlandManagementPlanReference);
    }
    
    [Theory, AutoMoqData]
    public void ShouldMapCompartmentModel_ToCompartmentEntity(CompartmentModel compartmentModel)
    {
        //Act
        var result = ModelMapping.ToCompartment(compartmentModel);

        //Assert
        Assert.NotNull(result);
        Assert.Equal(compartmentModel.Id, result.Id);
        Assert.Equal(compartmentModel.CompartmentNumber, result.CompartmentNumber);
        Assert.Equal(compartmentModel.TotalHectares, result.TotalHectares);
        Assert.Equal(compartmentModel.SubCompartmentName, result.SubCompartmentName);
        Assert.Equal(compartmentModel.GISData, result.GISData);
    }
    
    [Theory, AutoMoqData]
    public void ShouldMapCompartmentEntity_ToCompartmentModel(TestCompartment compartment, PropertyProfile propertyProfile)
    {
        //Arrange
        compartment.SetProperty(propertyProfile);
        //Act
        var result = ModelMapping.ToCompartmentModel(compartment);

        //Assert
        Assert.NotNull(result);
        Assert.Equal(compartment.Id, result.Id);
        Assert.Equal(compartment.CompartmentNumber, result.CompartmentNumber);
        Assert.Equal(compartment.TotalHectares, result.TotalHectares);
        Assert.Equal(compartment.SubCompartmentName, result.SubCompartmentName);
        Assert.Equal(compartment.GISData, result.GISData);
        Assert.Equal(compartment.PropertyProfile.Name, result.PropertyProfileName);
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
        Assert.Equal(proposedFellingDetail.Id, result.Id);
        Assert.Equal(speciesList.First().Code, result.Species[speciesList.First().Code].Species);
        Assert.Equal(speciesList.First().Name, result.Species[speciesList.First().Code].SpeciesName);
        Assert.Equal(speciesList.First().Name, result.ProposedRestockingDetails.First().Species[speciesList.First().Code].SpeciesName);
        
        Assert.Equal(proposedFellingDetail.OperationType, result.OperationType);
        Assert.Equal(proposedFellingDetail.TreeMarking, result.TreeMarking);
        Assert.Equal(proposedFellingDetail.NumberOfTrees, result.NumberOfTrees);
        Assert.Equal(proposedFellingDetail.AreaToBeFelled, result.AreaToBeFelled);
        Assert.Equal(proposedFellingDetail.IsWithinConservationArea, result.IsWithinConservationArea);
        Assert.Equal(proposedFellingDetail.IsPartOfTreePreservationOrder, result.IsPartOfTreePreservationOrder);
        Assert.Equal(totalHectares, result.CompartmentTotalHectares);
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
        Assert.Equal(proposedRestockingDetail.Id, result.Id);
        Assert.Equal(speciesList.First().Code, result.Species[speciesList.First().Code].Species);
        Assert.Equal(speciesList.First().Name, result.Species[speciesList.First().Code].SpeciesName);
        Assert.Equal(proposedRestockingDetail.Area, result.Area);
        Assert.Equal(proposedRestockingDetail.RestockingDensity, result.RestockingDensity);
        Assert.Equal(proposedRestockingDetail.RestockingProposal, result.RestockingProposal);
        Assert.Equal(proposedRestockingDetail.PercentageOfRestockArea, result.PercentageOfRestockArea);
        Assert.Equal(totalHectares, result.CompartmentTotalHectares);
    }
    
    [Theory, AutoMoqData]
    public void ShouldMapAddressModel_ToAddressEntity(Address address)
    {
        //arrange
        //act
        var result = ModelMapping.ToAddressEntity(address);

        //assert
        Assert.Equal(address.PostalCode, result.PostalCode);
        Assert.Equal(address.Line1, result.Line1);
        Assert.Equal(address.Line2, result.Line2);
        Assert.Equal(address.Line3, result.Line3);
        Assert.Equal(address.Line4, result.Line4);
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
        Assert.NotNull(result);
        Assert.Equal(userDocumentEntities.Count, result.Count());

        var orderedModelList = result.OrderBy(x=>x.Id).ToList();
        var orderedEntityList = documentEntities.OrderBy(x => x.Id).ToList();

        for (var i = 0; i < userDocumentEntities.Count; i++)
        {
            Assert.Equal(orderedEntityList[i].Id, orderedModelList[i].Id);
            Assert.Equal(orderedEntityList[i].CreatedTimestamp, orderedModelList[i].CreatedTimestamp);
            Assert.Equal(orderedEntityList[i].MimeType, orderedModelList[i].MimeType);
            Assert.Equal(orderedEntityList[i].FileSize, orderedModelList[i].FileSize);
            Assert.Equal(orderedEntityList[i].FileName, orderedModelList[i].FileName);
            Assert.Equal(orderedEntityList[i].FileType, orderedModelList[i].FileType);
            Assert.Equal(orderedEntityList[i].VisibleToApplicant, orderedModelList[i].VisibleToApplicant);
            Assert.Equal(orderedEntityList[i].VisibleToConsultee, orderedModelList[i].VisibleToConsultee);
            Assert.Equal(orderedEntityList[i].Purpose, orderedModelList[i].DocumentPurpose);
            Assert.True(orderedModelList[i].VisibleToApplicant);
        }
    }
    
    public void ShouldMapPropertyProfile_ToSubmittedFlaPropertyDetail(SubmittedFlaPropertyDetail proposedRestockingDetail, PropertyProfile propertyProfile)
    {
        //arrange


        //act
        var result = ModelMapping.ToSubmittedFlaPropertyDetail(propertyProfile);

        //assert

        // Testing only configured PropertyProfileId mapping

        Assert.Equal(propertyProfile.Id, result.PropertyProfileId);
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
            GISData = JsonConvert.SerializeObject(_fixture.Create<Polygon>());
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