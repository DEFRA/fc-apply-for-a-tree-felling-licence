using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Mvc;

namespace Forestry.Flo.Internal.Web.Services.Interfaces;

/// <summary>
/// Defines the contract for running the Constraint Check for an Internal User in the Felling Licence Application.
/// </summary>
public interface IRunFcInternalUserConstraintCheckUseCase
{
    /// <summary>
    /// Executes the constraint check for the specified internal user and application.
    /// </summary>
    /// <param name="user">The internal user requesting the constraint check.</param>
    /// <param name="applicationId">The unique identifier of the felling licence application.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>
    /// A <see cref="Result{T}"/> containing a <see cref="RedirectResult"/> on success, or a failure result if the check could not be executed.
    /// </returns>
    Task<Result<RedirectResult>> ExecuteConstraintsCheckAsync(
        InternalUser user,
        Guid applicationId,
        CancellationToken cancellationToken);
}
