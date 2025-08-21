using Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;

namespace Forestry.Flo.Internal.Web.Tests.Services.AdminOfficerReviewUseCaseTests;

public class FellingLicenceApplicationSummaryModelTests
{
    [Fact]
    public void IsCBWapplication_ShouldReturnTrue_WhenAllConditionsAreMet()
    {
        // Arrange
        var model = new FellingLicenceApplicationSummaryModel
        {
            DetailsList = new List<FellingAndRestockingDetail>
            {
                new FellingAndRestockingDetail
                {
                    FellingDetail = new ProposedFellingDetailModel
                    {
                        OperationType = FellingOperationType.FellingIndividualTrees,
                        Species = new Dictionary<string, FellingSpeciesModel>
                        {
                            { "CBW", new FellingSpeciesModel { Species = "CBW", SpeciesName = "Cricket Bat Willow" } }
                        },
                    },
                    RestockingDetail = new List<ProposedRestockingDetailModel>
                    {
                        new ProposedRestockingDetailModel
                        {
                            RestockingProposal = TypeOfProposal.RestockWithIndividualTrees,
                            Species = new Dictionary<string, RestockingSpeciesModel>
                            {
                                { "CBW", new RestockingSpeciesModel { Species = "CBW", SpeciesName = "Cricket Bat Willow" } }
                            }
                        }
                    }
                }
            }
        };

        // Act
        var result = model.IsCBWapplication;

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsCBWapplication_ShouldReturnFalse_WhenNotAllSpeciesAreCBW()
    {
        // Arrange
        var model = new FellingLicenceApplicationSummaryModel
        {
            DetailsList = new List<FellingAndRestockingDetail>
            {
                new FellingAndRestockingDetail
                {
                    FellingDetail = new ProposedFellingDetailModel
                    {
                        OperationType = FellingOperationType.FellingIndividualTrees,
                        Species = new Dictionary<string, FellingSpeciesModel>
                        {
                            { "CBW", new FellingSpeciesModel { Species = "CBW", SpeciesName = "Cricket Bat Willow" } },
                            { "OAK", new FellingSpeciesModel { Species = "OAK", SpeciesName = "Oak" } }
                        }
                    },
                    RestockingDetail = new List<ProposedRestockingDetailModel>
                    {
                        new ProposedRestockingDetailModel
                        {
                            RestockingProposal = TypeOfProposal.RestockWithIndividualTrees,
                            Species = new Dictionary<string, RestockingSpeciesModel>
                            {
                                { "CBW", new RestockingSpeciesModel { Species = "CBW", SpeciesName = "Cricket Bat Willow" } }
                            }
                        }
                    }
                }
            }
        };

        // Act
        var result = model.IsCBWapplication;

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsCBWapplication_ShouldReturnFalse_WhenNotAllFellingOperationsAreIndividualTrees()
    {
        // Arrange
        var model = new FellingLicenceApplicationSummaryModel
        {
            DetailsList = new List<FellingAndRestockingDetail>
            {
                new FellingAndRestockingDetail
                {
                    FellingDetail = new ProposedFellingDetailModel
                    {
                        OperationType = FellingOperationType.ClearFelling,
                        Species = new Dictionary<string, FellingSpeciesModel>
                        {
                            { "CBW", new FellingSpeciesModel { Species = "CBW", SpeciesName = "Cricket Bat Willow" } }
                        }
                    },
                    RestockingDetail = new List<ProposedRestockingDetailModel>
                    {
                        new ProposedRestockingDetailModel
                        {
                            RestockingProposal = TypeOfProposal.RestockWithIndividualTrees,
                            Species = new Dictionary<string, RestockingSpeciesModel>
                            {
                                { "CBW", new RestockingSpeciesModel { Species = "CBW", SpeciesName = "Cricket Bat Willow" } }
                            }
                        }
                    }
                }
            }
        };

        // Act
        var result = model.IsCBWapplication;

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsCBWapplication_ShouldReturnFalse_WhenNotAllRestockingProposalsAreIndividualTrees()
    {
        // Arrange
        var model = new FellingLicenceApplicationSummaryModel
        {
            DetailsList = new List<FellingAndRestockingDetail>
            {
                new FellingAndRestockingDetail
                {
                    FellingDetail = new ProposedFellingDetailModel
                    {
                        OperationType = FellingOperationType.FellingIndividualTrees,
                        Species = new Dictionary<string, FellingSpeciesModel>
                        {
                            { "CBW", new FellingSpeciesModel { Species = "CBW", SpeciesName = "Cricket Bat Willow" } }
                        }
                    },
                    RestockingDetail = new List<ProposedRestockingDetailModel>
                    {
                        new ProposedRestockingDetailModel
                        {
                            RestockingProposal = TypeOfProposal.ReplantTheFelledArea,
                            Species = new Dictionary<string, RestockingSpeciesModel>
                            {
                                { "CBW", new RestockingSpeciesModel { Species = "CBW", SpeciesName = "Cricket Bat Willow" } }
                            }
                        }
                    }
                }
            }
        };

        // Act
        var result = model.IsCBWapplication;

        // Assert
        Assert.False(result);
    }


    [Fact]
    public void IsCBWapplication_ShouldReturnFalse_WhenPlantAnAlternativeAreaWithIndividualTrees()
    {
        // Arrange
        var model = new FellingLicenceApplicationSummaryModel
        {
            DetailsList = new List<FellingAndRestockingDetail>
            {
                new FellingAndRestockingDetail
                {
                    FellingDetail = new ProposedFellingDetailModel
                    {
                        OperationType = FellingOperationType.FellingIndividualTrees,
                        Species = new Dictionary<string, FellingSpeciesModel>
                        {
                            { "CBW", new FellingSpeciesModel { Species = "CBW", SpeciesName = "Cricket Bat Willow" } }
                        }
                    },
                    RestockingDetail = new List<ProposedRestockingDetailModel>
                    {
                        new ProposedRestockingDetailModel
                        {
                            RestockingProposal = TypeOfProposal.PlantAnAlternativeAreaWithIndividualTrees,
                            Species = new Dictionary<string, RestockingSpeciesModel>
                            {
                                { "CBW", new RestockingSpeciesModel { Species = "CBW", SpeciesName = "Cricket Bat Willow" } }
                            }
                        }
                    }
                }
            }
        };

        // Act
        var result = model.IsCBWapplication;

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsCBWapplication_ShouldReturnFalse_WhenReplantingAnotherSpecies()
    {
        // Arrange
        var model = new FellingLicenceApplicationSummaryModel
        {
            DetailsList = new List<FellingAndRestockingDetail>
            {
                new FellingAndRestockingDetail
                {
                    FellingDetail = new ProposedFellingDetailModel
                    {
                        OperationType = FellingOperationType.FellingIndividualTrees,
                        Species = new Dictionary<string, FellingSpeciesModel>
                        {
                            { "CBW", new FellingSpeciesModel { Species = "CBW", SpeciesName = "Cricket Bat Willow" } }
                        }
                    },
                    RestockingDetail = new List<ProposedRestockingDetailModel>
                    {
                        new ProposedRestockingDetailModel
                        {
                            RestockingProposal = TypeOfProposal.ReplantTheFelledArea,
                            Species = new Dictionary<string, RestockingSpeciesModel>
                            {
                                { "CBW", new RestockingSpeciesModel { Species = "OAK", SpeciesName = "Oak" } }
                            }
                        }
                    }
                }
            }
        };

        // Act
        var result = model.IsCBWapplication;

        // Assert
        Assert.False(result);
    }

}
