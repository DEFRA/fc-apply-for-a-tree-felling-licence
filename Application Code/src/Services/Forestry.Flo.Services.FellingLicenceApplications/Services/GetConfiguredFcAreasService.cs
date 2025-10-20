using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.AdminHubs.Model;
using Forestry.Flo.Services.AdminHubs.Services;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Microsoft.Extensions.Logging;

namespace Forestry.Flo.Services.FellingLicenceApplications.Services;

/// <summary>
/// An implementation of a <see cref="IGetConfiguredFcAreas"/> contract.
/// This retrieves data from the Admin Hub repository along with static
/// data used to obtain CostCodes for each area assigned to an Admin Hub.
/// </summary>
public class GetConfiguredFcAreasService : IGetConfiguredFcAreas
{
    private readonly IAdminHubService _adminHubService;
    private readonly ILogger<GetConfiguredFcAreasService> _logger;

    public GetConfiguredFcAreasService(
        IAdminHubService adminHubService,
        ILogger<GetConfiguredFcAreasService> logger)
    {
        _adminHubService = Guard.Against.Null(adminHubService);
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<List<ConfiguredFcArea>>> GetAllAsync(
        CancellationToken cancellationToken)
    {
        var getAdminHubsResult = await _adminHubService.RetrieveAdminHubDataAsync(
            GetAdminHubsDataRequestModel.CreateSystemRequest,
            cancellationToken);

        if (getAdminHubsResult.IsFailure)
        {
            _logger.LogError("Unable to successfully query admin hub data in database, error  was {error}",
                getAdminHubsResult.Error);

            return Result.Failure<List<ConfiguredFcArea>>("Could not retrieve Admin Hubs from database");
        }

        List<ConfiguredFcArea> configuredFcAreasList = new();

        foreach (var adminHubModel in getAdminHubsResult.Value)
        {
            configuredFcAreasList.AddRange(from areaModel in adminHubModel.Areas
                select new ConfiguredFcArea(areaModel, areaModel.Code, adminHubModel.Name)
            );
        }

        _logger.LogDebug("FC areas configured in system : {FcAreasCount}", configuredFcAreasList.Count);

        return Result.Success(configuredFcAreasList.OrderBy(x => x.AreaCostCode).ToList());
    }

    /// <inheritdoc />
    public async Task<string> TryGetAdminHubAddress(string adminHubName, CancellationToken cancellationToken)
    {
        var getAdminHubsResult = await _adminHubService.RetrieveAdminHubDataAsync(
            GetAdminHubsDataRequestModel.CreateSystemRequest,
            cancellationToken);

        if (getAdminHubsResult.IsFailure)
        {
            _logger.LogError("Unable to successfully query admin hub data in database, error  was {error}",
                getAdminHubsResult.Error);

            return adminHubName;
        }

        var matchedHub = getAdminHubsResult.Value
            .FirstOrDefault(hub => string.Equals(hub.Name, adminHubName, StringComparison.OrdinalIgnoreCase));

        return matchedHub?.Address ?? adminHubName;
    }
}
