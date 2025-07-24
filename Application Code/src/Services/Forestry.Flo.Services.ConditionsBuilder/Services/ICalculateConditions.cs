using CSharpFunctionalExtensions;
using Forestry.Flo.Services.ConditionsBuilder.Models;

namespace Forestry.Flo.Services.ConditionsBuilder.Services;

/// <summary>
/// Defines the contract for a service that will calculate conditions for a felling licence application.
/// </summary>
public interface ICalculateConditions
{
    /// <summary>
    /// Calculates the conditions for the given details of a felling licence application.
    /// </summary>
    /// <param name="request">A populated <see cref="CalculateConditionsRequest"/> request model.</param>
    /// <param name="performingUserId">The id of the performing user.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A populated <see cref="ConditionsResponse"/> with the calculated conditions,
    /// or <see cref="Result.Failure"/> with a populated error message.</returns>
    Task<Result<ConditionsResponse>> CalculateConditionsAsync(
        CalculateConditionsRequest request,
        Guid performingUserId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Stores a set of conditions for a felling licence application.
    /// </summary>
    /// <param name="request">A populated <see cref="StoreConditionsRequest"/> request model.</param>
    /// <param name="performingUserId">The id of the performing user.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Result"/> struct representing the success or failure of the operation.</returns>
    Task<Result> StoreConditionsAsync(
        StoreConditionsRequest request,
        Guid performingUserId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves any existing conditions in the system for a felling licence application.
    /// </summary>
    /// <param name="applicationId">The id of the application to retrieve the existing conditions for.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A populated <see cref="ConditionsResponse"/> with the existing conditions.</returns>
    Task<ConditionsResponse> RetrieveExistingConditionsAsync(
        Guid applicationId,
        CancellationToken cancellationToken);
}