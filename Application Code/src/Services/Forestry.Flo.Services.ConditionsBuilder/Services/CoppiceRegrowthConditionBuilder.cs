using Ardalis.GuardClauses;
using Forestry.Flo.Services.ConditionsBuilder.Configuration;
using Forestry.Flo.Services.ConditionsBuilder.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Forestry.Flo.Services.ConditionsBuilder.Services;

/// <summary>
/// Implementation of <see cref="IBuildCondition"/> that calculates the condition for restock the felled
/// area by coppice regrowth.
/// </summary>
public class CoppiceRegrowthConditionBuilder : ConditionBuilderBase, IBuildCondition
{
    private RestockingProposalType[] _validProposalTypes =
    {
        RestockingProposalType.RestockWithCoppiceRegrowth
    };

    /// <summary>
    /// Creates a new instance of <see cref="CoppiceRegrowthConditionBuilder"/>.
    /// </summary>
    /// <param name="conditionBuilderOptions">The condition builder service options.</param>
    /// <param name="logger">A logging implementation.</param>
    public CoppiceRegrowthConditionBuilder(
        IOptions<ConditionsBuilderOptions> conditionBuilderOptions,
        ILogger<CoppiceRegrowthConditionBuilder> logger)
        : base(Guard.Against.Null(conditionBuilderOptions.Value.CoppiceRegrowthOptions), logger)
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