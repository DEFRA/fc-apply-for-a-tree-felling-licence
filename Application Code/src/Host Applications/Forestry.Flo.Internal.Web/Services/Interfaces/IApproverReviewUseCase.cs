using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Microsoft.EntityFrameworkCore.Storage;

namespace Forestry.Flo.Internal.Web.Services.Interfaces;

/// <summary>
/// Defines use case operations for handling approver reviews of felling licence applications.
/// </summary>
public interface IApproverReviewUseCase
{
    /// <summary>
    /// Retrieves the approver review summary model for a specific felling licence application.
    /// </summary>
    /// <param name="applicationId">The ID of the application to review.</param>
    /// <param name="viewingUser">The internal user viewing the summary.</param>
    /// <param name="cancellationToken">A cancellation token for the async operation.</param>
    /// <returns>
    /// A <see cref="Maybe{ApproverReviewSummaryModel}"/> containing the review summary model if found.
    /// </returns>
    Task<Maybe<ApproverReviewSummaryModel>> RetrieveApproverReviewAsync(
        Guid applicationId,
        InternalUser viewingUser,
        CancellationToken cancellationToken);

    /// <summary>
    /// Saves the approver review for a given application.
    /// </summary>
    /// <param name="model">The approver review model.</param>
    /// <param name="user">The internal user performing the operation.</param>
    /// <param name="cancellationToken">A cancellation token for the async operation.</param>
    /// <returns>
    /// A <see cref="Result"/> indicating the outcome of the save operation.
    /// </returns>
    Task<Result> SaveApproverReviewAsync(
        ApproverReviewModel model,
        InternalUser user,
        CancellationToken cancellationToken);

    /// <summary>
    /// Begins a transaction for the current context.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Task{IDbContextTransaction}"/> representing the transaction.</returns>
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken);
}