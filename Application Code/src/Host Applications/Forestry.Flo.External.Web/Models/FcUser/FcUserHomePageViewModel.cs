using Forestry.Flo.Services.Applicants.Entities;

namespace Forestry.Flo.External.Web.Models.FcUser;

/// <summary>
/// The View model to support the FC User homepage 
/// </summary>
public class FcUserHomePageViewModel
{
    /// <summary>
    /// Gets the list of all applicants in the system that match the current search term, sorted
    /// and paginated according to the specified parameters.
    /// </summary>
    public IReadOnlyList<Applicant> Applicants { get; set; } = new List<Applicant>();

    /// <summary>
    /// Gets the count of all applicants in the system that match the current search term.
    /// </summary>
    public int TotalApplicants { get; set; }

    /// <summary>
    /// Gets the total number of pages of applicants that match the current search term, sorting and paging options.
    /// </summary>
    public int TotalPages => (int)Math.Ceiling((double)TotalApplicants / SearchAndSortModel.PageSize);

    /// <summary>
    /// Gets a flag indicating whether there is a previous page of applicants to navigate back to based on the current
    /// page number in the search and sort model.
    /// </summary>
    public bool HasPreviousPage => SearchAndSortModel.PageNumber > 1;

    /// <summary>
    /// Gets a flag indicating whether there is a next page of applicants to navigate back to based on the current
    /// page number in the search and sort model.
    /// </summary>
    public bool HasNextPage => SearchAndSortModel.PageNumber < TotalPages;

    public FcUserHomePageSearchAndSortModel SearchAndSortModel { get; set; } = new FcUserHomePageSearchAndSortModel();
}

/// <summary>
/// A model class representing the current search and sort parameters to apply when filtering the
/// list of applicants to display on the FC User homepage.
/// </summary>
public class FcUserHomePageSearchAndSortModel
{
    /// <summary>
    /// Gets and sets the current search term to filter applicants by on the homepage.
    /// </summary>
    public string? SearchTerm { get; set; } = null;

    /// <summary>
    /// Gets and sets the current page of data to display on the homepage.
    /// </summary>
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Gets and sets the current size of pages of data to display on the homepage.
    /// </summary>
    public int PageSize { get; set; } = 10;

    /// <summary>
    /// Gets and sets the name of the column to sort applicants by on the homepage.
    /// </summary>
    public string SortColumn { get; set; } = nameof(Applicant.Name);

    /// <summary>
    /// Gets and sets a flag to indicate whether to sort applicants in ascending order on the homepage.
    /// </summary>
    public bool SortAscending { get; set; } = true;
}
