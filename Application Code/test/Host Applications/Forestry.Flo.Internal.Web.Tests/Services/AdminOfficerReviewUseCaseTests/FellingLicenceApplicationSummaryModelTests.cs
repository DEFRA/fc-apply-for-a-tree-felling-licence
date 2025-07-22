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
                            { "CBW", new FellingSpeciesModel { Species = "CBW", SpeciesName = "Common Beechwood" } }
                        }
                    },
                    RestockingDetail = new ProposedRestockingDetailModel
                    {
                        RestockingProposal = TypeOfProposal.RestockWithIndividualTrees
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
                            { "CBW", new FellingSpeciesModel { Species = "CBW", SpeciesName = "Common Beechwood" } },
                            { "OAK", new FellingSpeciesModel { Species = "OAK", SpeciesName = "Oak" } }
                        }
                    },
                    RestockingDetail = new ProposedRestockingDetailModel
                    {
                        RestockingProposal = TypeOfProposal.RestockWithIndividualTrees
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
                            { "CBW", new FellingSpeciesModel { Species = "CBW", SpeciesName = "Common Beechwood" } }
                        }
                    },
                    RestockingDetail = new ProposedRestockingDetailModel
                    {
                        RestockingProposal = TypeOfProposal.RestockWithIndividualTrees
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
                            { "CBW", new FellingSpeciesModel { Species = "CBW", SpeciesName = "Common Beechwood" } }
                        }
                    },
                    RestockingDetail = new ProposedRestockingDetailModel
                    {
                        RestockingProposal = TypeOfProposal.ReplantTheFelledArea
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
                            { "CBW", new FellingSpeciesModel { Species = "CBW", SpeciesName = "Common Beechwood" } }
                        }
                    },
                    RestockingDetail = new ProposedRestockingDetailModel
                    {
                        RestockingProposal = TypeOfProposal.PlantAnAlternativeAreaWithIndividualTrees
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
