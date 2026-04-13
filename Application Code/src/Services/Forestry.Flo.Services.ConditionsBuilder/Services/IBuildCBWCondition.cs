using CSharpFunctionalExtensions;
using Forestry.Flo.Services.ConditionsBuilder.Models;

namespace Forestry.Flo.Services.ConditionsBuilder.Services;

/// <summary>
/// Defines the contract for a service that builds the specific CBW condition.
/// </summary>
public interface IBuildCBWCondition
{
    /// <summary>
    /// Calculate the condition for a CBW application based on the provided restocking operations. The condition text and parameters
    /// are populated from the configured options and details from the restocking operations.
    /// </summary>
    /// <param name="restockingOperations">The full set of restocking operations in the application.</param>
    /// <returns>A populated <see cref="CalculatedCondition"/> instance representing the CBW condition, or a failure reason.</returns>
    Result<CalculatedCondition> CalculateCBWCondition(List<RestockingOperationDetails> restockingOperations);
}