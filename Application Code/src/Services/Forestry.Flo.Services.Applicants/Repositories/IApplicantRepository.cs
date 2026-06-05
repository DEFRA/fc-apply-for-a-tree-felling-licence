using Forestry.Flo.Services.Applicants.Entities;

namespace Forestry.Flo.Services.Applicants.Repositories;

/// <summary>
/// Interface defining the contract for a repository that retrieves Applicant data from the underlying database.
/// </summary>
public interface IApplicantRepository
{
    /// <summary>
    /// Gets the count of applicants matching the provided search term.
    /// </summary>
    /// <param name="searchTerm">A search term to return a count of applicants that match.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns></returns>
    Task<int> GetApplicantsCountAsync(string? searchTerm, CancellationToken cancellationToken);

    /// <summary>
    /// Gets a list of applicants matching the provided search term, sorted and paginated according to the specified parameters.
    /// </summary>
    /// <param name="searchTerm">A search term to return a list of applicants that match.</param>
    /// <param name="sortColumn">The name of the column to sort by.</param>
    /// <param name="sortAscending">A flag to indicate sorting is in ascending order.</param>
    /// <param name="pageNumber">The number of the page of results to return.</param>
    /// <param name="pageSize">The size of each page of results.</param>
    /// <returns>A list of applicants matching the search term and sorted/paged as per the input parameters.</returns>
    IEnumerable<Applicant> GetApplicants(
        string? searchTerm, 
        string sortColumn, 
        bool sortAscending, 
        int pageNumber, 
        int pageSize);
}