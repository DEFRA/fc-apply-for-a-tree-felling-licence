using CSharpFunctionalExtensions;

namespace Forestry.Flo.Internal.Web.Services.Interfaces;

/// <summary>
/// Defines the contract for reverting a felling license application from the withdrawn state.
/// </summary>
public interface IRevertApplicationFromWithdrawnUseCase
{
    /// <summary>
    /// Reverts a felling license application from the withdrawn state.
    /// </summary>
    /// <param name="user">The internal user performing the operation.</param>
    /// <param name="applicationId">The unique identifier of the application to be reverted.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// A <see cref="Result"/> indicating the success or failure of the operation.
    /// </returns>
    Task<Result> RevertApplicationFromWithdrawnAsync(
        InternalUser user,
        Guid applicationId,
        CancellationToken cancellationToken);
}
