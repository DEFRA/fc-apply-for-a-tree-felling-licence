using Forestry.Flo.External.Web.Models.FellingLicenceApplication;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;

namespace Forestry.Flo.External.Web.Tests.Model;

public class FellingLicenceApplicationModelTests
{
    [Fact]
    public void IsCBWapplication_ShouldReturnTrue_WhenAllConditionsAreMet()
    {
        // Arrange
        var model = new FellingLicenceApplicationModel
        {
            FellingAndRestockingDetails = new FellingAndRestockingDetails
            {
                DetailsList = new List<FellingAndRestockingDetail>
                {
                    new FellingAndRestockingDetail
                    {
                        FellingDetails = new List<ProposedFellingDetailModel>
                        {
                            new ProposedFellingDetailModel
                            {
                                Species = new Dictionary<string, SpeciesModel> { { "CBW", new SpeciesModel()  } },
                                OperationType = FellingOperationType.FellingIndividualTrees,
                                ProposedRestockingDetails = new List<ProposedRestockingDetailModel>
                                {
                                    new ProposedRestockingDetailModel
                                    {
                                        RestockingProposal = TypeOfProposal.RestockWithIndividualTrees
                                    }
                                }
                            }
                        },
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
        var model = new FellingLicenceApplicationModel
        {
            FellingAndRestockingDetails = new FellingAndRestockingDetails
            {
                DetailsList = new List<FellingAndRestockingDetail>
                {
                    new FellingAndRestockingDetail
                    {
                        FellingDetails = new List<ProposedFellingDetailModel>
                        {
                            new ProposedFellingDetailModel
                            {
                                Species = new Dictionary<string, SpeciesModel> { { "NonCBW", new SpeciesModel()  } },
                                OperationType = FellingOperationType.FellingIndividualTrees,
                                ProposedRestockingDetails = new List<ProposedRestockingDetailModel>
                                {
                                    new ProposedRestockingDetailModel
                                    {
                                        RestockingProposal = TypeOfProposal.RestockWithIndividualTrees
                                    }
                                }
                            }
                        },
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
    public void IsCBWapplication_ShouldReturnFalse_WhenNotAllOperationTypesAreFellingIndividualTrees()
    {
        // Arrange
        var model = new FellingLicenceApplicationModel
        {
            FellingAndRestockingDetails = new FellingAndRestockingDetails
            {
                DetailsList = new List<FellingAndRestockingDetail>
                {
                    new FellingAndRestockingDetail
                    {
                        FellingDetails = new List<ProposedFellingDetailModel>
                        {
                            new ProposedFellingDetailModel
                            {
                                Species = new Dictionary<string, SpeciesModel> { { "CBW", new SpeciesModel()  } },
                                OperationType = FellingOperationType.Thinning,
                                ProposedRestockingDetails = new List<ProposedRestockingDetailModel>
                                {
                                    new ProposedRestockingDetailModel
                                    {
                                        RestockingProposal = TypeOfProposal.RestockWithIndividualTrees
                                    }
                                }
                            }
                        },
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
        var model = new FellingLicenceApplicationModel
        {
            FellingAndRestockingDetails = new FellingAndRestockingDetails
            {
                DetailsList = new List<FellingAndRestockingDetail>
                {
                    new FellingAndRestockingDetail
                    {
                        FellingDetails = new List<ProposedFellingDetailModel>
                        {
                            new ProposedFellingDetailModel
                            {
                                Species = new Dictionary<string, SpeciesModel> { { "CBW", new SpeciesModel()  } },
                                OperationType = FellingOperationType.FellingIndividualTrees,
                                ProposedRestockingDetails = new List<ProposedRestockingDetailModel>
                                {
                                    new ProposedRestockingDetailModel
                                    {
                                        RestockingProposal = TypeOfProposal.NaturalColonisation
                                    }
                                }
                            }
                        },
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
        var model = new FellingLicenceApplicationModel
        {
            FellingAndRestockingDetails = new FellingAndRestockingDetails
            {
                DetailsList = new List<FellingAndRestockingDetail>
                {
                    new FellingAndRestockingDetail
                    {
                        FellingDetails = new List<ProposedFellingDetailModel>
                        {
                            new ProposedFellingDetailModel
                            {
                                Species = new Dictionary<string, SpeciesModel> { { "CBW", new SpeciesModel()  } },
                                OperationType = FellingOperationType.FellingIndividualTrees,
                                ProposedRestockingDetails = new List<ProposedRestockingDetailModel>
                                {
                                    new ProposedRestockingDetailModel
                                    {
                                        RestockingProposal = TypeOfProposal.PlantAnAlternativeAreaWithIndividualTrees
                                    }
                                }
                            }
                        },
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