using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.External.Web.Models.FcUser;
using Forestry.Flo.Services.Applicants.Services;

namespace Forestry.Flo.External.Web.Services.FcUser;

/// <summary>
/// Coordinates the calls to retrieve required data to build the
/// view model necessary to display the FC user homepage. 
/// </summary>
public class GetDataForFcUserHomepageUseCase
{
    private readonly IRetrieveWoodlandOwners _retrieveWoodlandOwnersService;
    private readonly IRetrieveAgencies _retrieveAgenciesService;
    private readonly ILogger<GetDataForFcUserHomepageUseCase> _logger;

    public GetDataForFcUserHomepageUseCase(
        IRetrieveWoodlandOwners retrieveWoodlandOwnersService,
        IRetrieveAgencies retrieveAgenciesService,
        ILogger<GetDataForFcUserHomepageUseCase> logger)
    {
        _retrieveWoodlandOwnersService = Guard.Against.Null(retrieveWoodlandOwnersService);
        _retrieveAgenciesService = Guard.Against.Null(retrieveAgenciesService);
        _logger = logger;
    }

    /// <summary>
    /// Executes the use case.
    /// </summary>
    /// <param name="user">The User requesting the execution of this use case</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns></returns>
    public async Task<Result<FcUserHomePageViewModel>> ExecuteAsync(
        ExternalApplicant user,
        CancellationToken cancellationToken)
    {
        if (user == null) throw new ArgumentNullException(nameof(user));

        var getAllWoodlandOwnersForFcResult = await _retrieveWoodlandOwnersService.GetAllWoodlandOwnersForFcAsync(user.UserAccountId!.Value, cancellationToken);

        if (getAllWoodlandOwnersForFcResult.IsFailure)
        {
            _logger.LogError("Unable to retrieve All woodland owners in system for Fc user Dashboard, error : {error}", getAllWoodlandOwnersForFcResult.Error);
            return Result.Failure<FcUserHomePageViewModel>(getAllWoodlandOwnersForFcResult.Error);
        }

        var getAllAgenciesForFcResult = await _retrieveAgenciesService.GetAllAgenciesForFcAsync(user.UserAccountId.Value, cancellationToken);

        if (getAllAgenciesForFcResult.IsFailure)
        {
            _logger.LogError("Unable to retrieve All agencies in system for Fc user Dashboard, error : {error}", getAllAgenciesForFcResult.Error);
            return Result.Failure<FcUserHomePageViewModel>(getAllAgenciesForFcResult.Error);
        }
        
        var viewModel = new FcUserHomePageViewModel
        {
            AllWoodlandOwnersManagedByFc = getAllWoodlandOwnersForFcResult.Value
                .Where(x=>x.HasActiveUserAccounts == false)
                .ToList()
                .AsReadOnly(), 

            AllAgenciesManagedByFc = getAllAgenciesForFcResult.Value
                .Where(x => x.HasActiveUserAccounts == false)
                .ToList()
                .AsReadOnly(),
            
            AllExternalAgencies = getAllAgenciesForFcResult.Value
                .Where(x => x.HasActiveUserAccounts)
                .ToList()
                .AsReadOnly(),
            
            AllExternalWoodlandOwners = getAllWoodlandOwnersForFcResult.Value
                .Where(x => x.HasActiveUserAccounts)
                .ToList()
                .AsReadOnly()
        };

        return Result.Success(viewModel);
    }
}