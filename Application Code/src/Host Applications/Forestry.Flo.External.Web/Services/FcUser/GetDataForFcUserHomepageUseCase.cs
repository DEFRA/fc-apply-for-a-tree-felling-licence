using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.External.Web.Models.FcUser;
using Forestry.Flo.Services.Applicants.Repositories;
using Forestry.Flo.Services.Applicants.Services;

namespace Forestry.Flo.External.Web.Services.FcUser;

/// <summary>
/// Coordinates the calls to retrieve required data to build the
/// view model necessary to display the FC user homepage. 
/// </summary>
public class GetDataForFcUserHomepageUseCase(
    IApplicantRepository applicantRepository,
    ILogger<GetDataForFcUserHomepageUseCase> logger)
{
    private readonly IApplicantRepository _applicantRepository = Guard.Against.Null(applicantRepository);
    private readonly ILogger<GetDataForFcUserHomepageUseCase> _logger = logger;

    /// <summary>
    /// Executes the use case.
    /// </summary>
    /// <param name="user">The User requesting the execution of this use case</param>
    /// <param name="searchAndSortModel">A model of the searching, sorting and paging parameters to retrieve applicants with.</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A view model for the FC user home page.</returns>
    public async Task<Result<FcUserHomePageViewModel>> ExecuteAsync(
        ExternalApplicant user,
        FcUserHomePageSearchAndSortModel searchAndSortModel,
        CancellationToken cancellationToken)
    {
        if (user == null) throw new ArgumentNullException(nameof(user));

        if (!user.IsFcUser)
        {
            _logger.LogError("Current user {UserId} is not an FC User in GetDataForFcUserHomepageUseCase", user.UserAccountId!.Value);
            return Result.Failure<FcUserHomePageViewModel>("Current user does not have permission to retrieve all applicants");
        }

        try
        {
            var count = await _applicantRepository.GetApplicantsCountAsync(searchAndSortModel.SearchTerm, cancellationToken);
            _logger.LogDebug("{Count} applicants found for search term {SearchTerm}", count, searchAndSortModel.SearchTerm);

            var results = _applicantRepository.GetApplicants(
                searchAndSortModel.SearchTerm,
                searchAndSortModel.SortColumn,
                searchAndSortModel.SortAscending,
                searchAndSortModel.PageNumber,
                searchAndSortModel.PageSize);
            
            _logger.LogDebug("Successfully retrieved applicants for search term {SearchTerm}, sorting by {SortColumn} {SortAscending}, page {PageNumber} with page size {PageSize}",
                searchAndSortModel.SearchTerm,
                searchAndSortModel.SortColumn,
                searchAndSortModel.SortAscending ? "ascending" : "descending",
                searchAndSortModel.PageNumber,
                searchAndSortModel.PageSize);

            var result = new FcUserHomePageViewModel()
            {
                Applicants = results.ToList().AsReadOnly(),
                TotalApplicants = count,
                SearchAndSortModel = searchAndSortModel
            };

            return Result.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception caught in GetDataForFcUserHomepageUseCase");
            return Result.Failure<FcUserHomePageViewModel>("Failed to retrieve applicants");
        }
    }
}