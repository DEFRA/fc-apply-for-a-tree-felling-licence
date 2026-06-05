using Ardalis.GuardClauses;
using Forestry.Flo.Services.ConditionsBuilder.Configuration;
using Forestry.Flo.Services.ConditionsBuilder.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Forestry.Flo.Services.ConditionsBuilder.Services;

/// <summary>
/// Implementation of <see cref="IBuildCondition"/> that calculates the condition for create
/// designed open ground.
/// </summary>
public class CreateDesignedOpenGroundConditionBuilder : ConditionBuilderBase, IBuildCondition
{
    private RestockingProposalType[] _validProposalTypes =
    [
        RestockingProposalType.CreateDesignedOpenGround
    ];

    /// <summary>
    /// Creates a new instance of <see cref="RestockByPlantingConditionBuilder"/>.
    /// </summary>
    /// <param name="conditionBuilderOptions">The condition builder service options.</param>
    /// <param name="logger">A logging implementation.</param>
    public CreateDesignedOpenGroundConditionBuilder(
        IOptions<ConditionsBuilderOptions> conditionBuilderOptions,
        ILogger<CreateDesignedOpenGroundConditionBuilder> logger)
        : base(Guard.Against.Null(conditionBuilderOptions.Value.CreateDesignedOpenGroundOptions), logger)
    {
    }

    /// <inheritdoc />
    public override bool AppliesToOperation(RestockingOperationDetails restockingOperation)
    {
        return _validProposalTypes.Contains(restockingOperation.RestockingProposalType);
    }

    /// <inheritdoc />
    protected override bool MustMatchOnNaturalRegenPercentage => false;

    /// <inheritdoc />
    protected override bool MustMatchOnRestockArea => false;
}