using AutoFixture;
using AutoFixture.Xunit2;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Extensions;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Forestry.Flo.Services.DataImport.Tests.Services;

public class ValidateImportFileSetsServiceRestockingValidationTests : ApplicationFileSetTestsBase
{
    [Theory, AutoData]
    public async Task WithValidFellingAndRestocking(Guid woodlandOwnerId)
    {
        var input = GenerateValidImportSets(restockingPerFelling: 3);

        var sut = CreateSut();

        var result = await sut.ValidateImportFileSetAsync(woodlandOwnerId, Properties, input, CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Theory, AutoData]
    public async Task WhenRestockingDetailsHaveSameRestockingTypeForSameFellingInFellingCompartment(Guid woodlandOwnerId)
    {
        var input = GenerateValidImportSets(applicationsCount: 1, fellingPerApplication: 1, restockingPerFelling: 3);

        var restockingType = TypeOfProposal.ReplantTheFelledArea;
        var area = input.ProposedFellingSourceRecords.First().AreaToBeFelled;

        input.ProposedRestockingSourceRecords!.ForEach(x =>
        {
            x.RestockingProposal = restockingType;
            x.Flov2CompartmentName = null;
            x.AreaToBeRestocked = area;
            x.SpeciesAndPercentages = $"{SpeciesCodes.RandomElement()},100";  // in case original was create designed open ground
            x.RestockingDensity = FixtureInstance.Create<double>();
        });

        var sut = CreateSut();

        var result = await sut.ValidateImportFileSetAsync(woodlandOwnerId, Properties, input, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Single(result.Error);

        Assert.Equal("There are repeated restocking operation types for the same felling operation within the Proposed Restocking records source", result.Error.Single().ErrorMessage);
    }

    [Theory, AutoData]
    public async Task WhenRestockingDetailsHaveSameRestockingTypeForSameFellingInAltCompartment(Guid woodlandOwnerId)
    {
        var input = GenerateValidImportSets(applicationsCount: 1, fellingPerApplication: 1, restockingPerFelling: 3);

        input.ProposedFellingSourceRecords.First().OperationType = FellingOperationType.ClearFelling; // ensure it is clearfelling so we can plant an alternative area
        var restockingType = TypeOfProposal.PlantAnAlternativeArea;
        var cpt = Properties.Single(x => x.Name == input.ApplicationSourceRecords.Single().Flov2PropertyName)
            .CompartmentIds.First(x => x.CompartmentName != input.ProposedFellingSourceRecords.First().Flov2CompartmentName);

        input.ProposedRestockingSourceRecords!.ForEach(x =>
        {
            x.RestockingProposal = restockingType;
            x.Flov2CompartmentName = cpt.CompartmentName;
            x.AreaToBeRestocked = 1;
            x.SpeciesAndPercentages = $"{SpeciesCodes.RandomElement()},100";  // in case original was create designed open ground
            x.RestockingDensity = FixtureInstance.Create<double>();
        });

        var sut = CreateSut();

        var result = await sut.ValidateImportFileSetAsync(woodlandOwnerId, Properties, input, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Single(result.Error);

        Assert.Equal("There are repeated restocking operation types for the same restocking compartment and felling operation within the Proposed Restocking records source", result.Error.Single().ErrorMessage);
    }

    [Theory, AutoData]
    public async Task WhenRestockingDetailsHasUnknownFellingId(Guid woodlandOwnerId)
    {
        var input = GenerateValidImportSets(applicationsCount: 1, fellingPerApplication: 1, restockingPerFelling: 3);

        var maxValidId = input.ProposedFellingSourceRecords!.Max(x => x.ProposedFellingId);
        input.ProposedRestockingSourceRecords!.First().ProposedFellingId = maxValidId + 1;

        var sut = CreateSut();

        var result = await sut.ValidateImportFileSetAsync(woodlandOwnerId, Properties, input, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Single(result.Error);

        Assert.Equal($"Proposed Felling Id {maxValidId + 1} was not found in the Proposed Felling source records for proposed restocking {input.ProposedRestockingSourceRecords.First().RestockingProposal}", result.Error.Single().ErrorMessage);
    }

    [Theory, AutoData]
    public async Task WhenRestockingOperationIsInvalidForFellingType(Guid woodlandOwnerId)
    {
        var input = GenerateValidImportSets(applicationsCount: 1, fellingPerApplication: 1, restockingPerFelling: 3);

        var restockingToAlter = input.ProposedRestockingSourceRecords!.First();
        var felling =
            input.ProposedFellingSourceRecords!.Single(x => x.ProposedFellingId == restockingToAlter.ProposedFellingId);
        var fellingCpt = Properties.Single(x => x.Name == input.ApplicationSourceRecords.Single().Flov2PropertyName)
            .CompartmentIds.Single(x => x.CompartmentName == felling.Flov2CompartmentName);
        var validTypes = felling.OperationType.AllowedRestockingForFellingType(false);
        var invalidTypes = Enum.GetValues<TypeOfProposal>()
            .Where(x => !validTypes.Contains(x) && x != TypeOfProposal.PlantAnAlternativeArea && x != TypeOfProposal.PlantAnAlternativeAreaWithIndividualTrees)
            .ToList();

        restockingToAlter.RestockingProposal = invalidTypes.RandomElement();
        restockingToAlter.AreaToBeRestocked = fellingCpt.Area!.Value; // ensure area is valid for the compartment
        if (restockingToAlter.RestockingProposal == TypeOfProposal.RestockWithIndividualTrees)
        {
            restockingToAlter.NumberOfTrees = 10;
        }
        else
        {
            restockingToAlter.RestockingDensity = FixtureInstance.Create<double>();
        }
        restockingToAlter.SpeciesAndPercentages = $"{SpeciesCodes.RandomElement()},100";  // in case original was create designed open ground

        var sut = CreateSut();

        var result = await sut.ValidateImportFileSetAsync(woodlandOwnerId, Properties, input, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Single(result.Error);

        Assert.Equal($"Restocking type is not valid for felling type {felling.OperationType} for proposed restocking {restockingToAlter.RestockingProposal} with proposed felling id {restockingToAlter.ProposedFellingId}", result.Error.Single().ErrorMessage);
    }

    [Theory, AutoData]
    public async Task WhenRestockingOperationIsPlantAlternateAreaAndNoCompartmentNameProvided(Guid woodlandOwnerId)
    {
        var input = GenerateValidImportSets(applicationsCount: 1, fellingPerApplication: 1, restockingPerFelling: 1);

        var restockingToAlter = input.ProposedRestockingSourceRecords!.Single();
        var fellingToAlter = input.ProposedFellingSourceRecords!.Single();

        //make it clearfelling, plant alt area but no cpt name
        fellingToAlter.OperationType = FellingOperationType.ClearFelling;
        restockingToAlter.RestockingProposal = TypeOfProposal.PlantAnAlternativeArea;
        restockingToAlter.Flov2CompartmentName = null;
        restockingToAlter.SpeciesAndPercentages = $"{SpeciesCodes.RandomElement()},100";  // in case original was create designed open ground
        restockingToAlter.RestockingDensity = FixtureInstance.Create<double>();

        var sut = CreateSut();

        var result = await sut.ValidateImportFileSetAsync(woodlandOwnerId, Properties, input, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Single(result.Error);

        Assert.Equal($"Flov2 Compartment Name must be provided when restocking an alternate area for proposed restocking {restockingToAlter.RestockingProposal} with proposed felling id {restockingToAlter.ProposedFellingId}", result.Error.Single().ErrorMessage);
    }

    [Theory, AutoData]
    public async Task WhenRestockingOperationIsPlantAlternateAreaIndividualTreesAndNoCompartmentNameProvided(Guid woodlandOwnerId)
    {
        var input = GenerateValidImportSets(applicationsCount: 1, fellingPerApplication: 1, restockingPerFelling: 1);

        var restockingToAlter = input.ProposedRestockingSourceRecords!.Single();
        var fellingToAlter = input.ProposedFellingSourceRecords!.Single();

        //make it fell trees, plant alt area with trees, but no cpt name
        fellingToAlter.OperationType = FellingOperationType.FellingIndividualTrees;
        restockingToAlter.RestockingProposal = TypeOfProposal.PlantAnAlternativeAreaWithIndividualTrees;
        restockingToAlter.NumberOfTrees = 10;  // in case was not required on original type
        restockingToAlter.Flov2CompartmentName = null;
        restockingToAlter.SpeciesAndPercentages = $"{SpeciesCodes.RandomElement()},100";  // in case original was create designed open ground

        var sut = CreateSut();

        var result = await sut.ValidateImportFileSetAsync(woodlandOwnerId, Properties, input, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Single(result.Error);

        Assert.Equal($"Flov2 Compartment Name must be provided when restocking an alternate area for proposed restocking {restockingToAlter.RestockingProposal} with proposed felling id {restockingToAlter.ProposedFellingId}", result.Error.Single().ErrorMessage);
    }

    [Theory, AutoData]
    public async Task WhenRestockingOperationIsPlantAlternateAreaAndCompartmentNameIsUnknown(Guid woodlandOwnerId)
    {
        var input = GenerateValidImportSets(applicationsCount: 1, fellingPerApplication: 1, restockingPerFelling: 1);

        var restockingToAlter = input.ProposedRestockingSourceRecords!.Single();
        var fellingToAlter = input.ProposedFellingSourceRecords!.Single();

        //make it clearfelling, plant alt area but no cpt name
        fellingToAlter.OperationType = FellingOperationType.ClearFelling;
        restockingToAlter.RestockingProposal = TypeOfProposal.PlantAnAlternativeArea;
        restockingToAlter.Flov2CompartmentName = FixtureInstance.Create<string>();
        restockingToAlter.SpeciesAndPercentages = $"{SpeciesCodes.RandomElement()},100";  // in case original was create designed open ground
        restockingToAlter.RestockingDensity = FixtureInstance.Create<double>();

        var sut = CreateSut();

        var result = await sut.ValidateImportFileSetAsync(woodlandOwnerId, Properties, input, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Single(result.Error);

        Assert.Equal($"Flov2 Compartment Name {restockingToAlter.Flov2CompartmentName} not found on linked property for proposed restocking {restockingToAlter.RestockingProposal} with proposed felling id {restockingToAlter.ProposedFellingId}", result.Error.Single().ErrorMessage);
    }

    [Theory, AutoData]
    public async Task WhenRestockingOperationIsPlantAlternateAreaIndividualTreesAndCompartmentNameIsUnknown(Guid woodlandOwnerId)
    {
        var input = GenerateValidImportSets(applicationsCount: 1, fellingPerApplication: 1, restockingPerFelling: 1);

        var restockingToAlter = input.ProposedRestockingSourceRecords!.Single();
        var fellingToAlter = input.ProposedFellingSourceRecords!.Single();

        //make it fell trees, plant alt area with trees, but no cpt name
        fellingToAlter.OperationType = FellingOperationType.FellingIndividualTrees;
        restockingToAlter.RestockingProposal = TypeOfProposal.PlantAnAlternativeAreaWithIndividualTrees;
        restockingToAlter.NumberOfTrees = 10;  // in case was not required on original type
        restockingToAlter.Flov2CompartmentName = FixtureInstance.Create<string>();
        restockingToAlter.SpeciesAndPercentages = $"{SpeciesCodes.RandomElement()},100";  // in case original was create designed open ground

        var sut = CreateSut();

        var result = await sut.ValidateImportFileSetAsync(woodlandOwnerId, Properties, input, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Single(result.Error);

        Assert.Equal($"Flov2 Compartment Name {restockingToAlter.Flov2CompartmentName} not found on linked property for proposed restocking {restockingToAlter.RestockingProposal} with proposed felling id {restockingToAlter.ProposedFellingId}", result.Error.Single().ErrorMessage);
    }

    [Theory, AutoData]
    public async Task WhenRestockingOperationIsPlantAlternateAreaAndCompartmentNameIsSameAsFellingCompartment(Guid woodlandOwnerId)
    {
        var input = GenerateValidImportSets(applicationsCount: 1, fellingPerApplication: 1, restockingPerFelling: 1);

        var restockingToAlter = input.ProposedRestockingSourceRecords!.Single();
        var fellingToAlter = input.ProposedFellingSourceRecords!.Single();

        var fellingCpt = Properties.Single(x => x.Name == input.ApplicationSourceRecords.Single().Flov2PropertyName)
            .CompartmentIds.Single(x => x.CompartmentName == fellingToAlter.Flov2CompartmentName);

        //make it clearfelling, plant alt area but no cpt name
        fellingToAlter.OperationType = FellingOperationType.ClearFelling;
        restockingToAlter.RestockingProposal = TypeOfProposal.PlantAnAlternativeArea;
        restockingToAlter.Flov2CompartmentName = fellingToAlter.Flov2CompartmentName;
        restockingToAlter.AreaToBeRestocked = fellingCpt.Area!.Value; // ensure area is valid for the compartment
        restockingToAlter.SpeciesAndPercentages = $"{SpeciesCodes.RandomElement()},100";  // in case original was create designed open ground
        restockingToAlter.RestockingDensity = FixtureInstance.Create<double>();

        var sut = CreateSut();

        var result = await sut.ValidateImportFileSetAsync(woodlandOwnerId, Properties, input, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Single(result.Error);

        Assert.Equal($"Restocking Compartment {restockingToAlter.Flov2CompartmentName} must not be the same as the felling compartment {fellingToAlter.Flov2CompartmentName} for proposed restocking {restockingToAlter.RestockingProposal} with proposed felling id {restockingToAlter.ProposedFellingId}", result.Error.Single().ErrorMessage);
    }

    [Theory, AutoData]
    public async Task WhenRestockingOperationIsPlantAlternateAreaIndividualTreesAndCompartmentNameIsSameAsFellingCompartment(Guid woodlandOwnerId)
    {
        var input = GenerateValidImportSets(applicationsCount: 1, fellingPerApplication: 1, restockingPerFelling: 1);

        var restockingToAlter = input.ProposedRestockingSourceRecords!.Single();
        var fellingToAlter = input.ProposedFellingSourceRecords!.Single();
        var fellingCpt = Properties.Single(x => x.Name == input.ApplicationSourceRecords.Single().Flov2PropertyName)
            .CompartmentIds.Single(x => x.CompartmentName == fellingToAlter.Flov2CompartmentName);

        //make it fell trees, plant alt area with trees, but no cpt name
        fellingToAlter.OperationType = FellingOperationType.FellingIndividualTrees;
        restockingToAlter.RestockingProposal = TypeOfProposal.PlantAnAlternativeAreaWithIndividualTrees;
        restockingToAlter.NumberOfTrees = 10;  // in case was not required on original type
        restockingToAlter.Flov2CompartmentName = fellingToAlter.Flov2CompartmentName;
        restockingToAlter.AreaToBeRestocked = fellingCpt.Area!.Value; // ensure area is valid for the compartment
        restockingToAlter.SpeciesAndPercentages = $"{SpeciesCodes.RandomElement()},100";  // in case original was create designed open ground

        var sut = CreateSut();

        var result = await sut.ValidateImportFileSetAsync(woodlandOwnerId, Properties, input, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Single(result.Error);

        Assert.Equal($"Restocking Compartment {restockingToAlter.Flov2CompartmentName} must not be the same as the felling compartment {fellingToAlter.Flov2CompartmentName} for proposed restocking {restockingToAlter.RestockingProposal} with proposed felling id {restockingToAlter.ProposedFellingId}", result.Error.Single().ErrorMessage);
    }

    [Theory, AutoData]
    public async Task WhenRestockingGreaterThanCompartmentSizeRestockingFellingCompartment(Guid woodlandOwnerId)
    {
        var input = GenerateValidImportSets(applicationsCount: 1, fellingPerApplication: 1, restockingPerFelling: 1);

        var restockingToAlter = input.ProposedRestockingSourceRecords!.Single();
        var fellingToAlter = input.ProposedFellingSourceRecords!.Single();

        var property = Properties.Single(x => x.Name.Equals(input.ApplicationSourceRecords!.Single().Flov2PropertyName,
            StringComparison.InvariantCultureIgnoreCase));
        var cpt = property.CompartmentIds.Single(x =>
            x.CompartmentName.Equals(fellingToAlter.Flov2CompartmentName, StringComparison.InvariantCultureIgnoreCase));

        //make it fell trees, plant alt area with trees, but no cpt name
        fellingToAlter.OperationType = FellingOperationType.FellingIndividualTrees;
        restockingToAlter.RestockingProposal = TypeOfProposal.RestockWithIndividualTrees;
        restockingToAlter.NumberOfTrees = 10;  // in case was not required on original type
        restockingToAlter.AreaToBeRestocked = cpt.Area!.Value + 1; // make it larger than the compartment area
        restockingToAlter.SpeciesAndPercentages = $"{SpeciesCodes.RandomElement()},100";  // in case original was create designed open ground

        var sut = CreateSut();

        var result = await sut.ValidateImportFileSetAsync(woodlandOwnerId, Properties, input, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Single(result.Error);

        Assert.Equal($"Restocking area must not be greater than the compartment area for proposed restocking {restockingToAlter.RestockingProposal} with proposed felling id {restockingToAlter.ProposedFellingId}", result.Error.Single().ErrorMessage);
    }

    [Theory, AutoData]
    public async Task WhenRestockingGreaterThanCompartmentSizeRestockingAlternateCompartment(Guid woodlandOwnerId)
    {
        var input = GenerateValidImportSets(applicationsCount: 1, fellingPerApplication: 1, restockingPerFelling: 1);

        var restockingToAlter = input.ProposedRestockingSourceRecords!.Single();
        var fellingToAlter = input.ProposedFellingSourceRecords!.Single();

        var property = Properties.Single(x => x.Name.Equals(input.ApplicationSourceRecords!.Single().Flov2PropertyName,
            StringComparison.InvariantCultureIgnoreCase));
        var cpt = property.CompartmentIds.First(x =>
            !x.CompartmentName.Equals(fellingToAlter.Flov2CompartmentName, StringComparison.InvariantCultureIgnoreCase));

        //make it fell trees, plant alt area with trees, but no cpt name
        fellingToAlter.OperationType = FellingOperationType.FellingIndividualTrees;
        restockingToAlter.RestockingProposal = TypeOfProposal.PlantAnAlternativeAreaWithIndividualTrees;
        restockingToAlter.Flov2CompartmentName = cpt.CompartmentName; 
        restockingToAlter.NumberOfTrees = 10;  // in case was not required on original type
        restockingToAlter.AreaToBeRestocked = cpt.Area!.Value + 1; // make it larger than the compartment area
        restockingToAlter.SpeciesAndPercentages = $"{SpeciesCodes.RandomElement()},100";  // in case original was create designed open ground

        var sut = CreateSut();

        var result = await sut.ValidateImportFileSetAsync(woodlandOwnerId, Properties, input, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Single(result.Error);

        Assert.Equal($"Restocking area must not be greater than the compartment area for proposed restocking {restockingToAlter.RestockingProposal} with proposed felling id {restockingToAlter.ProposedFellingId}", result.Error.Single().ErrorMessage);
    }

    [Theory, AutoData]
    public async Task WhenRestockingAreaIsNegative(Guid woodlandOwnerId)
    {
        var input = GenerateValidImportSets(applicationsCount: 1, fellingPerApplication: 1, restockingPerFelling: 1);

        var restockingToAlter = input.ProposedRestockingSourceRecords!.Single();
        restockingToAlter.AreaToBeRestocked = -5;

        var sut = CreateSut();

        var result = await sut.ValidateImportFileSetAsync(woodlandOwnerId, Properties, input, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Single(result.Error);

        Assert.Equal($"Area to be restocked must be greater than zero for proposed restocking {restockingToAlter.RestockingProposal} with proposed felling id {restockingToAlter.ProposedFellingId}", result.Error.Single().ErrorMessage);
    }

    [Theory, AutoData]
    public async Task WhenRestockingAreaIsZero(Guid woodlandOwnerId)
    {
        var input = GenerateValidImportSets(applicationsCount: 1, fellingPerApplication: 1, restockingPerFelling: 1);

        var restockingToAlter = input.ProposedRestockingSourceRecords!.Single();
        restockingToAlter.AreaToBeRestocked = 0;

        var sut = CreateSut();

        var result = await sut.ValidateImportFileSetAsync(woodlandOwnerId, Properties, input, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Single(result.Error);

        Assert.Equal($"Area to be restocked must be greater than zero for proposed restocking {restockingToAlter.RestockingProposal} with proposed felling id {restockingToAlter.ProposedFellingId}", result.Error.Single().ErrorMessage);
    }

    [Theory, AutoData]
    public async Task WhenRestockingOperationIsNotIndividualTreesAndRestockingDensityIsNull(Guid woodlandOwnerId)
    {
        var input = GenerateValidImportSets(applicationsCount: 1, fellingPerApplication: 1, restockingPerFelling: 1);

        var restockingToAlter = input.ProposedRestockingSourceRecords!.Single();
        var fellingToAlter = input.ProposedFellingSourceRecords!.Single();

        var fellingCpt = Properties.Single(x => x.Name == input.ApplicationSourceRecords.Single().Flov2PropertyName)
            .CompartmentIds.Single(x => x.CompartmentName == fellingToAlter.Flov2CompartmentName);

        //make it clearfelling, plant felled area, but no density
        fellingToAlter.OperationType = FellingOperationType.ClearFelling;
        restockingToAlter.RestockingProposal = TypeOfProposal.ReplantTheFelledArea;
        restockingToAlter.RestockingDensity = null;
        restockingToAlter.AreaToBeRestocked = fellingCpt.Area!.Value; // ensure area is valid for the compartment
        restockingToAlter.SpeciesAndPercentages = $"{SpeciesCodes.RandomElement()},100";  // in case original was create designed open ground

        var sut = CreateSut();

        var result = await sut.ValidateImportFileSetAsync(woodlandOwnerId, Properties, input, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Single(result.Error);

        Assert.Equal($"Restocking density must be provided and greater than zero for proposed restocking {restockingToAlter.RestockingProposal} with proposed felling id {restockingToAlter.ProposedFellingId}", result.Error.Single().ErrorMessage);
    }

    [Theory, AutoData]
    public async Task WhenRestockingOperationIsNotIndividualTreesAndRestockingDensityIsZero(Guid woodlandOwnerId)
    {
        var input = GenerateValidImportSets(applicationsCount: 1, fellingPerApplication: 1, restockingPerFelling: 1);

        var fellingCpt = Properties.Single(x => x.Name == input.ApplicationSourceRecords.Single().Flov2PropertyName)
            .CompartmentIds.Single(x => x.CompartmentName == input.ProposedFellingSourceRecords.Single().Flov2CompartmentName);

        var restockingToAlter = input.ProposedRestockingSourceRecords!.Single();
        var fellingToAlter = input.ProposedFellingSourceRecords!.Single();

        //make it clearfelling, plant felled area, but no density
        fellingToAlter.OperationType = FellingOperationType.ClearFelling;
        restockingToAlter.RestockingProposal = TypeOfProposal.ReplantTheFelledArea;
        restockingToAlter.RestockingDensity = 0;
        restockingToAlter.AreaToBeRestocked = fellingCpt.Area!.Value; // ensure area is valid for the compartment
        restockingToAlter.Flov2CompartmentName = null;
        restockingToAlter.SpeciesAndPercentages = $"{SpeciesCodes.RandomElement()},100";  // in case original was create designed open ground

        var sut = CreateSut();

        var result = await sut.ValidateImportFileSetAsync(woodlandOwnerId, Properties, input, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Single(result.Error);

        Assert.Equal($"Restocking density must be provided and greater than zero for proposed restocking {restockingToAlter.RestockingProposal} with proposed felling id {restockingToAlter.ProposedFellingId}", result.Error.Single().ErrorMessage);
    }

    [Theory, AutoData]
    public async Task WhenRestockingOperationIsIndividualTreesAndNumberOfTreesIsNull(Guid woodlandOwnerId)
    {
        var input = GenerateValidImportSets(applicationsCount: 1, fellingPerApplication: 1, restockingPerFelling: 1);

        var restockingToAlter = input.ProposedRestockingSourceRecords!.Single();
        var fellingToAlter = input.ProposedFellingSourceRecords!.Single();

        var fellingCpt = Properties.Single(x => x.Name == input.ApplicationSourceRecords.Single().Flov2PropertyName)
            .CompartmentIds.Single(x => x.CompartmentName == fellingToAlter.Flov2CompartmentName);

        //make it felling trees, restock with trees, but no number
        fellingToAlter.OperationType = FellingOperationType.FellingIndividualTrees;
        restockingToAlter.RestockingProposal = TypeOfProposal.RestockWithIndividualTrees;
        restockingToAlter.NumberOfTrees = null;
        restockingToAlter.SpeciesAndPercentages = $"{SpeciesCodes.RandomElement()},100";  // in case original was create designed open ground
        restockingToAlter.AreaToBeRestocked = fellingCpt.Area!.Value; // ensure area is valid for the compartment

        var sut = CreateSut();

        var result = await sut.ValidateImportFileSetAsync(woodlandOwnerId, Properties, input, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Single(result.Error);

        Assert.Equal($"Number of trees must be provided and greater than zero for proposed restocking {restockingToAlter.RestockingProposal} with proposed felling id {restockingToAlter.ProposedFellingId}", result.Error.Single().ErrorMessage);
    }

    [Theory, AutoData]
    public async Task WhenRestockingOperationIsIndividualTreesAndNumberOfTreesIsZero(Guid woodlandOwnerId)
    {
        var input = GenerateValidImportSets(applicationsCount: 1, fellingPerApplication: 1, restockingPerFelling: 1);

        var restockingToAlter = input.ProposedRestockingSourceRecords!.Single();
        var fellingToAlter = input.ProposedFellingSourceRecords!.Single();

        var fellingCpt = Properties.Single(x => x.Name == input.ApplicationSourceRecords.Single().Flov2PropertyName)
            .CompartmentIds.Single(x => x.CompartmentName == fellingToAlter.Flov2CompartmentName);

        //make it felling trees, restock with trees, but no number
        fellingToAlter.OperationType = FellingOperationType.FellingIndividualTrees;
        restockingToAlter.RestockingProposal = TypeOfProposal.RestockWithIndividualTrees;
        restockingToAlter.NumberOfTrees = 0;
        restockingToAlter.AreaToBeRestocked = fellingCpt.Area!.Value; // ensure area is valid for the compartment
        restockingToAlter.Flov2CompartmentName = null;
        restockingToAlter.SpeciesAndPercentages = $"{SpeciesCodes.RandomElement()},100";  // in case original was create designed open ground

        var sut = CreateSut();

        var result = await sut.ValidateImportFileSetAsync(woodlandOwnerId, Properties, input, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Single(result.Error);

        Assert.Equal($"Number of trees must be provided and greater than zero for proposed restocking {restockingToAlter.RestockingProposal} with proposed felling id {restockingToAlter.ProposedFellingId}", result.Error.Single().ErrorMessage);
    }

    [Theory, AutoData]
    public async Task WhenRestockingSpeciesIsEmpty(Guid woodlandOwnerId)
    {
        var input = GenerateValidImportSets(applicationsCount: 1, fellingPerApplication: 1, restockingPerFelling: 1);

        var restockingToAlter = input.ProposedRestockingSourceRecords!.Single();

        restockingToAlter.SpeciesAndPercentages = string.Empty;
        if (restockingToAlter.RestockingProposal == TypeOfProposal.CreateDesignedOpenGround)  // ok to be empty for designed open ground
        {
            restockingToAlter.RestockingProposal = TypeOfProposal.ReplantTheFelledArea;
            restockingToAlter.RestockingDensity = FixtureInstance.Create<double>();
        }

        var sut = CreateSut();

        var result = await sut.ValidateImportFileSetAsync(woodlandOwnerId, Properties, input, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Single(result.Error);

        Assert.Equal($"Species and percentages must be provided for proposed restocking {restockingToAlter.RestockingProposal} with proposed felling id {restockingToAlter.ProposedFellingId}", result.Error.Single().ErrorMessage);
    }

    [Theory, AutoData]
    public async Task WhenRestockingSpeciesIsNotCommaSeparatedValues(Guid woodlandOwnerId)
    {
        var input = GenerateValidImportSets(applicationsCount: 1, fellingPerApplication: 1, restockingPerFelling: 1);

        var restockingToAlter = input.ProposedRestockingSourceRecords!.Single();

        restockingToAlter.SpeciesAndPercentages = FixtureInstance.Create<string>();
        if (restockingToAlter.RestockingProposal == TypeOfProposal.CreateDesignedOpenGround)  // ignored for designed open ground
        {
            restockingToAlter.RestockingProposal = TypeOfProposal.ReplantTheFelledArea;
            restockingToAlter.RestockingDensity = FixtureInstance.Create<double>();
        }

        var sut = CreateSut();

        var result = await sut.ValidateImportFileSetAsync(woodlandOwnerId, Properties, input, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(2, result.Error.Count);

        Assert.Contains(result.Error, x => x.ErrorMessage == $"Species and percentages {restockingToAlter.SpeciesAndPercentages} contains invalid species codes for proposed restocking {restockingToAlter.RestockingProposal} with proposed felling id {restockingToAlter.ProposedFellingId}");
        Assert.Contains(result.Error, x => x.ErrorMessage == $"Species and percentages {restockingToAlter.SpeciesAndPercentages} contains invalid percentage or percentages don't total 100% for proposed restocking {restockingToAlter.RestockingProposal} with proposed felling id {restockingToAlter.ProposedFellingId}");
    }

    [Theory, AutoData]
    public async Task WhenRestockingSpeciesContainsUnknownSpecies(Guid woodlandOwnerId)
    {
        var input = GenerateValidImportSets(applicationsCount: 1, fellingPerApplication: 1, restockingPerFelling: 1);

        var restockingToAlter = input.ProposedRestockingSourceRecords!.Single();

        if (restockingToAlter.RestockingProposal == TypeOfProposal.CreateDesignedOpenGround)  // ignored for designed open ground
        {
            restockingToAlter.RestockingProposal = TypeOfProposal.ReplantTheFelledArea;
            restockingToAlter.RestockingDensity = FixtureInstance.Create<double>();
            restockingToAlter.SpeciesAndPercentages = $"{SpeciesCodes.RandomElement()},50,{SpeciesCodes.RandomElement()},50";  // in case original was create designed open ground
        }
        var species = restockingToAlter.SpeciesAndPercentages.Split(',').ToList();
        species[0] = FixtureInstance.Create<string>();
        restockingToAlter.SpeciesAndPercentages = string.Join(',', species);

        var sut = CreateSut();

        var result = await sut.ValidateImportFileSetAsync(woodlandOwnerId, Properties, input, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Single(result.Error);

        Assert.Equal($"Species and percentages {restockingToAlter.SpeciesAndPercentages} contains invalid species codes for proposed restocking {restockingToAlter.RestockingProposal} with proposed felling id {restockingToAlter.ProposedFellingId}", result.Error.Single().ErrorMessage);
    }

    [Theory, AutoData]
    public async Task WhenRestockingSpeciesContainsJustASingleSpeciesCode(Guid woodlandOwnerId)
    {
        var input = GenerateValidImportSets(applicationsCount: 1, fellingPerApplication: 1, restockingPerFelling: 1);

        var restockingToAlter = input.ProposedRestockingSourceRecords!.Single();

        if (restockingToAlter.RestockingProposal == TypeOfProposal.CreateDesignedOpenGround)  // ignored for designed open ground
        {
            restockingToAlter.RestockingProposal = TypeOfProposal.ReplantTheFelledArea;
            restockingToAlter.RestockingDensity = FixtureInstance.Create<double>();
        }

        restockingToAlter.SpeciesAndPercentages = SpeciesCodes.RandomElement();

        var sut = CreateSut();

        var result = await sut.ValidateImportFileSetAsync(woodlandOwnerId, Properties, input, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Single(result.Error);

        Assert.Equal($"Species and percentages {restockingToAlter.SpeciesAndPercentages} contains invalid percentage or percentages don't total 100% for proposed restocking {restockingToAlter.RestockingProposal} with proposed felling id {restockingToAlter.ProposedFellingId}", result.Error.Single().ErrorMessage);
    }

    [Theory, AutoData]
    public async Task WhenRestockingSpeciesContainsSpeciesCodesButNoPercentages(Guid woodlandOwnerId)
    {
        var input = GenerateValidImportSets(applicationsCount: 1, fellingPerApplication: 1, restockingPerFelling: 1);

        var restockingToAlter = input.ProposedRestockingSourceRecords!.Single();

        if (restockingToAlter.RestockingProposal == TypeOfProposal.CreateDesignedOpenGround)  // ignored for designed open ground
        {
            restockingToAlter.RestockingProposal = TypeOfProposal.ReplantTheFelledArea;
            restockingToAlter.RestockingDensity = FixtureInstance.Create<double>();
        }

        var species1 = SpeciesCodes.RandomElement();
        var species2 = SpeciesCodes.Where(x => x != species1).RandomElement();

        restockingToAlter.SpeciesAndPercentages = $"{species1},{species2}";

        var sut = CreateSut();

        var result = await sut.ValidateImportFileSetAsync(woodlandOwnerId, Properties, input, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Single(result.Error);

        Assert.Equal($"Species and percentages {restockingToAlter.SpeciesAndPercentages} contains invalid percentage or percentages don't total 100% for proposed restocking {restockingToAlter.RestockingProposal} with proposed felling id {restockingToAlter.ProposedFellingId}", result.Error.Single().ErrorMessage);
    }

    [Theory, AutoData]
    public async Task WhenRestockingSpeciesContainsRepeatedSpeciesCodes(Guid woodlandOwnerId)
    {
        var input = GenerateValidImportSets(applicationsCount: 1, fellingPerApplication: 1, restockingPerFelling: 1);

        var restockingToAlter = input.ProposedRestockingSourceRecords!.Single();

        if (restockingToAlter.RestockingProposal == TypeOfProposal.CreateDesignedOpenGround)  // ignored for designed open ground
        {
            restockingToAlter.RestockingProposal = TypeOfProposal.ReplantTheFelledArea;
            restockingToAlter.RestockingDensity = FixtureInstance.Create<double>();
        }

        var species1 = SpeciesCodes.RandomElement();
        var species2 = SpeciesCodes.Where(x => x != species1).RandomElement();

        restockingToAlter.SpeciesAndPercentages = $"{species1},33,{species2},33,{species1},34";

        var sut = CreateSut();

        var result = await sut.ValidateImportFileSetAsync(woodlandOwnerId, Properties, input, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Single(result.Error);

        Assert.Equal($"Species and percentages {restockingToAlter.SpeciesAndPercentages} contains repeated duplicate species codes for proposed restocking {restockingToAlter.RestockingProposal} with proposed felling id {restockingToAlter.ProposedFellingId}", result.Error.Single().ErrorMessage);
    }

    [Theory, AutoData]
    public async Task WhenRestockingSpeciesContainsInvalidPercentage(Guid woodlandOwnerId)
    {
        var input = GenerateValidImportSets(applicationsCount: 1, fellingPerApplication: 1, restockingPerFelling: 1);

        var restockingToAlter = input.ProposedRestockingSourceRecords!.Single();

        if (restockingToAlter.RestockingProposal == TypeOfProposal.CreateDesignedOpenGround)  // ignored for designed open ground
        {
            restockingToAlter.RestockingProposal = TypeOfProposal.ReplantTheFelledArea;
            restockingToAlter.RestockingDensity = FixtureInstance.Create<double>();
            restockingToAlter.SpeciesAndPercentages = $"{SpeciesCodes.RandomElement()},50,{SpeciesCodes.RandomElement()},50";  // in case original was create designed open ground
        }

        var species = restockingToAlter.SpeciesAndPercentages.Split(',').ToList();
        species[1] = FixtureInstance.Create<string>();
        restockingToAlter.SpeciesAndPercentages = string.Join(',', species);

        var sut = CreateSut();

        var result = await sut.ValidateImportFileSetAsync(woodlandOwnerId, Properties, input, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Single(result.Error);

        Assert.Equal($"Species and percentages {restockingToAlter.SpeciesAndPercentages} contains invalid percentage or percentages don't total 100% for proposed restocking {restockingToAlter.RestockingProposal} with proposed felling id {restockingToAlter.ProposedFellingId}", result.Error.Single().ErrorMessage);
    }

    [Theory, AutoData] public async Task WhenRestockingSpeciesPercentagesDoNotTotal100(Guid woodlandOwnerId)
    {
        var input = GenerateValidImportSets(applicationsCount: 1, fellingPerApplication: 1, restockingPerFelling: 1);

        var restockingToAlter = input.ProposedRestockingSourceRecords!.Single();

        if (restockingToAlter.RestockingProposal == TypeOfProposal.CreateDesignedOpenGround)  // ignored for designed open ground
        {
            restockingToAlter.RestockingProposal = TypeOfProposal.ReplantTheFelledArea;
            restockingToAlter.RestockingDensity = FixtureInstance.Create<double>();
            restockingToAlter.SpeciesAndPercentages = $"{SpeciesCodes.RandomElement()},50,{SpeciesCodes.RandomElement()},50";  // in case original was create designed open ground
        }

        var species = restockingToAlter.SpeciesAndPercentages.Split(',').ToList();
        species[1] = (int.Parse(species[1]) + 1).ToString();
        restockingToAlter.SpeciesAndPercentages = string.Join(',', species);

        var sut = CreateSut();

        var result = await sut.ValidateImportFileSetAsync(woodlandOwnerId, Properties, input, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Single(result.Error);

        Assert.Equal($"Species and percentages {restockingToAlter.SpeciesAndPercentages} contains invalid percentage or percentages don't total 100% for proposed restocking {restockingToAlter.RestockingProposal} with proposed felling id {restockingToAlter.ProposedFellingId}", result.Error.Single().ErrorMessage);
    }
}