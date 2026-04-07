using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.ConditionsBuilder.Configuration;
using Forestry.Flo.Services.ConditionsBuilder.Entities;
using Forestry.Flo.Services.ConditionsBuilder.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Forestry.Flo.Services.ConditionsBuilder.Services;

/// <summary>
/// Condition builder class that calculates the single condition for a CBW application as a whole. This
/// does not implement the <see cref="IBuildCondition"/> interface as the CBW conditions are not based on
/// individual or matched restocking operations, rather CBW applications as a whole have a single conditions
/// template that is populated with details from the application.
/// </summary>
public class CBWConditionBuilder(
    IOptions<ConditionsBuilderOptions> conditionBuilderOptions,
    ILogger<CBWConditionBuilder> logger) : IBuildCBWCondition
{
    private readonly ConditionOptions _conditionBuilderOptions = Guard.Against.Null(conditionBuilderOptions.Value.CBWOptions);

    /// <inheritdoc/>
    public Result<CalculatedCondition> CalculateCBWCondition(List<RestockingOperationDetails> restockingOperations)
    {
        logger.LogDebug("Calculating CBW condition");
        try
        {
            var condition = new CalculatedCondition
            {
                ConditionsText = ApplyParametersToText(restockingOperations, _conditionBuilderOptions.ConditionText),
                Parameters = _conditionBuilderOptions.ConditionParameters.Select(x => new ConditionParameter
                {
                    Value = ApplyParametersToString(restockingOperations, x.DefaultValue),
                    Description = x.Description,
                    Index = x.Index
                }).ToList(),
                AppliesToSubmittedCompartmentIds = restockingOperations
                    .Select(x => x.RestockingSubmittedFlaPropertyCompartmentId).Distinct().ToList()
            };

            return Result.Success(condition);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception caught in CalculateCBWCondition");
            return Result.Failure<CalculatedCondition>("Error calculating CBW condition: " + ex.Message);
        }
    }

    private static string[] ApplyParametersToText(List<RestockingOperationDetails> restockingOperations, string[] originalLines)
    {
        var updatedText = new List<string>(originalLines.Length);
        foreach (var line in originalLines)
        {
            var updatedLine = line.Replace(
                ConditionOptions.RestockingNumberParameter, 
                GetRestockingNumberText(restockingOperations));
            updatedText.Add(updatedLine);
        }

        return updatedText.ToArray();
    }

    private static string? ApplyParametersToString(
        List<RestockingOperationDetails> restockingOperations,
        string? originalLine)
    {
        if (string.IsNullOrWhiteSpace(originalLine))
        {
            return originalLine;
        }

        return originalLine.Replace(
            ConditionOptions.RestockingNumberParameter,
            GetRestockingNumberText(restockingOperations));
    }

    private static string GetRestockingNumberText(List<RestockingOperationDetails> restockingOperations)
    {
        var restockingCount = restockingOperations
            .Where(x => x.NumberOfTrees.HasValue)
            .Sum(x => x.NumberOfTrees.Value);

        var isAllRestockingIndividualTrees = restockingOperations
            .All(x => x.RestockingProposalType is RestockingProposalType.RestockWithIndividualTrees
                or RestockingProposalType.PlantAnAlternativeAreaWithIndividualTrees);

        return isAllRestockingIndividualTrees
            ? $"with {restockingCount} in total equivalent "
            : string.Empty;
    }
}