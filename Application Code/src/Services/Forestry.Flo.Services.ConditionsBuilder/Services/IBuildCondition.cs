using CSharpFunctionalExtensions;
using Forestry.Flo.Services.ConditionsBuilder.Models;

namespace Forestry.Flo.Services.ConditionsBuilder.Services;

/// <summary>
/// Defines the contract for a service that builds a particular condition.
/// </summary>
public interface IBuildCondition
{
    /// <summary>
    /// Checks the felling and restocking details for a particular compartment to see if this
    /// condition applies to the compartment or not.
    /// </summary>
    /// <param name="restockingOperation">A populated <see cref="RestockingOperationDetails"/> model of a specific restocking operation.</param>
    /// <returns>True if this condition applies to the compartment, otherwise false.</returns>
    bool AppliesToOperation(RestockingOperationDetails restockingOperation);

    /// <summary>
    /// Calculates one or more conditions for one or more restocking operations that this condition applies to,
    /// operations that are functionally identical will be combined into one calculated condition.
    /// </summary>
    /// <param name="restockingOperations">A list of <see cref="RestockingOperationDetails"/> that this condition applies to.</param>
    /// <returns>A list of populated <see cref="CalculatedCondition"/> models.</returns>
    /// <remarks>Restocking operations are functionally identical if they have the same felling type, species list, restocking
    /// density, percentage open space, and (for condition C only) coppice or natural regen percentage.</remarks>
    Result<List<CalculatedCondition>> CalculateCondition(List<RestockingOperationDetails> restockingOperations);
}