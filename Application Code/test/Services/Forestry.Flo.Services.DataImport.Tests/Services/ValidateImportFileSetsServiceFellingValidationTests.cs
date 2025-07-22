using AutoFixture;
using AutoFixture.Xunit2;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace Forestry.Flo.Services.DataImport.Tests.Services;

public class ValidateImportFileSetsServiceFellingValidationTests : ApplicationFileSetTestsBase
{
    [Theory, AutoData]
    public async Task WithValidFelling(Guid woodlandOwnerId)
    {
        var input = GenerateValidImportSets();

        var sut = CreateSut();

        var result = await sut.ValidateImportFileSetAsync(woodlandOwnerId, Properties, input, CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Theory, AutoData]
    public async Task WhenFellingDetailsHaveSameId(Guid woodlandOwnerId)
    {
        var input = GenerateValidImportSets(applicationsCount: 1, restockingPerFelling: 0);

        input.ProposedFellingSourceRecords.ForEach(x => x.ProposedFellingId = 1);

        var sut = CreateSut();

        var result = await sut.ValidateImportFileSetAsync(woodlandOwnerId, Properties, input, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Single(result.Error);

        Assert.Equal("There are repeated id values within the Proposed Felling records source", result.Error.Single().ErrorMessage);
    }

    [Theory, AutoData]
    public async Task WhenFellingDetailsHaveSameFellingTypeInSameCompartment(Guid woodlandOwnerId)
    {
        var input = GenerateValidImportSets(applicationsCount: 1, restockingPerFelling: 0);

        var fellingType = input.ProposedFellingSourceRecords.First().OperationType;
        var cpt = input.ProposedFellingSourceRecords.First().Flov2CompartmentName;
        var area = input.ProposedFellingSourceRecords.First().AreaToBeFelled;

        input.ProposedFellingSourceRecords.ForEach(x =>
        {
            if (x is { OperationType: FellingOperationType.Thinning, IsRestocking: false })
            {
                x.NoRestockingReason = FixtureInstance.Create<string>(); // need to give it a reason if we're changing away from thinning
            }
            x.OperationType = fellingType;
            x.Flov2CompartmentName = cpt;
            x.AreaToBeFelled = area;
        });

        var sut = CreateSut();

        var result = await sut.ValidateImportFileSetAsync(woodlandOwnerId, Properties, input, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Single(result.Error);

        Assert.Equal("There are repeated felling operation types in the same compartment for the same application within the Proposed Felling records source", result.Error.Single().ErrorMessage);
    }

    [Theory, AutoData]
    public async Task WhenFellingDetailsHasUnknownApplicationId(Guid woodlandOwnerId)
    {
        var input = GenerateValidImportSets(applicationsCount: 1, restockingPerFelling: 0);

        var maxValidId = input.ApplicationSourceRecords.Max(x => x.ApplicationId);
        input.ProposedFellingSourceRecords.First().ApplicationId = maxValidId + 1;

        var sut = CreateSut();

        var result = await sut.ValidateImportFileSetAsync(woodlandOwnerId, Properties, input, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Single(result.Error);

        Assert.Equal($"Application Id {maxValidId + 1} was not found amongst imported application source records for proposed felling with id {input.ProposedFellingSourceRecords!.First().ProposedFellingId}", result.Error.Single().ErrorMessage);
    }

    [Theory, AutoData]
    public async Task WhenFellingDetailsHasNoCompartmentName(Guid woodlandOwnerId)
    {
        var input = GenerateValidImportSets(applicationsCount: 1, restockingPerFelling: 0);

        input.ProposedFellingSourceRecords.First().Flov2CompartmentName = string.Empty;

        var sut = CreateSut();

        var result = await sut.ValidateImportFileSetAsync(woodlandOwnerId, Properties, input, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Single(result.Error);

        Assert.Equal($"FLOv2 Compartment Name must be provided for proposed felling with id {input.ProposedFellingSourceRecords!.First().ProposedFellingId}", result.Error.Single().ErrorMessage);
    }

    [Theory, AutoData]
    public async Task WhenFellingDetailsHasCompartmentNameForWrongProperty(Guid woodlandOwnerId)
    {
        var input = GenerateValidImportSets(applicationsCount: 1, restockingPerFelling: 0);

        var applicationPropertyName = input.ApplicationSourceRecords.Single().Flov2PropertyName;
        var alternateProperty = Properties
            .Where(x => !x.Name.Equals(applicationPropertyName, StringComparison.InvariantCultureIgnoreCase))
            .RandomElement();

        var fellingToAlter = input.ProposedFellingSourceRecords.First();
        fellingToAlter.Flov2CompartmentName = alternateProperty.CompartmentIds.First().CompartmentName;

        var sut = CreateSut();

        var result = await sut.ValidateImportFileSetAsync(woodlandOwnerId, Properties, input, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Single(result.Error);

        Assert.Equal($"FLOv2 Compartment Name {fellingToAlter.Flov2CompartmentName} not found on linked application property for proposed felling with id {fellingToAlter.ProposedFellingId}", result.Error.Single().ErrorMessage);
    }

    [Theory, AutoData]
    public async Task WhenFellingOperationIsNone(Guid woodlandOwnerId)
    {
        var input = GenerateValidImportSets(applicationsCount: 1, restockingPerFelling: 0);

        input.ProposedFellingSourceRecords.First().OperationType = FellingOperationType.None;
        input.ProposedFellingSourceRecords.First().NoRestockingReason = FixtureInstance.Create<string>(); // need to give it a reason if we're changing away from thinning

        var sut = CreateSut();

        var result = await sut.ValidateImportFileSetAsync(woodlandOwnerId, Properties, input, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Single(result.Error);

        Assert.Equal($"Felling operation type {FellingOperationType.None} not valid for import process for proposed felling with id {input.ProposedFellingSourceRecords!.First().ProposedFellingId}", result.Error.Single().ErrorMessage);
    }

    [Theory, AutoData]
    public async Task WhenThereShouldBeRestockingOperationsButThereAreNone(Guid woodlandOwnerId)
    {
        var input = GenerateValidImportSets(applicationsCount: 1, fellingPerApplication: 1, restockingPerFelling: 0);

        input.ProposedFellingSourceRecords.First().OperationType = FellingOperationType.FellingIndividualTrees;
        input.ProposedFellingSourceRecords.First().IsRestocking = true;
        input.ProposedFellingSourceRecords.First().NoRestockingReason = null;

        var sut = CreateSut();

        var result = await sut.ValidateImportFileSetAsync(woodlandOwnerId, Properties, input, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Single(result.Error);

        Assert.Equal($"At least one restocking operation must be provided unless the operation type is Thinning or IsRestocking is false for proposed felling with id {input.ProposedFellingSourceRecords!.First().ProposedFellingId}", result.Error.Single().ErrorMessage);
    }

    [Theory, AutoData]
    public async Task WhenFellingAreaIsNegative(Guid woodlandOwnerId)
    {
        var input = GenerateValidImportSets(applicationsCount: 1, fellingPerApplication: 1, restockingPerFelling: 0);

        input.ProposedFellingSourceRecords.First().AreaToBeFelled = -2;

        var sut = CreateSut();

        var result = await sut.ValidateImportFileSetAsync(woodlandOwnerId, Properties, input, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Single(result.Error);

        Assert.Equal($"Area to be felled must be greater than zero for proposed felling with id {input.ProposedFellingSourceRecords!.First().ProposedFellingId}", result.Error.Single().ErrorMessage);
    }

    [Theory, AutoData]
    public async Task WhenFellingAreaIsZero(Guid woodlandOwnerId)
    {
        var input = GenerateValidImportSets(applicationsCount: 1, fellingPerApplication: 1, restockingPerFelling: 0);

        input.ProposedFellingSourceRecords.First().AreaToBeFelled = 0;

        var sut = CreateSut();

        var result = await sut.ValidateImportFileSetAsync(woodlandOwnerId, Properties, input, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Single(result.Error);

        Assert.Equal($"Area to be felled must be greater than zero for proposed felling with id {input.ProposedFellingSourceRecords!.First().ProposedFellingId}", result.Error.Single().ErrorMessage);
    }

    [Theory, AutoData]
    public async Task WhenFellingAreaIsLargerThanCompartmentArea(Guid woodlandOwnerId)
    {
        var input = GenerateValidImportSets(applicationsCount: 1, fellingPerApplication: 1, restockingPerFelling: 0);

        var property = Properties.Single(x => x.Name.Equals(input.ApplicationSourceRecords!.Single().Flov2PropertyName,
            StringComparison.InvariantCultureIgnoreCase));
        var cpt = property.CompartmentIds.Single(x =>
            x.CompartmentName.Equals(input.ProposedFellingSourceRecords!.First().Flov2CompartmentName, StringComparison.InvariantCultureIgnoreCase));

        input.ProposedFellingSourceRecords.First().AreaToBeFelled = cpt.Area!.Value + 1;

        var sut = CreateSut();

        var result = await sut.ValidateImportFileSetAsync(woodlandOwnerId, Properties, input, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Single(result.Error);

        Assert.Equal($"Felling area must not be greater than the compartment area for proposed felling with id {input.ProposedFellingSourceRecords!.First().ProposedFellingId}", result.Error.Single().ErrorMessage);
    }

    [Theory, AutoData]
    public async Task WhenFellingIndividualTreesButNumberOfTreesIsNull(Guid woodlandOwnerId)
    {
        var input = GenerateValidImportSets(applicationsCount: 1, fellingPerApplication: 1, restockingPerFelling: 0);

        input.ProposedFellingSourceRecords.First().OperationType = FellingOperationType.FellingIndividualTrees;
        input.ProposedFellingSourceRecords.First().NoRestockingReason = FixtureInstance.Create<string>(); // need to give it a reason if we're changing away from thinning
        input.ProposedFellingSourceRecords.First().NumberOfTrees = null;

        var sut = CreateSut();

        var result = await sut.ValidateImportFileSetAsync(woodlandOwnerId, Properties, input, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Single(result.Error);

        Assert.Equal($"Number of trees must be provided and greater than zero when felling individual trees for proposed felling with id {input.ProposedFellingSourceRecords!.First().ProposedFellingId}", result.Error.Single().ErrorMessage);
    }

    [Theory, AutoData]
    public async Task WhenFellingIndividualTreesButNumberOfTreesIsZero(Guid woodlandOwnerId)
    {
        var input = GenerateValidImportSets(applicationsCount: 1, fellingPerApplication: 1, restockingPerFelling: 0);

        input.ProposedFellingSourceRecords.First().OperationType = FellingOperationType.FellingIndividualTrees;
        input.ProposedFellingSourceRecords.First().NoRestockingReason = FixtureInstance.Create<string>(); // need to give it a reason if we're changing away from thinning
        input.ProposedFellingSourceRecords.First().NumberOfTrees = 0;

        var sut = CreateSut();

        var result = await sut.ValidateImportFileSetAsync(woodlandOwnerId, Properties, input, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Single(result.Error);

        Assert.Equal($"Number of trees must be provided and greater than zero when felling individual trees for proposed felling with id {input.ProposedFellingSourceRecords!.First().ProposedFellingId}", result.Error.Single().ErrorMessage);
    }

    [Theory, AutoData]
    public async Task WhenEstimatedFellingVolumeIsZero(Guid woodlandOwnerId)
    {
        var input = GenerateValidImportSets(applicationsCount: 1, fellingPerApplication: 1, restockingPerFelling: 0);

        input.ProposedFellingSourceRecords.First().EstimatedTotalFellingVolume = 0;

        var sut = CreateSut();

        var result = await sut.ValidateImportFileSetAsync(woodlandOwnerId, Properties, input, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Single(result.Error);

        Assert.Equal($"Estimated total felling volume must be greater than zero for proposed felling with id {input.ProposedFellingSourceRecords!.First().ProposedFellingId}", result.Error.Single().ErrorMessage);
    }

    [Theory, AutoData]
    public async Task WhenIsTpoButReferenceIsNull(Guid woodlandOwnerId)
    {
        var input = GenerateValidImportSets(applicationsCount: 1, fellingPerApplication: 1, restockingPerFelling: 0);

        input.ProposedFellingSourceRecords.First().IsPartOfTreePreservationOrder = true;
        input.ProposedFellingSourceRecords.First().TreePreservationOrderReference = null;

        var sut = CreateSut();

        var result = await sut.ValidateImportFileSetAsync(woodlandOwnerId, Properties, input, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Single(result.Error);

        Assert.Equal($"Tree preservation order reference must be provided when the felling is part of a tree preservation order for proposed felling with id {input.ProposedFellingSourceRecords!.First().ProposedFellingId}", result.Error.Single().ErrorMessage);
    }

    [Theory, AutoData]
    public async Task WhenIsTpoButReferenceIsEmpty(Guid woodlandOwnerId)
    {
        var input = GenerateValidImportSets(applicationsCount: 1, fellingPerApplication: 1, restockingPerFelling: 0);

        input.ProposedFellingSourceRecords.First().IsPartOfTreePreservationOrder = true;
        input.ProposedFellingSourceRecords.First().TreePreservationOrderReference = string.Empty;

        var sut = CreateSut();

        var result = await sut.ValidateImportFileSetAsync(woodlandOwnerId, Properties, input, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Single(result.Error);

        Assert.Equal($"Tree preservation order reference must be provided when the felling is part of a tree preservation order for proposed felling with id {input.ProposedFellingSourceRecords!.First().ProposedFellingId}", result.Error.Single().ErrorMessage);
    }

    [Theory, AutoData]
    public async Task WhenIsCaButReferenceIsNull(Guid woodlandOwnerId)
    {
        var input = GenerateValidImportSets(applicationsCount: 1, fellingPerApplication: 1, restockingPerFelling: 0);

        input.ProposedFellingSourceRecords.First().IsWithinConservationArea = true;
        input.ProposedFellingSourceRecords.First().ConservationAreaReference = null;

        var sut = CreateSut();

        var result = await sut.ValidateImportFileSetAsync(woodlandOwnerId, Properties, input, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Single(result.Error);

        Assert.Equal($"Conservation area reference must be provided when the felling is part of a conservation area for proposed felling with id {input.ProposedFellingSourceRecords!.First().ProposedFellingId}", result.Error.Single().ErrorMessage);
    }

    [Theory, AutoData]
    public async Task WhenIsCaButReferenceIsEmpty(Guid woodlandOwnerId)
    {
        var input = GenerateValidImportSets(applicationsCount: 1, fellingPerApplication: 1, restockingPerFelling: 0);

        input.ProposedFellingSourceRecords.First().IsWithinConservationArea = true;
        input.ProposedFellingSourceRecords.First().ConservationAreaReference = string.Empty;

        var sut = CreateSut();

        var result = await sut.ValidateImportFileSetAsync(woodlandOwnerId, Properties, input, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Single(result.Error);

        Assert.Equal($"Conservation area reference must be provided when the felling is part of a conservation area for proposed felling with id {input.ProposedFellingSourceRecords!.First().ProposedFellingId}", result.Error.Single().ErrorMessage);
    }

    [Theory, AutoData]
    public async Task WhenIsNoRestockingButReasonIsNull(Guid woodlandOwnerId)
    {
        var input = GenerateValidImportSets(applicationsCount: 1, fellingPerApplication: 1, restockingPerFelling: 0);

        input.ProposedFellingSourceRecords.First().IsRestocking = false;
        input.ProposedFellingSourceRecords.First().NoRestockingReason = null;

        var sut = CreateSut();

        var result = await sut.ValidateImportFileSetAsync(woodlandOwnerId, Properties, input, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Single(result.Error);

        Assert.Equal($"Reason for not restocking must be provided when not restocking and the operation type is not thinning for proposed felling with id {input.ProposedFellingSourceRecords!.First().ProposedFellingId}", result.Error.Single().ErrorMessage);
    }

    [Theory, AutoData]
    public async Task WhenIsNoRestockingButReasonIsEmpty(Guid woodlandOwnerId)
    {
        var input = GenerateValidImportSets(applicationsCount: 1, fellingPerApplication: 1, restockingPerFelling: 0);

        input.ProposedFellingSourceRecords.First().IsRestocking = false;
        input.ProposedFellingSourceRecords.First().NoRestockingReason = string.Empty;

        var sut = CreateSut();

        var result = await sut.ValidateImportFileSetAsync(woodlandOwnerId, Properties, input, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Single(result.Error);

        Assert.Equal($"Reason for not restocking must be provided when not restocking and the operation type is not thinning for proposed felling with id {input.ProposedFellingSourceRecords!.First().ProposedFellingId}", result.Error.Single().ErrorMessage);
    }

    [Theory, AutoData]
    public async Task WhenSpeciesIsEmpty(Guid woodlandOwnerId)
    {
        var input = GenerateValidImportSets(applicationsCount: 1, fellingPerApplication: 1, restockingPerFelling: 0);

        input.ProposedFellingSourceRecords.First().Species = string.Empty;

        var sut = CreateSut();

        var result = await sut.ValidateImportFileSetAsync(woodlandOwnerId, Properties, input, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Single(result.Error);

        Assert.Equal($"Species must be provided for proposed felling with id {input.ProposedFellingSourceRecords!.First().ProposedFellingId}", result.Error.Single().ErrorMessage);
    }

    [Theory, AutoData]
    public async Task WhenSpeciesIsNotCommaSeparatedValues(Guid woodlandOwnerId)
    {
        var input = GenerateValidImportSets(applicationsCount: 1, fellingPerApplication: 1, restockingPerFelling: 0);

        input.ProposedFellingSourceRecords.First().Species = FixtureInstance.Create<string>();

        var sut = CreateSut();

        var result = await sut.ValidateImportFileSetAsync(woodlandOwnerId, Properties, input, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Single(result.Error);

        Assert.Equal($"Species {input.ProposedFellingSourceRecords.First().Species} contains invalid species codes for proposed felling with id {input.ProposedFellingSourceRecords!.First().ProposedFellingId}", result.Error.Single().ErrorMessage);
    }

    [Theory, AutoData]
    public async Task WhenSpeciesContainsUnknownCode(Guid woodlandOwnerId)
    {
        var input = GenerateValidImportSets(applicationsCount: 1, fellingPerApplication: 1, restockingPerFelling: 0);

        input.ProposedFellingSourceRecords.First().Species += ",UNKNOWN";

        var sut = CreateSut();

        var result = await sut.ValidateImportFileSetAsync(woodlandOwnerId, Properties, input, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Single(result.Error);

        Assert.Equal($"Species {input.ProposedFellingSourceRecords.First().Species} contains invalid species codes for proposed felling with id {input.ProposedFellingSourceRecords!.First().ProposedFellingId}", result.Error.Single().ErrorMessage);
    }

    [Theory, AutoData]
    public async Task WhenSpeciesContainsRepeatedCode(Guid woodlandOwnerId)
    {
        var input = GenerateValidImportSets(applicationsCount: 1, fellingPerApplication: 1, restockingPerFelling: 0);

        var species1 = SpeciesCodes.RandomElement();
        var species2 = SpeciesCodes.Where(x => x != species1).RandomElement();

        input.ProposedFellingSourceRecords.First().Species = $"{species1},{species2},{species1}";

        var sut = CreateSut();

        var result = await sut.ValidateImportFileSetAsync(woodlandOwnerId, Properties, input, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Single(result.Error);

        Assert.Equal($"Species {input.ProposedFellingSourceRecords.First().Species} contains repeated duplicate species codes for proposed felling with id {input.ProposedFellingSourceRecords!.First().ProposedFellingId}", result.Error.Single().ErrorMessage);
    }
}