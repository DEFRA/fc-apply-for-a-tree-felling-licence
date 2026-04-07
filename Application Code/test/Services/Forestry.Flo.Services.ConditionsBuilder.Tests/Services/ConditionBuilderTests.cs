using AutoFixture;
using AutoFixture.Xunit2;
using Forestry.Flo.Services.ConditionsBuilder.Configuration;
using Forestry.Flo.Services.ConditionsBuilder.Models;
using Forestry.Flo.Services.ConditionsBuilder.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Xunit;

namespace Forestry.Flo.Services.ConditionsBuilder.Tests.Services;

public class ConditionBuilderTests
{
    private static readonly Fixture Fixture = new();

    private ConditionsBuilderOptions _options;

    public static IEnumerable<object[]> ApplicableConditionBuilderTestData()
    {
        yield return new object[] { RestockingProposalType.None, null };
        yield return new object[] { RestockingProposalType.CreateDesignedOpenGround, null };
        yield return new object[] { RestockingProposalType.DoNotIntendToRestock, null };
        yield return new object[] { RestockingProposalType.PlantAnAlternativeArea, typeof(RestockByPlantingConditionBuilder) };
        yield return new object[] { RestockingProposalType.NaturalColonisation, null };
        yield return new object[] { RestockingProposalType.PlantAnAlternativeAreaWithIndividualTrees, typeof(RestockByPlantingConditionBuilder) };
        yield return new object[] { RestockingProposalType.ReplantTheFelledArea, typeof(RestockByPlantingConditionBuilder) };
        yield return new object[] { RestockingProposalType.RestockByNaturalRegeneration, typeof(NaturalRegenerationConditionBuilder) };
        yield return new object[] { RestockingProposalType.RestockWithCoppiceRegrowth, typeof(CoppiceRegrowthConditionBuilder) };
        yield return new object[] { RestockingProposalType.RestockWithIndividualTrees, typeof(RestockByPlantingConditionBuilder) };
    }

    [Theory]
    [MemberData(nameof(ApplicableConditionBuilderTestData))]
    public void CorrectConditionBuilderAppliesToCompartment(
        RestockingProposalType compartmentRestockingType,
        Type expectedConditionBuilderType)
    {
        var (replantCondition, regenCondition, coppiceCondition) = GetConditionBuilders();

        var compartment = Fixture.Create<RestockingOperationDetails>();
        compartment.RestockingProposalType = compartmentRestockingType;

        var isReplantCondition = replantCondition.AppliesToOperation(compartment);
        Assert.Equal(expectedConditionBuilderType == typeof(RestockByPlantingConditionBuilder), isReplantCondition);

        var isRegenCondition = regenCondition.AppliesToOperation(compartment);
        Assert.Equal(expectedConditionBuilderType == typeof(NaturalRegenerationConditionBuilder), isRegenCondition);

        var isCoppiceCondition = coppiceCondition.AppliesToOperation(compartment);
        Assert.Equal(expectedConditionBuilderType == typeof(CoppiceRegrowthConditionBuilder), isCoppiceCondition);
    }

    [Theory, AutoData]
    public void ReturnsFailureIfAttemptToCalculateConditionWithInvalidCompartment(
        RestockingOperationDetails compartment)
    {
        var (replantCondition, _, _) = GetConditionBuilders();

        compartment.RestockingProposalType = RestockingProposalType.NaturalColonisation;

        var result = replantCondition.CalculateCondition(new List<RestockingOperationDetails> { compartment });

        Assert.True(result.IsFailure);
    }

    [Theory, AutoData]
    public void CanMatchCompartmentsWhichAreFunctionallyIdentical(
        RestockingOperationDetails baseCompartment,
        Guid compartmentId1,
        Guid compartmentId2,
        string compartmentNumber1,
        string compartmentNumber2,
        string subCompartmentName1,
        string subCompartmentName2)
    {
        var (replantCondition, _, _) = GetConditionBuilders();

        var (compartment1, compartment2) = GetTestCompartments(baseCompartment, compartmentId1, compartmentId2,
            compartmentNumber1, compartmentNumber2, subCompartmentName1, subCompartmentName2);

        var result = replantCondition.CalculateCondition(new List<RestockingOperationDetails> { compartment1, compartment2 });

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);

        Assert.Contains(result.Value.Single().AppliesToSubmittedCompartmentIds, x => x == compartmentId1);
        Assert.Contains(result.Value.Single().AppliesToSubmittedCompartmentIds, x => x == compartmentId2);
    }

    [Theory, AutoData]
    public void DoNotMatchCompartmentsWithDifferentFellingTypes(
        RestockingOperationDetails baseCompartment,
        Guid compartmentId1,
        Guid compartmentId2,
        string compartmentNumber1,
        string compartmentNumber2,
        string subCompartmentName1,
        string subCompartmentName2)
    {
        var (replantCondition, _, _) = GetConditionBuilders();

        var (compartment1, compartment2) = GetTestCompartments(baseCompartment, compartmentId1, compartmentId2,
            compartmentNumber1, compartmentNumber2, subCompartmentName1, subCompartmentName2);
        compartment1.FellingOperationType = FellingOperationType.ClearFelling;
        compartment2.FellingOperationType = FellingOperationType.FellingOfCoppice;

        var result = replantCondition.CalculateCondition(new List<RestockingOperationDetails> { compartment1, compartment2 });

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);

        Assert.Contains(result.Value.First().AppliesToSubmittedCompartmentIds, x => x == compartmentId1);
        Assert.Contains(result.Value.Last().AppliesToSubmittedCompartmentIds, x => x == compartmentId2);
    }

    [Theory, AutoData]
    public void DoNotMatchCompartmentsWithDifferentOpenSpace(
        RestockingOperationDetails baseCompartment,
        Guid compartmentId1,
        Guid compartmentId2,
        string compartmentNumber1,
        string compartmentNumber2,
        string subCompartmentName1,
        string subCompartmentName2)
    {
        var (replantCondition, _, _) = GetConditionBuilders();

        var (compartment1, compartment2) = GetTestCompartments(baseCompartment, compartmentId1, compartmentId2,
            compartmentNumber1, compartmentNumber2, subCompartmentName1, subCompartmentName2);
        compartment2.PercentOpenSpace += 10;

        var result = replantCondition.CalculateCondition(new List<RestockingOperationDetails> { compartment1, compartment2 });

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);

        Assert.Contains(result.Value.First().AppliesToSubmittedCompartmentIds, x => x == compartmentId1);
        Assert.Contains(result.Value.Last().AppliesToSubmittedCompartmentIds, x => x == compartmentId2);
    }

    [Theory, AutoData]
    public void DoNotMatchCompartmentsWithDifferentSpecies(
        RestockingOperationDetails baseCompartment,
        Guid compartmentId1,
        Guid compartmentId2,
        string compartmentNumber1,
        string compartmentNumber2,
        string subCompartmentName1,
        string subCompartmentName2)
    {
        var (replantCondition, _, _) = GetConditionBuilders();

        var (compartment1, compartment2) = GetTestCompartments(baseCompartment, compartmentId1, compartmentId2,
            compartmentNumber1, compartmentNumber2, subCompartmentName1, subCompartmentName2);
        compartment2.RestockingSpecies[0].SpeciesCode = "XYZ";

        var result = replantCondition.CalculateCondition(new List<RestockingOperationDetails> { compartment1, compartment2 });

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);

        Assert.Contains(result.Value.First().AppliesToSubmittedCompartmentIds, x => x == compartmentId1);
        Assert.Contains(result.Value.Last().AppliesToSubmittedCompartmentIds, x => x == compartmentId2);
    }

    [Theory, AutoData]
    public void DoNotMatchCompartmentsWithDifferentSpeciesPercentages(
        RestockingOperationDetails baseCompartment,
        Guid compartmentId1,
        Guid compartmentId2,
        string compartmentNumber1,
        string compartmentNumber2,
        string subCompartmentName1,
        string subCompartmentName2)
    {
        var (replantCondition, _, _) = GetConditionBuilders();

        var (compartment1, compartment2) = GetTestCompartments(baseCompartment, compartmentId1, compartmentId2,
            compartmentNumber1, compartmentNumber2, subCompartmentName1, subCompartmentName2);
        compartment2.RestockingSpecies[0].Percentage += 10;

        var result = replantCondition.CalculateCondition(new List<RestockingOperationDetails> { compartment1, compartment2 });

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);

        Assert.Contains(result.Value.First().AppliesToSubmittedCompartmentIds, x => x == compartmentId1);
        Assert.Contains(result.Value.Last().AppliesToSubmittedCompartmentIds, x => x == compartmentId2);
    }

    [Theory, AutoData]
    public void DoNotMatchCompartmentsWithDifferentRestockingDensity(
        RestockingOperationDetails baseCompartment,
        Guid compartmentId1,
        Guid compartmentId2,
        string compartmentNumber1,
        string compartmentNumber2,
        string subCompartmentName1,
        string subCompartmentName2)
    {
        var (replantCondition, _, _) = GetConditionBuilders();

        var (compartment1, compartment2) = GetTestCompartments(baseCompartment, compartmentId1, compartmentId2,
            compartmentNumber1, compartmentNumber2, subCompartmentName1, subCompartmentName2);
        compartment2.RestockingDensity += 10;

        var result = replantCondition.CalculateCondition(new List<RestockingOperationDetails> { compartment1, compartment2 });

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);

        Assert.Contains(result.Value.First().AppliesToSubmittedCompartmentIds, x => x == compartmentId1);
        Assert.Contains(result.Value.Last().AppliesToSubmittedCompartmentIds, x => x == compartmentId2);
    }

    [Theory, AutoData]
    public void DoNotMatchCompartmentsWithDifferentNaturalRegenIfRestockByNaturalRegen(
        RestockingOperationDetails baseCompartment,
        Guid compartmentId1,
        Guid compartmentId2,
        string compartmentNumber1,
        string compartmentNumber2,
        string subCompartmentName1,
        string subCompartmentName2)
    {
        var (_, regenCondition, _) = GetConditionBuilders();

        var (compartment1, compartment2) = GetTestCompartments(baseCompartment, compartmentId1, compartmentId2,
            compartmentNumber1, compartmentNumber2, subCompartmentName1, subCompartmentName2);
        compartment1.RestockingProposalType = RestockingProposalType.RestockByNaturalRegeneration;
        compartment2.RestockingProposalType = RestockingProposalType.RestockByNaturalRegeneration;
        compartment2.PercentNaturalRegeneration += 10;

        var result = regenCondition.CalculateCondition(new List<RestockingOperationDetails> { compartment1, compartment2 });

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);

        Assert.Contains(result.Value.First().AppliesToSubmittedCompartmentIds, x => x == compartmentId1);
        Assert.Contains(result.Value.Last().AppliesToSubmittedCompartmentIds, x => x == compartmentId2);
    }

    [Theory, AutoData]
    public void DoNotMatchCompartmentsWithDifferentNaturalRegenIfRestockByCoppice(
        RestockingOperationDetails baseCompartment,
        Guid compartmentId1,
        Guid compartmentId2,
        string compartmentNumber1,
        string compartmentNumber2,
        string subCompartmentName1,
        string subCompartmentName2)
    {
        var (_, _, coppiceCondition) = GetConditionBuilders();

        var (compartment1, compartment2) = GetTestCompartments(baseCompartment, compartmentId1, compartmentId2,
            compartmentNumber1, compartmentNumber2, subCompartmentName1, subCompartmentName2);
        compartment1.RestockingProposalType = RestockingProposalType.RestockWithCoppiceRegrowth;
        compartment2.RestockingProposalType = RestockingProposalType.RestockWithCoppiceRegrowth;
        compartment2.PercentNaturalRegeneration += 10;

        var result = coppiceCondition.CalculateCondition(new List<RestockingOperationDetails> { compartment1, compartment2 });

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);

        Assert.Contains(result.Value.First().AppliesToSubmittedCompartmentIds, x => x == compartmentId1);
        Assert.Contains(result.Value.Last().AppliesToSubmittedCompartmentIds, x => x == compartmentId2);
    }

    [Theory, AutoData]
    public void CanMatchCompartmentsWithDifferentNaturalRegenIfPlanting(
        RestockingOperationDetails baseCompartment,
        Guid compartmentId1,
        Guid compartmentId2,
        string compartmentNumber1,
        string compartmentNumber2,
        string subCompartmentName1,
        string subCompartmentName2)
    {
        var (replantCondition, _, _) = GetConditionBuilders();

        var (compartment1, compartment2) = GetTestCompartments(baseCompartment, compartmentId1, compartmentId2,
            compartmentNumber1, compartmentNumber2, subCompartmentName1, subCompartmentName2);
        compartment1.RestockingProposalType = RestockingProposalType.ReplantTheFelledArea;
        compartment2.RestockingProposalType = RestockingProposalType.PlantAnAlternativeArea;
        compartment2.PercentNaturalRegeneration += 10;

        var result = replantCondition.CalculateCondition(new List<RestockingOperationDetails> { compartment1, compartment2 });

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);

        Assert.Contains(result.Value.Single().AppliesToSubmittedCompartmentIds, x => x == compartmentId1);
        Assert.Contains(result.Value.Single().AppliesToSubmittedCompartmentIds, x => x == compartmentId2);
    }

    [Theory, AutoData]
    public void ImportsCorrectDetailsIntoConditionText(
        RestockingOperationDetails compartment)
    {
        compartment.RestockingProposalType = RestockingProposalType.ReplantTheFelledArea;

        var (replantCondition, _, _) = GetConditionBuilders();

        var speciesList = compartment
            .RestockingSpecies
            .OrderBy(x => x.SpeciesCode)
            .Select(x => $"{x.Percentage:0.00}% {x.SpeciesName}")
            .ToList();
        var expectedSpeciesText = string.Join(", ", speciesList);
        var expectedCompartmentName =
            $"compartment {compartment.RestockingCompartmentNumber}";

        var result = replantCondition.CalculateCondition(new List<RestockingOperationDetails> { compartment });

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
        Assert.Equal(expectedSpeciesText, result.Value.Single().ConditionsText[0]);
        Assert.Equal($"{compartment.RestockingDensity.ToString()} stems per Ha", result.Value.Single().ConditionsText[1]);
        Assert.Equal(expectedCompartmentName, result.Value.Single().ConditionsText[2]);
        Assert.Equal($"{compartment.PercentNaturalRegeneration:0.00}% natural regeneration", result.Value.Single().ConditionsText[3]);
        Assert.Equal(_options.ReplantingOptions.ConditionText[4], result.Value.Single().ConditionsText[4]);
        Assert.Equal(_options.ReplantingOptions.ConditionText[5], result.Value.Single().ConditionsText[5]);
    }

    [Theory, AutoData]
    public void ImportsCorrectDetailsIntoConditionTextForLessThan100PercentRegen(
        RestockingOperationDetails compartment)
    {
        compartment.RestockingProposalType = RestockingProposalType.RestockByNaturalRegeneration;
        compartment.PercentNaturalRegeneration = 80;

        var (_, regenCondition, _) = GetConditionBuilders();

        var speciesList = compartment
            .RestockingSpecies
            .OrderBy(x => x.SpeciesCode)
            .Select(x => $"{x.Percentage:0.00}% {x.SpeciesName}")
            .ToList();
        var expectedSpeciesText = string.Join(", ", speciesList);
        var expectedCompartmentName =
            $"compartment {compartment.RestockingCompartmentNumber}";

        var result = regenCondition.CalculateCondition(new List<RestockingOperationDetails> { compartment });

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
        Assert.Equal(expectedSpeciesText, result.Value.Single().ConditionsText[0]);
        Assert.Equal($"{compartment.RestockingDensity.ToString()} stems per Ha", result.Value.Single().ConditionsText[1]);
        Assert.Equal(expectedCompartmentName, result.Value.Single().ConditionsText[2]);
        Assert.Equal($"{compartment.PercentNaturalRegeneration:0.00}% natural regeneration", result.Value.Single().ConditionsText[3]);
        Assert.Equal(_options.NaturalRegenOptions.ConditionText[4], result.Value.Single().ConditionsText[4]);
        Assert.Equal(_options.NaturalRegenOptions.ConditionText[5], result.Value.Single().ConditionsText[5]);
        Assert.Equal("If planting alongside natural regeneration, the remaining area will be planted with {0} to achieve not less than {1}.", result.Value.Single().ConditionsText[6]);
    }

    [Theory, AutoData]
    public void ImportsCorrectDetailsIntoConditionTextForSingleDigitPercentRegen(
        RestockingOperationDetails compartment)
    {
        compartment.RestockingProposalType = RestockingProposalType.RestockByNaturalRegeneration;
        compartment.PercentNaturalRegeneration = 8.5;

        var (_, regenCondition, _) = GetConditionBuilders();

        var speciesList = compartment
            .RestockingSpecies
            .OrderBy(x => x.SpeciesCode)
            .Select(x => $"{x.Percentage:0.00}% {x.SpeciesName}")
            .ToList();
        var expectedSpeciesText = string.Join(", ", speciesList);
        var expectedCompartmentName =
            $"compartment {compartment.RestockingCompartmentNumber}";

        var result = regenCondition.CalculateCondition(new List<RestockingOperationDetails> { compartment });

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
        Assert.Equal(expectedSpeciesText, result.Value.Single().ConditionsText[0]);
        Assert.Equal($"{compartment.RestockingDensity.ToString()} stems per Ha", result.Value.Single().ConditionsText[1]);
        Assert.Equal(expectedCompartmentName, result.Value.Single().ConditionsText[2]);
        Assert.Equal($"8.50% natural regeneration", result.Value.Single().ConditionsText[3]);
        Assert.Equal(_options.NaturalRegenOptions.ConditionText[4], result.Value.Single().ConditionsText[4]);
        Assert.Equal(_options.NaturalRegenOptions.ConditionText[5], result.Value.Single().ConditionsText[5]);
        Assert.Equal("If planting alongside natural regeneration, the remaining area will be planted with {0} to achieve not less than {1}.", result.Value.Single().ConditionsText[6]);
    }

    [Theory, AutoData]
    public void ImportsCorrectDetailsIntoConditionTextForLessThan100PercentCoppice(
        RestockingOperationDetails compartment)
    {
        compartment.RestockingProposalType = RestockingProposalType.RestockWithCoppiceRegrowth;
        compartment.PercentNaturalRegeneration = 80;

        var (_, _, coppiceCondition) = GetConditionBuilders();

        var speciesList = compartment
            .RestockingSpecies
            .OrderBy(x => x.SpeciesCode)
            .Select(x => $"{x.Percentage:0.00}% {x.SpeciesName}")
            .ToList();
        var expectedSpeciesText = string.Join(", ", speciesList);
        var expectedCompartmentName =
            $"compartment {compartment.RestockingCompartmentNumber}";

        var result = coppiceCondition.CalculateCondition(new List<RestockingOperationDetails> { compartment });

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
        Assert.Equal(expectedSpeciesText, result.Value.Single().ConditionsText[0]);
        Assert.Equal($"{compartment.RestockingDensity.ToString()} stems per Ha", result.Value.Single().ConditionsText[1]);
        Assert.Equal(expectedCompartmentName, result.Value.Single().ConditionsText[2]);
        Assert.Equal($"{compartment.PercentNaturalRegeneration:0.00}% coppice regrowth", result.Value.Single().ConditionsText[3]);
        Assert.Equal(_options.NaturalRegenOptions.ConditionText[4], result.Value.Single().ConditionsText[4]);
        Assert.Equal(_options.NaturalRegenOptions.ConditionText[5], result.Value.Single().ConditionsText[5]);
        Assert.Equal("If planting alongside coppice regrowth, the remaining area will be planted with {0} to achieve not less than {1}.", result.Value.Single().ConditionsText[6]);
        Assert.Equal($"{compartment.RestockingDensity.ToString()} stools per Ha", result.Value.Single().ConditionsText[7]);
    }

    [Theory, AutoData]
    public void ImportsCorrectDetailsIntoConditionTextFor100PercentRegen(
        RestockingOperationDetails compartment)
    {
        compartment.RestockingProposalType = RestockingProposalType.RestockByNaturalRegeneration;
        compartment.PercentNaturalRegeneration = 100;

        var (_, regenCondition, _) = GetConditionBuilders();

        var speciesList = compartment
            .RestockingSpecies
            .OrderBy(x => x.SpeciesCode)
            .Select(x => $"{x.Percentage:0.00}% {x.SpeciesName}")
            .ToList();
        var expectedSpeciesText = string.Join(", ", speciesList);
        var expectedCompartmentName =
            $"compartment {compartment.RestockingCompartmentNumber}";

        var result = regenCondition.CalculateCondition(new List<RestockingOperationDetails> { compartment });

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
        Assert.Equal(expectedSpeciesText, result.Value.Single().ConditionsText[0]);
        Assert.Equal($"{compartment.RestockingDensity.ToString()} stems per Ha", result.Value.Single().ConditionsText[1]);
        Assert.Equal(expectedCompartmentName, result.Value.Single().ConditionsText[2]);
        Assert.Equal($"{compartment.PercentNaturalRegeneration:0.00}% natural regeneration", result.Value.Single().ConditionsText[3]);
        Assert.Equal(_options.NaturalRegenOptions.ConditionText[4], result.Value.Single().ConditionsText[4]);
        Assert.Equal(_options.NaturalRegenOptions.ConditionText[5], result.Value.Single().ConditionsText[5]);
        Assert.Equal($"{compartment.RestockingDensity.ToString()} stools per Ha", result.Value.Single().ConditionsText[6]);
        Assert.Equal(7, result.Value.Single().ConditionsText.Length);  // the planting alongside text should not be included for 100% regen
    }

    [Theory, AutoData]
    public void ImportsCorrectDetailsIntoConditionTextFor100PercentCoppice(
        RestockingOperationDetails compartment)
    {
        compartment.RestockingProposalType = RestockingProposalType.RestockWithCoppiceRegrowth;
        compartment.PercentNaturalRegeneration = 100;

        var (_, _, coppiceCondition) = GetConditionBuilders();

        var speciesList = compartment
            .RestockingSpecies
            .OrderBy(x => x.SpeciesCode)
            .Select(x => $"{x.Percentage:0.00}% {x.SpeciesName}")
            .ToList();
        var expectedSpeciesText = string.Join(", ", speciesList);
        var expectedCompartmentName =
            $"compartment {compartment.RestockingCompartmentNumber}";

        var result = coppiceCondition.CalculateCondition(new List<RestockingOperationDetails> { compartment });

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
        Assert.Equal(expectedSpeciesText, result.Value.Single().ConditionsText[0]);
        Assert.Equal($"{compartment.RestockingDensity.ToString()} stems per Ha", result.Value.Single().ConditionsText[1]);
        Assert.Equal(expectedCompartmentName, result.Value.Single().ConditionsText[2]);
        Assert.Equal($"{compartment.PercentNaturalRegeneration:0.00}% coppice regrowth", result.Value.Single().ConditionsText[3]);
        Assert.Equal(_options.NaturalRegenOptions.ConditionText[4], result.Value.Single().ConditionsText[4]);
        Assert.Equal(_options.NaturalRegenOptions.ConditionText[5], result.Value.Single().ConditionsText[5]);
        Assert.Equal($"{compartment.RestockingDensity.ToString()} stools per Ha", result.Value.Single().ConditionsText[6]);
        Assert.Equal(7, result.Value.Single().ConditionsText.Length);  // the planting alongside text should not be included for 100% regen
    }

    private (IBuildCondition ReplantCondition, IBuildCondition RegenCondition, IBuildCondition CoppiceCondition) GetConditionBuilders()
    {
        _options = Fixture.Create<ConditionsBuilderOptions>();

        var text = new List<string>
        {
            ConditionOptions.SpeciesParameter,
            ConditionOptions.DensityParameter,
            ConditionOptions.CompartmentsParameter,
            ConditionOptions.RegenerationParameter,
            "{0}",
            Fixture.Create<string>(),
            ConditionOptions.LessThan100PercentRegenerationParameter,
            ConditionOptions.DensityCoppiceParameter
        };

        _options.ReplantingOptions.ConditionText = text.ToArray();
        _options.NaturalRegenOptions.ConditionText = text.ToArray();
        _options.CoppiceRegrowthOptions.ConditionText = text.ToArray();

        var replantCondition = new RestockByPlantingConditionBuilder(
            new OptionsWrapper<ConditionsBuilderOptions>(_options),
            new NullLogger<RestockByPlantingConditionBuilder>());

        var regenCondition = new NaturalRegenerationConditionBuilder(
            new OptionsWrapper<ConditionsBuilderOptions>(_options),
            new NullLogger<NaturalRegenerationConditionBuilder>());

        var coppiceCondition = new CoppiceRegrowthConditionBuilder(
                       new OptionsWrapper<ConditionsBuilderOptions>(_options),
                                  new NullLogger<CoppiceRegrowthConditionBuilder>());

        return (replantCondition, regenCondition, coppiceCondition);
    }

    private (RestockingOperationDetails compartment1, RestockingOperationDetails compartment2) GetTestCompartments(
        RestockingOperationDetails baseCompartment,
        Guid compartmentId1,
        Guid compartmentId2,
        string compartmentNumber1,
        string compartmentNumber2,
        string subCompartmentName1,
        string subCompartmentName2)
    {
        var speciesJson = JsonSerializer.Serialize(baseCompartment.RestockingSpecies);
        
        var compartment1 = new RestockingOperationDetails
        {
            RestockingSubmittedFlaPropertyCompartmentId = compartmentId1,
            FellingCompartmentNumber = compartmentNumber1,
            FellingSubcompartmentName = subCompartmentName1,
            RestockingCompartmentNumber = compartmentNumber1,
            RestockingSubcompartmentName = subCompartmentName1,
            RestockingProposalType = RestockingProposalType.ReplantTheFelledArea,
            PercentNaturalRegeneration = baseCompartment.PercentNaturalRegeneration,
            RestockingSpecies = JsonSerializer.Deserialize<List<RestockingSpecies>>(speciesJson),
            RestockingDensity = baseCompartment.RestockingDensity,
            FellingOperationType = baseCompartment.FellingOperationType,
            PercentOpenSpace = baseCompartment.PercentOpenSpace,
            TotalRestockingArea = baseCompartment.TotalRestockingArea
        };

        var compartment2 = new RestockingOperationDetails
        {
            RestockingCompartmentNumber = compartmentNumber2,
            RestockingSubcompartmentName = subCompartmentName2,
            RestockingSubmittedFlaPropertyCompartmentId = compartmentId2,
            FellingCompartmentNumber = compartmentNumber1,
            FellingSubcompartmentName = subCompartmentName1,
            RestockingProposalType = RestockingProposalType.ReplantTheFelledArea,
            PercentNaturalRegeneration = baseCompartment.PercentNaturalRegeneration,
            RestockingSpecies = JsonSerializer.Deserialize<List<RestockingSpecies>>(speciesJson),
            RestockingDensity = baseCompartment.RestockingDensity,
            FellingOperationType = baseCompartment.FellingOperationType,
            PercentOpenSpace = baseCompartment.PercentOpenSpace,
            TotalRestockingArea = baseCompartment.TotalRestockingArea
        };

        return (compartment1, compartment2);
    }
}