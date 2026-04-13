using AutoFixture;
using AutoFixture.AutoMoq;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Extensions;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Forestry.Flo.Services.FellingLicenceApplications.Tests.Extensions
{
    public class EntityExtensionsTests
    {
        private IFixture _fixture;

        public EntityExtensionsTests()
        {
            _fixture = new Fixture().Customize(new CompositeCustomization(
                new AutoMoqCustomization(),
                new SupportMutableValueTypesCustomization()));

            _fixture.Behaviors.Add(new OmitOnRecursionBehavior());

            _fixture.Customize<DateOnly>(composer => composer.FromFactory<DateTime>(DateOnly.FromDateTime));

        }

        [Theory]
        [InlineData(FellingOperationType.ClearFelling, new[] { TypeOfProposal.CreateDesignedOpenGround, TypeOfProposal.DoNotIntendToRestock, TypeOfProposal.ReplantTheFelledArea, TypeOfProposal.RestockByNaturalRegeneration, TypeOfProposal.RestockWithCoppiceRegrowth, TypeOfProposal.PlantAnAlternativeArea, TypeOfProposal.NaturalColonisation })]
        [InlineData(FellingOperationType.FellingOfCoppice, new[] { TypeOfProposal.CreateDesignedOpenGround, TypeOfProposal.DoNotIntendToRestock, TypeOfProposal.RestockWithCoppiceRegrowth })]
        [InlineData(FellingOperationType.FellingIndividualTrees, new[] { TypeOfProposal.CreateDesignedOpenGround, TypeOfProposal.DoNotIntendToRestock, TypeOfProposal.RestockByNaturalRegeneration, TypeOfProposal.RestockWithCoppiceRegrowth, TypeOfProposal.RestockWithIndividualTrees, TypeOfProposal.PlantAnAlternativeAreaWithIndividualTrees })]
        [InlineData(FellingOperationType.RegenerationFelling, new[] { TypeOfProposal.CreateDesignedOpenGround, TypeOfProposal.DoNotIntendToRestock, TypeOfProposal.RestockWithCoppiceRegrowth, TypeOfProposal.ReplantTheFelledArea, TypeOfProposal.RestockByNaturalRegeneration })]
        [InlineData(FellingOperationType.Thinning, new TypeOfProposal[0])]
        public void ShouldValidateRestockingOptionBasedOnFellingType(
        FellingOperationType fellingType,
        TypeOfProposal[] validRestockingOptions)
        {
            var allowedTypes = fellingType.AllowedRestockingForFellingType(false);

            foreach (var restockingType in Enum.GetValues<TypeOfProposal>())
            {
                if (validRestockingOptions.Contains(restockingType))
                {
                    Assert.Contains(restockingType, allowedTypes);
                }
                else
                {
                    Assert.DoesNotContain(restockingType, allowedTypes);
                }
            }
        }

        [Theory, CombinatorialData]
        public void CorrectlyReportsIfOperationTypeSupportsAlternativeCompartmentRestocking(FellingOperationType fellingOperationType)
        {
            var shouldSupport = fellingOperationType == FellingOperationType.ClearFelling
                || fellingOperationType == FellingOperationType.FellingIndividualTrees;

            var result = fellingOperationType.SupportsAlternativeCompartmentRestocking();

            Assert.Equal(shouldSupport, result);
        }

        [Theory]
        [InlineData(FellingOperationType.ClearFelling, TypeOfProposal.ReplantTheFelledArea, 10, 50)]  // numbers don't matter for this combo
        [InlineData(FellingOperationType.FellingIndividualTrees, TypeOfProposal.RestockWithIndividualTrees, 5, 5)]
        public void IsCBWApplication_ShouldReturnTrue_WhenAllConditionsAreMet_Proposed(FellingOperationType fellingType, TypeOfProposal restockingType, int? numberOfTreesFelled, int? numberOfTreesRestocked)
        {
            var application = _fixture.Create<FellingLicenceApplication>();

            application.WoodlandOfficerReview = null;

            application.LinkedPropertyProfile.ProposedFellingDetails = 
            [
                new ProposedFellingDetail
                {
                    FellingSpecies = new List<FellingSpecies>
                    { 
                        new() { Species = "CBW" }
                    },
                    OperationType = fellingType,
                    NumberOfTrees = numberOfTreesFelled,
                    ProposedRestockingDetails = new List<ProposedRestockingDetail>
                    {
                        new ProposedRestockingDetail
                        {
                            RestockingSpecies = new List<RestockingSpecies>()
                            {
                                new RestockingSpecies
                                {
                                    Species = "CBW"
                                }
                            },
                            RestockingProposal = restockingType,
                            NumberOfTrees = numberOfTreesRestocked,
                        }
                    }
                }
            ];

            // Act
            var result = application.IsCBWApplication();

            // Assert
            Assert.True(result);
        }


        [Theory]
        [InlineData(FellingOperationType.ClearFelling, TypeOfProposal.ReplantTheFelledArea, 10, 50)]  // numbers don't matter for this combo
        [InlineData(FellingOperationType.FellingIndividualTrees, TypeOfProposal.RestockWithIndividualTrees, 5, 5)]
        public void IsCBWApplication_ShouldReturnTrue_WhenAllConditionsAreMet_Submitted(FellingOperationType fellingType, TypeOfProposal restockingType, int? numberOfTreesFelled, int? numberOfTreesRestocked)
        {
            var application = _fixture.Create<FellingLicenceApplication>();

            application.WoodlandOfficerReview.ConfirmedFellingAndRestockingComplete = true;

            application.SubmittedFlaPropertyDetail.SubmittedFlaPropertyCompartments =
            [
                new SubmittedFlaPropertyCompartment
                {
                    ConfirmedFellingDetails = new List<ConfirmedFellingDetail>
                    {
                        new ConfirmedFellingDetail
                        {
                            ConfirmedFellingSpecies= new List<ConfirmedFellingSpecies>
                            {
                                new() { Species = "CBW" }
                            },
                            OperationType = fellingType,
                            NumberOfTrees = numberOfTreesFelled,
                            ConfirmedRestockingDetails = new List<ConfirmedRestockingDetail>
                            {
                                new ConfirmedRestockingDetail
                                {
                                    ConfirmedRestockingSpecies = new List<ConfirmedRestockingSpecies>
                                    {
                                        new ConfirmedRestockingSpecies
                                        {
                                            Species = "CBW"
                                        }
                                    },
                                    RestockingProposal = restockingType,
                                    NumberOfTrees = numberOfTreesRestocked,
                                }
                            }
                        }
                    }

                }
            ];

            // Act
            var result = application.IsCBWApplication();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsCBWApplication_ShouldReturnFalse_WhenNotAllSpeciesAreCBW_Proposed()
        {
            // Arrange
            var application = _fixture.Create<FellingLicenceApplication>();

            application.WoodlandOfficerReview = null;

            application.LinkedPropertyProfile.ProposedFellingDetails =
            [
                new ProposedFellingDetail
                {
                    FellingSpecies = new List<FellingSpecies>
                    {
                        new() { Species = "CBW" },
                        new() { Species = "OAK" }
                    },
                    OperationType = FellingOperationType.FellingIndividualTrees,
                    NumberOfTrees = 10,
                    ProposedRestockingDetails = new List<ProposedRestockingDetail>
                    {
                        new ProposedRestockingDetail
                        {
                            RestockingSpecies = new List<RestockingSpecies>()
                            {
                                new RestockingSpecies
                                {
                                    Species = "CBW"
                                }
                            },
                            RestockingProposal = TypeOfProposal.RestockWithIndividualTrees,
                            NumberOfTrees = 10,
                        }
                    }
                }
            ];

            // Act
            var result = application.IsCBWApplication();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsCBWApplication_ShouldReturnFalse_WhenNotAllSpeciesAreCBW_Submitted()
        {
            // Arrange
            var application = _fixture.Create<FellingLicenceApplication>();

            application.WoodlandOfficerReview.ConfirmedFellingAndRestockingComplete = true;

            application.SubmittedFlaPropertyDetail.SubmittedFlaPropertyCompartments =
            [
                new SubmittedFlaPropertyCompartment
                {
                    ConfirmedFellingDetails = new List<ConfirmedFellingDetail>
                    {
                        new ConfirmedFellingDetail
                        {
                            ConfirmedFellingSpecies= new List<ConfirmedFellingSpecies>
                            {
                                new() { Species = "CBW" },
                                new() { Species = "OAK" }
                            },
                            OperationType = FellingOperationType.FellingIndividualTrees,
                            NumberOfTrees = 10,
                            ConfirmedRestockingDetails = new List<ConfirmedRestockingDetail>
                            {
                                new ConfirmedRestockingDetail
                                {
                                    ConfirmedRestockingSpecies = new List<ConfirmedRestockingSpecies>
                                    {
                                        new ConfirmedRestockingSpecies
                                        {
                                            Species = "CBW"
                                        }
                                    },
                                    RestockingProposal = TypeOfProposal.RestockWithIndividualTrees,
                                    NumberOfTrees = 10,
                                }
                            }
                        }
                    }

                }
            ];

            // Act
            var result = application.IsCBWApplication();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsCBWApplication_ShouldReturnFalse_WhenNumberOfTreesDoesNotMatch_Proposed()
        {
            // Arrange
            var application = _fixture.Create<FellingLicenceApplication>();

            application.WoodlandOfficerReview = null;

            application.LinkedPropertyProfile.ProposedFellingDetails =
            [
                new ProposedFellingDetail
                {
                    FellingSpecies = new List<FellingSpecies>
                    {
                        new() { Species = "CBW" },
                    },
                    OperationType = FellingOperationType.FellingIndividualTrees,
                    NumberOfTrees = 20,
                    ProposedRestockingDetails = new List<ProposedRestockingDetail>
                    {
                        new ProposedRestockingDetail
                        {
                            RestockingSpecies = new List<RestockingSpecies>()
                            {
                                new RestockingSpecies
                                {
                                    Species = "CBW"
                                }
                            },
                            RestockingProposal = TypeOfProposal.RestockWithIndividualTrees,
                            NumberOfTrees = 10,
                        }
                    }
                }
            ];

            // Act
            var result = application.IsCBWApplication();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsCBWApplication_ShouldReturnFalse_WhenNumberOfTreesDoesNotMatch_Submitted()
        {
            // Arrange
            var application = _fixture.Create<FellingLicenceApplication>();

            application.WoodlandOfficerReview.ConfirmedFellingAndRestockingComplete = true;

            application.SubmittedFlaPropertyDetail.SubmittedFlaPropertyCompartments =
            [
                new SubmittedFlaPropertyCompartment
                {
                    ConfirmedFellingDetails = new List<ConfirmedFellingDetail>
                    {
                        new ConfirmedFellingDetail
                        {
                            ConfirmedFellingSpecies= new List<ConfirmedFellingSpecies>
                            {
                                new() { Species = "CBW" },
                            },
                            OperationType = FellingOperationType.FellingIndividualTrees,
                            NumberOfTrees = 20,
                            ConfirmedRestockingDetails = new List<ConfirmedRestockingDetail>
                            {
                                new ConfirmedRestockingDetail
                                {
                                    ConfirmedRestockingSpecies = new List<ConfirmedRestockingSpecies>
                                    {
                                        new ConfirmedRestockingSpecies
                                        {
                                            Species = "CBW"
                                        }
                                    },
                                    RestockingProposal = TypeOfProposal.RestockWithIndividualTrees,
                                    NumberOfTrees = 10,
                                }
                            }
                        }
                    }

                }
            ];

            // Act
            var result = application.IsCBWApplication();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsCBWApplication_ShouldReturnFalse_WhenReplantingAnotherSpecies_Proposed()
        {
            // Arrange
            var application = _fixture.Create<FellingLicenceApplication>();

            application.WoodlandOfficerReview = null;

            application.LinkedPropertyProfile.ProposedFellingDetails =
            [
                new ProposedFellingDetail
                {
                    FellingSpecies = new List<FellingSpecies>
                    {
                        new() { Species = "CBW" },
                    },
                    OperationType = FellingOperationType.FellingIndividualTrees,
                    NumberOfTrees = 10,
                    ProposedRestockingDetails = new List<ProposedRestockingDetail>
                    {
                        new ProposedRestockingDetail
                        {
                            RestockingSpecies = new List<RestockingSpecies>()
                            {
                                new RestockingSpecies
                                {
                                    Species = "CBW"
                                },
                                new RestockingSpecies
                                {
                                    Species = "OAK"
                                }
                            },
                            RestockingProposal = TypeOfProposal.RestockWithIndividualTrees,
                            NumberOfTrees = 10,
                        }
                    }
                }
            ];

            // Act
            var result = application.IsCBWApplication();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsCBWApplication_ShouldReturnFalse_WhenReplantingAnotherSpecies_Submitted()
        {
            // Arrange
            var application = _fixture.Create<FellingLicenceApplication>();

            application.WoodlandOfficerReview.ConfirmedFellingAndRestockingComplete = true;

            application.SubmittedFlaPropertyDetail.SubmittedFlaPropertyCompartments =
            [
                new SubmittedFlaPropertyCompartment
                {
                    ConfirmedFellingDetails = new List<ConfirmedFellingDetail>
                    {
                        new ConfirmedFellingDetail
                        {
                            ConfirmedFellingSpecies= new List<ConfirmedFellingSpecies>
                            {
                                new() { Species = "CBW" },
                            },
                            OperationType = FellingOperationType.FellingIndividualTrees,
                            NumberOfTrees = 10,
                            ConfirmedRestockingDetails = new List<ConfirmedRestockingDetail>
                            {
                                new ConfirmedRestockingDetail
                                {
                                    ConfirmedRestockingSpecies = new List<ConfirmedRestockingSpecies>
                                    {
                                        new ConfirmedRestockingSpecies
                                        {
                                            Species = "CBW"
                                        },
                                        new ConfirmedRestockingSpecies
                                        {
                                            Species = "OAK"
                                        }
                                    },
                                    RestockingProposal = TypeOfProposal.RestockWithIndividualTrees,
                                    NumberOfTrees = 10,
                                }
                            }
                        }
                    }

                }
            ];

            // Act
            var result = application.IsCBWApplication();

            // Assert
            Assert.False(result);
        }

        [Theory]
        [InlineData(FellingOperationType.FellingOfCoppice)]
        [InlineData(FellingOperationType.RegenerationFelling)]
        [InlineData(FellingOperationType.Thinning)]
        public void IsCBWApplication_ShouldReturnFalse_WhenNotAllFellingOperationTypesAreValid_Proposed(FellingOperationType fellingOperation)
        {
            // Arrange
            var application = _fixture.Create<FellingLicenceApplication>();

            application.WoodlandOfficerReview = null;

            application.LinkedPropertyProfile.ProposedFellingDetails =
            [
                new ProposedFellingDetail
                {
                    FellingSpecies = new List<FellingSpecies>
                    {
                        new() { Species = "CBW" },
                    },
                    OperationType = fellingOperation,
                    NumberOfTrees = 10,
                    ProposedRestockingDetails = new List<ProposedRestockingDetail>
                    {
                        new ProposedRestockingDetail
                        {
                            RestockingSpecies = new List<RestockingSpecies>()
                            {
                                new RestockingSpecies
                                {
                                    Species = "CBW"
                                }
                            },
                            RestockingProposal = TypeOfProposal.RestockWithIndividualTrees,
                            NumberOfTrees = 10,
                        }
                    }
                }
            ];

            // Act
            var result = application.IsCBWApplication();

            // Assert
            Assert.False(result);
        }

        [Theory]
        [InlineData(FellingOperationType.FellingOfCoppice)]
        [InlineData(FellingOperationType.RegenerationFelling)]
        [InlineData(FellingOperationType.Thinning)]
        public void IsCBWApplication_ShouldReturnFalse_WhenNotAllFellingOperationTypesAreValid_Submitted(FellingOperationType fellingOperation)
        {
            // Arrange
            var application = _fixture.Create<FellingLicenceApplication>();

            application.WoodlandOfficerReview.ConfirmedFellingAndRestockingComplete = true;

            application.SubmittedFlaPropertyDetail.SubmittedFlaPropertyCompartments =
            [
                new SubmittedFlaPropertyCompartment
                {
                    ConfirmedFellingDetails = new List<ConfirmedFellingDetail>
                    {
                        new ConfirmedFellingDetail
                        {
                            ConfirmedFellingSpecies= new List<ConfirmedFellingSpecies>
                            {
                                new() { Species = "CBW" },
                            },
                            OperationType = fellingOperation,
                            NumberOfTrees = 10,
                            ConfirmedRestockingDetails = new List<ConfirmedRestockingDetail>
                            {
                                new ConfirmedRestockingDetail
                                {
                                    ConfirmedRestockingSpecies = new List<ConfirmedRestockingSpecies>
                                    {
                                        new ConfirmedRestockingSpecies
                                        {
                                            Species = "CBW"
                                        }
                                    },
                                    RestockingProposal = TypeOfProposal.RestockWithIndividualTrees,
                                    NumberOfTrees = 10,
                                }
                            }
                        }
                    }

                }
            ];

            // Act
            var result = application.IsCBWApplication();

            // Assert
            Assert.False(result);
        }

        [Theory]
        [InlineData(TypeOfProposal.CreateDesignedOpenGround)]
        [InlineData(TypeOfProposal.DoNotIntendToRestock)]
        [InlineData(TypeOfProposal.PlantAnAlternativeArea)]
        [InlineData(TypeOfProposal.NaturalColonisation)]
        [InlineData(TypeOfProposal.PlantAnAlternativeAreaWithIndividualTrees)]
        [InlineData(TypeOfProposal.RestockByNaturalRegeneration)]
        [InlineData(TypeOfProposal.RestockWithCoppiceRegrowth)]
        public void IsCBWApplication_ShouldReturnFalse_WhenNotAllRestockingProposalsAreValidTypes_Proposed(TypeOfProposal restockingType)
        {
            // Arrange
            var application = _fixture.Create<FellingLicenceApplication>();

            application.WoodlandOfficerReview = null;

            application.LinkedPropertyProfile.ProposedFellingDetails =
            [
                new ProposedFellingDetail
                {
                    FellingSpecies = new List<FellingSpecies>
                    {
                        new() { Species = "CBW" },
                    },
                    OperationType = FellingOperationType.FellingIndividualTrees,
                    NumberOfTrees = 10,
                    ProposedRestockingDetails = new List<ProposedRestockingDetail>
                    {
                        new ProposedRestockingDetail
                        {
                            RestockingSpecies = new List<RestockingSpecies>()
                            {
                                new RestockingSpecies
                                {
                                    Species = "CBW"
                                }
                            },
                            RestockingProposal = restockingType,
                            NumberOfTrees = 10,
                        }
                    }
                }
            ];

            // Act
            var result = application.IsCBWApplication();

            // Assert
            Assert.False(result);
        }

        [Theory]
        [InlineData(TypeOfProposal.CreateDesignedOpenGround)]
        [InlineData(TypeOfProposal.DoNotIntendToRestock)]
        [InlineData(TypeOfProposal.PlantAnAlternativeArea)]
        [InlineData(TypeOfProposal.NaturalColonisation)]
        [InlineData(TypeOfProposal.PlantAnAlternativeAreaWithIndividualTrees)]
        [InlineData(TypeOfProposal.RestockByNaturalRegeneration)]
        [InlineData(TypeOfProposal.RestockWithCoppiceRegrowth)]
        public void IsCBWApplication_ShouldReturnFalse_WhenNotAllRestockingProposalsAreValidTypes_Submitted(TypeOfProposal restockingType)
        {
            // Arrange
            var application = _fixture.Create<FellingLicenceApplication>();

            application.WoodlandOfficerReview.ConfirmedFellingAndRestockingComplete = true;

            application.SubmittedFlaPropertyDetail.SubmittedFlaPropertyCompartments =
            [
                new SubmittedFlaPropertyCompartment
                {
                    ConfirmedFellingDetails = new List<ConfirmedFellingDetail>
                    {
                        new ConfirmedFellingDetail
                        {
                            ConfirmedFellingSpecies= new List<ConfirmedFellingSpecies>
                            {
                                new() { Species = "CBW" },
                            },
                            OperationType = FellingOperationType.FellingIndividualTrees,
                            NumberOfTrees = 10,
                            ConfirmedRestockingDetails = new List<ConfirmedRestockingDetail>
                            {
                                new ConfirmedRestockingDetail
                                {
                                    ConfirmedRestockingSpecies = new List<ConfirmedRestockingSpecies>
                                    {
                                        new ConfirmedRestockingSpecies
                                        {
                                            Species = "CBW"
                                        }
                                    },
                                    RestockingProposal = restockingType,
                                    NumberOfTrees = 10,
                                }
                            }
                        }
                    }

                }
            ];

            // Act
            var result = application.IsCBWApplication();

            // Assert
            Assert.False(result);
        }
    }
}
