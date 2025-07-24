using Ardalis.GuardClauses;
using Forestry.Flo.Services.ConditionsBuilder.Configuration;
using Forestry.Flo.Services.ConditionsBuilder.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Forestry.Flo.Services.ConditionsBuilder.Services;

/// <summary>
/// Implementation of <see cref="IBuildCondition"/> that calculates the condition for restock the felled
/// or alternative area by planting.
/// </summary>
public class RestockByPlantingConditionBuilder : ConditionBuilderBase, IBuildCondition
{
    private RestockingProposalType[] _validProposalTypes =
    {
        RestockingProposalType.ReplantTheFelledArea,
        RestockingProposalType.RestockWithIndividualTrees,
        RestockingProposalType.PlantAnAlternativeArea,
        RestockingProposalType.PlantAnAlternativeAreaWithIndividualTrees
    };

    /// <summary>
    /// Creates a new instance of <see cref="RestockByPlantingConditionBuilder"/>.
    /// </summary>
    /// <param name="conditionBuilderOptions">The condition builder service options.</param>
    /// <param name="logger">A logging implementation.</param>
    public RestockByPlantingConditionBuilder(
        IOptions<ConditionsBuilderOptions> conditionBuilderOptions, 
        ILogger<RestockByPlantingConditionBuilder> logger) 
        : base(Guard.Against.Null(conditionBuilderOptions.Value.ReplantingOptions), logger)
    {
    }

    /// <inheritdoc />
    public override bool AppliesToOperation(RestockingOperationDetails restockingOperation)
    {
        return _validProposalTypes.Contains(restockingOperation.RestockingProposalType);
    }

    /// <inheritdoc />
    protected override bool MustMatchOnNaturalRegenPercentage => false;
}