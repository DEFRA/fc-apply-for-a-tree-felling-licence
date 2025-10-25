using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Models.WoodlandOfficerReview;

namespace Forestry.Flo.Internal.Web.Services.Interfaces;

/// <summary>
/// Defines the contract for the PW14 use case, handling retrieval and update of PW14 checks for felling licence applications.
/// </summary>
public interface IPw14UseCase
{
    /// <summary>
    /// Retrieves the PW14 check details for a given application.
    /// </summary>
    /// <param name="applicationId">The unique identifier of the felling licence application.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>
    /// A <see cref="Result{T}"/> containing a <see cref="Pw14ChecksViewModel"/> if successful, or an error message if not.
    /// </returns>
    Task<Result<Pw14ChecksViewModel>> GetPw14CheckDetailsAsync(Guid applicationId, CancellationToken cancellationToken);

    /// <summary>
    /// Saves the updated PW14 checks for a given application.
    /// </summary>
    /// <param name="model">A populated <see cref="Pw14ChecksViewModel"/> containing the new values.</param>
    /// <param name="user">The internal user performing the update.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>
    /// A <see cref="Result"/> indicating the success or failure of the operation.
    /// </returns>
    Task<Result> SavePw14ChecksAsync(Pw14ChecksViewModel model, InternalUser user, CancellationToken cancellationToken);
}