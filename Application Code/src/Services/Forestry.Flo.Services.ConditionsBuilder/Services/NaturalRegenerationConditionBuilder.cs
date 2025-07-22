using Ardalis.GuardClauses;
using Forestry.Flo.Services.ConditionsBuilder.Configuration;
using Forestry.Flo.Services.ConditionsBuilder.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Forestry.Flo.Services.ConditionsBuilder.Services;

/// <summary>
/// Implementation of <see cref="IBuildCondition"/> that calculates the condition for restock the felled
/// area by natural regeneration.
/// </summary>
public class NaturalRegenerationConditionBuilder : ConditionBuilderBase, IBuildCondition
{
    private RestockingProposalType[] _validProposalTypes =
    {
        RestockingProposalType.RestockByNaturalRegeneration
    };

    /// <summary>
    /// Creates a new instance of <see cref="NaturalRegenerationConditionBuilder"/>.
    /// </summary>
    /// <param name="conditionBuilderOptions">The condition builder service options.</param>
    /// <param name="logger">A logging implementation.</param>
    public NaturalRegenerationConditionBuilder(
        IOptions<ConditionsBuilderOptions> conditionBuilderOptions,
        ILogger<NaturalRegenerationConditionBuilder> logger)
        : base(Guard.Against.Null(conditionBuilderOptions.Value.NaturalRegenOptions), logger)
    {
        
    }

    /// <inheritdoc />
    public override bool AppliesToOperation(RestockingOperationDetails restockingOperation)
    {
        return _validProposalTypes.Contains(restockingOperation.RestockingProposalType);
    }

    /// <inheritdoc />
    protected override bool MustMatchOnNaturalRegenPercentage => true;
}