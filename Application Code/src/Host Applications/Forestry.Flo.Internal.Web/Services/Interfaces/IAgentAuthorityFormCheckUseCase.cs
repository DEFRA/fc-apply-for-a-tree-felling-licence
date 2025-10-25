using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Models.AdminOfficerReview;

namespace Forestry.Flo.Internal.Web.Services.Interfaces;

/// <summary>
/// Contract for a use case that handles agent authority form checks for admin officer review of felling licence applications.
/// </summary>
public interface IAgentAuthorityFormCheckUseCase
{
    /// <summary>
    /// Gets a populated <see cref="AgentAuthorityFormCheckModel"/> for admin officers to verify an agent authority form for an application in review.
    /// </summary>
    /// <param name="applicationId">The identifier for the application.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// A <see cref="Result{T}"/> containing a populated <see cref="AgentAuthorityFormCheckModel"/> with details of an application's agent authority form,
    /// or a failure result if retrieval was unsuccessful.
    /// </returns>
    Task<Result<AgentAuthorityFormCheckModel>> GetAgentAuthorityFormCheckModelAsync(Guid applicationId, CancellationToken cancellationToken);

    /// <summary>
    /// Completes the agent authority form check task in the admin officer review.
    /// </summary>
    /// <param name="applicationId">The identifier for the application.</param>
    /// <param name="performingUserId">The identifier for the internal user completing the check.</param>
    /// <param name="isCheckPassed">A flag indicating whether the agent authority form check is successful.</param>
    /// <param name="failureReason">A textual reason why the agent authority form check has failed, if the check is unsuccessful.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// A <see cref="Result"/> indicating whether the agent authority check has been updated successfully.
    /// </returns>
    Task<Result> CompleteAgentAuthorityCheckAsync(
        Guid applicationId,
        Guid performingUserId,
        bool isCheckPassed,
        string? failureReason,
        CancellationToken cancellationToken);
}