using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common.Extensions;
using Forestry.Flo.Services.ConditionsBuilder.Configuration;
using Forestry.Flo.Services.ConditionsBuilder.Entities;
using Forestry.Flo.Services.ConditionsBuilder.Models;
using Microsoft.Extensions.Logging;

namespace Forestry.Flo.Services.ConditionsBuilder.Services;

public abstract class ConditionBuilderBase : IBuildCondition
{
    private readonly ILogger<ConditionBuilderBase> _logger;
    private readonly ConditionOptions _conditionOptions;

    protected ConditionBuilderBase(
        ConditionOptions conditionOptions,
        ILogger<ConditionBuilderBase> logger)
    {
        _logger = logger;
        _conditionOptions = Guard.Against.Null(conditionOptions);
    }

    /// <inheritdoc />
    public abstract bool AppliesToOperation(RestockingOperationDetails restockingOperation);

    /// <summary>
    /// Indicates whether the condition must match on the natural regeneration percentage.
    /// </summary>
    protected abstract bool MustMatchOnNaturalRegenPercentage { get; }

    /// <inheritdoc />
    public Result<List<CalculatedCondition>> CalculateCondition(List<RestockingOperationDetails> restockingOperations)
    {
        if (restockingOperations.NotAny(AppliesToOperation))
        {
            _logger.LogError("Invalid restocking operation encountered in CalculateCondition");
            return Result.Failure<List<CalculatedCondition>>("Cannot calculate condition with invalid restocking operations");
        }

        try
        {
            var matchedRestockingLists = GetMatchedRestockingOperations(restockingOperations);

            var result = new List<CalculatedCondition>(matchedRestockingLists.Count);
            foreach (var matchedRestocking in matchedRestockingLists)
            {
                var condition = new CalculatedCondition
                {
                    ConditionsText = ApplyParametersToText(matchedRestocking, _conditionOptions.ConditionText),
                    Parameters = _conditionOptions.ConditionParameters.Select(x => new ConditionParameter
                    {
                        Value = ApplyParametersToString(matchedRestocking, x.DefaultValue),
                        Description = x.Description,
                        Index = x.Index
                    }).ToList(),
                    AppliesToSubmittedCompartmentIds = matchedRestocking.RestockingOperations
                        .Select(x => x.RestockingSubmittedFlaPropertyCompartmentId).ToList()
                };

                result.Add(condition);
            }

            return Result.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception caught in CalculateCondition");
            return Result.Failure<List<CalculatedCondition>>("Error calculating conditions: " + ex.Message);
        }
    }

    private List<MatchedRestockingOperations> GetMatchedRestockingOperations(
        List<RestockingOperationDetails> restockingOperations)
    {
        var dictionary = new Dictionary<string, List<RestockingOperationDetails>>();

        foreach (var compartment in restockingOperations)
        {
            var keyBuilder = new List<string>
            {
                $"{compartment.FellingOperationType}",
                $"{compartment.PercentOpenSpace:0.00}",
                string.Join(",", compartment.RestockingSpecies
                    .OrderBy(x => x.SpeciesCode)
                    .Select(x => x.SpeciesCode + ":" + $"{x.Percentage:0.00}")),
                $"{compartment.RestockingDensity:0.00}"
            };

            if (MustMatchOnNaturalRegenPercentage)
            {
                keyBuilder.Add($"{compartment.PercentNaturalRegeneration:0.00}");
            }

            var key = string.Join("|", keyBuilder);

            if (dictionary.ContainsKey(key))
            {
                dictionary[key].Add(compartment);
            }
            else
            {
                dictionary.Add(key, new List<RestockingOperationDetails> { compartment });
            }
        }

        return dictionary.Values.Select(x => new MatchedRestockingOperations(x)).ToList();
    }

    private static string GetSpeciesText(RestockingOperationDetails restockingOperation)
    {
        var list = restockingOperation
            .RestockingSpecies
            .OrderBy(x => x.SpeciesCode)
            .Select(x => $"{x.Percentage:0.00}% {x.SpeciesName}")
            .ToList();

        return string.Join(", ", list);
    }

    private static string GetDensityText(double? restockingDensity) => $"{restockingDensity} stems per Ha";
    private static string GetDensityTextForCoppice(double? restockingDensity) => $"{restockingDensity} stools per Ha";
    private static string GetTreesText(int numberOfTrees) => $"{numberOfTrees} trees";

    private static string[] ApplyParametersToText(MatchedRestockingOperations restockingOperations, string[] originalLines)
    {
        var restockingOperation = restockingOperations.RestockingOperations.First();
        var regen = restockingOperation.RestockingProposalType == RestockingProposalType.RestockWithCoppiceRegrowth
            ? $"{restockingOperation.PercentNaturalRegeneration:0.00}% coppice regrowth"
            : $"{restockingOperation.PercentNaturalRegeneration:0.00}% natural regeneration";

        var densityOrTreesText = restockingOperation.RestockingProposalType == RestockingProposalType.RestockWithIndividualTrees ||
                                 restockingOperation.RestockingProposalType == RestockingProposalType.PlantAnAlternativeAreaWithIndividualTrees
            ? GetTreesText(restockingOperation.NumberOfTrees ?? 0)
            : GetDensityText(restockingOperation.RestockingDensity);

        var regenOp = restockingOperation.RestockingProposalType == RestockingProposalType.RestockWithCoppiceRegrowth
            ? "coppice regrowth"
            : "natural regeneration";
        var lessThan100PercentRegenText = restockingOperations.RestockingOperations.Any(x => x.PercentNaturalRegeneration is < 100)
            ? "If planting alongside " + regenOp + ", the remaining area will be planted with {0} to achieve not less than {1}."
            : string.Empty;

        var updatedText = new List<string>(originalLines.Length);
        foreach (var line in originalLines)
        {
            var nextLine = line
                .Replace(ConditionOptions.SpeciesParameter, GetSpeciesText(restockingOperation))
                .Replace(ConditionOptions.DensityParameter, densityOrTreesText)
                .Replace(ConditionOptions.DensityCoppiceParameter, GetDensityTextForCoppice(restockingOperation.RestockingDensity))
                .Replace(ConditionOptions.CompartmentsParameter, restockingOperations.RestockingCompartmentNames())
                .Replace(ConditionOptions.FellingCompartmentsParameter, restockingOperations.FellingCompartmentNames())
                .Replace(ConditionOptions.RegenerationParameter, regen)
                .Replace(ConditionOptions.LessThan100PercentRegenerationParameter, lessThan100PercentRegenText);

            if (!string.IsNullOrWhiteSpace(nextLine))
            {
                updatedText.Add(nextLine);
            }
        }

        return updatedText.ToArray();
    }

    private static string? ApplyParametersToString(MatchedRestockingOperations restockingOperations, string? originalLine)
    {
        if (string.IsNullOrWhiteSpace(originalLine))
        {
            return originalLine;
        }

        var restockingOperation = restockingOperations.RestockingOperations.First();
        var regen = restockingOperation.RestockingProposalType == RestockingProposalType.RestockWithCoppiceRegrowth
            ? $"{restockingOperation.PercentNaturalRegeneration:0.00}% coppice regrowth"
            : $"{restockingOperation.PercentNaturalRegeneration:0.00}% natural regeneration";

        var densityOrTreesText = restockingOperation.RestockingProposalType == RestockingProposalType.RestockWithIndividualTrees ||
                                 restockingOperation.RestockingProposalType == RestockingProposalType.PlantAnAlternativeAreaWithIndividualTrees
            ? GetTreesText(restockingOperation.NumberOfTrees ?? 0)
            : GetDensityText(restockingOperation.RestockingDensity);

        return originalLine
            .Replace(ConditionOptions.SpeciesParameter, GetSpeciesText(restockingOperation))
            .Replace(ConditionOptions.DensityParameter, densityOrTreesText)
            .Replace(ConditionOptions.DensityCoppiceParameter, GetDensityTextForCoppice(restockingOperation.RestockingDensity))
            .Replace(ConditionOptions.CompartmentsParameter, restockingOperations.RestockingCompartmentNames())
            .Replace(ConditionOptions.FellingCompartmentsParameter, restockingOperations.FellingCompartmentNames())
            .Replace(ConditionOptions.RegenerationParameter, regen);
    }
}