using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Extensions;
using System;
using System.Linq;
using Xunit;

namespace Forestry.Flo.Services.FellingLicenceApplications.Tests.Extensions
{
    public class EntityExtensionsTests
    {
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
    }
}
