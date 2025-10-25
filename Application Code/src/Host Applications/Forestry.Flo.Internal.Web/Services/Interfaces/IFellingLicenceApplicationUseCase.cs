using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;

namespace Forestry.Flo.Internal.Web.Services.Interfaces;

/// <summary>
/// Defines use case operations for felling licence applications, including assignment listing,
/// review summary retrieval, and reopening withdrawn applications.
/// </summary>
public interface IFellingLicenceApplicationUseCase
{
    /// <summary>
    /// Retrieves a paginated and optionally filtered list of felling licence application assignments.
    /// </summary>
    /// <param name="assignedToUserAccountIdOnly">If true, only applications assigned to the specified user account are included.</param>
    /// <param name="assignedToUserAccountId">The user account ID to filter assignments by.</param>
    /// <param name="includeFellingLicenceStatuses">A list of statuses to include in the results.</param>
    /// <param name="cancellationToken">A cancellation token for the async operation.</param>
    /// <param name="pageNumber">The page number for pagination.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="sortColumn">The column to sort by.</param>
    /// <param name="sortDirection">The direction to sort ("asc" or "desc").</param>
    /// <param name="searchText">Optional search text to filter results.</param>
    /// <returns>
    /// A <see cref="Result{FellingLicenceApplicationAssignmentListModel}"/> containing the assignment list model if successful.
    /// </returns>
    Task<Result<FellingLicenceApplicationAssignmentListModel>> GetFellingLicenceApplicationAssignmentListModelAsync(
        bool assignedToUserAccountIdOnly,
        Guid assignedToUserAccountId,
        IList<FellingLicenceStatus> includeFellingLicenceStatuses,
        CancellationToken cancellationToken,
        int pageNumber,
        int pageSize,
        string sortColumn,
        string sortDirection,
        string? searchText);

    /// <summary>
    /// Retrieves a summary model for reviewing a specific felling licence application.
    /// </summary>
    /// <param name="applicationId">The ID of the application to review.</param>
    /// <param name="viewingUser">The internal user viewing the summary.</param>
    /// <param name="cancellationToken">A cancellation token for the async operation.</param>
    /// <returns>
    /// A <see cref="Maybe{FellingLicenceApplicationReviewSummaryModel}"/> containing the review summary model if found.
    /// </returns>
    Task<Maybe<FellingLicenceApplicationReviewSummaryModel>> RetrieveFellingLicenceApplicationReviewSummaryAsync(
        Guid applicationId,
        InternalUser viewingUser,
        CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves the model required to reopen a withdrawn felling licence application.
    /// </summary>
    /// <param name="applicationId">The ID of the withdrawn application to reopen.</param>
    /// <param name="hostingPage">The name of the hosting page for the operation.</param>
    /// <param name="cancellationToken">A cancellation token for the async operation.</param>
    /// <returns>
    /// A <see cref="Result{ReopenWithdrawnApplicationModel}"/> containing the model for reopening the application if successful.
    /// </returns>
    Task<Result<ReopenWithdrawnApplicationModel>> RetrieveReopenWithdrawnApplicationModelAsync(
        Guid applicationId,
        string hostingPage,
        CancellationToken cancellationToken);
}
