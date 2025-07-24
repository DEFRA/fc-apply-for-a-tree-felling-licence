using CSharpFunctionalExtensions;
using Forestry.Flo.Services.AdminHubs.Model;

namespace Forestry.Flo.Services.AdminHubs.Services;

public interface IAdminHubService
{
    /// <summary>
    /// Validates Admin Manager and then retrieves all associated Admin Hub data.
    /// </summary>
    /// <param name="request">The request model.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns></returns>
    Task<Result<IReadOnlyCollection<AdminHubModel>, ManageAdminHubOutcome>> RetrieveAdminHubDataAsync(
        GetAdminHubsDataRequestModel request,
        CancellationToken cancellationToken);

    /// <summary>
    /// Validates and then Adds a user as an Admin Officer to an Admin Hub.
    /// </summary>
    /// <param name="requestModel">Model containing the required data to fulfill the request</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<UnitResult<ManageAdminHubOutcome>> AddAdminOfficerAsync(
        AddAdminOfficerToAdminHubRequestModel requestModel,
        CancellationToken cancellationToken);

    /// <summary>
    /// Validates and then Removes an existing Admin Officer user from an Admin Hub.
    /// </summary>
    /// <param name="requestModel">Model containing the required data to fulfill the request</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<UnitResult<ManageAdminHubOutcome>> RemoveAdminOfficerAsync(
        RemoveAdminOfficerFromAdminHubRequestModel requestModel,
        CancellationToken cancellationToken);

    /// <summary>
    /// Updates an Admin Hub's details
    /// </summary>
    /// <param name="requestModel">Model containing the required data to fulfill the request</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<UnitResult<ManageAdminHubOutcome>> UpdateAdminHubDetailsAsync(
        UpdateAdminHubDetailsRequestModel requestModel,
        CancellationToken cancellationToken
    );
}