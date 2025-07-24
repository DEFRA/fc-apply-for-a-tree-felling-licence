using Ardalis.GuardClauses;
using Forestry.Flo.Services.AdminHubs.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.AdminHubs.Model;
using Forestry.Flo.Services.Common.User;

namespace Forestry.Flo.Services.AdminHubs.Services;

/// <summary>
/// Service class implementation of a <see cref="IAdminHubService"/>
/// </summary>
public class AdminHubService : IAdminHubService
{
    private readonly IAdminHubRepository _adminHubRepository;
    private readonly ILogger<AdminHubService> _logger;

    public AdminHubService(
        IAdminHubRepository adminHubRepository,
        ILogger<AdminHubService> logger)
    {
        _adminHubRepository = Guard.Against.Null(adminHubRepository);
        _logger = logger ?? new NullLogger<AdminHubService>();
    }

    ///<inheritdoc />
    public async Task<Result<IReadOnlyCollection<AdminHubModel>, ManageAdminHubOutcome>> RetrieveAdminHubDataAsync(
        GetAdminHubsDataRequestModel request,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Request received to retrieve all Admin Hubs data");

        if (AssertCurrentUserIsAdminHubManager(request.PerformingUserAccountType) == false)
        {
            _logger.LogError("User without Admin Hub Manager Access attempted to manage an Admin Hub");
            return Result.Failure<IReadOnlyCollection<AdminHubModel>, ManageAdminHubOutcome>(ManageAdminHubOutcome.Unauthorized);
        }

        var result = await _adminHubRepository.GetAllAsync(cancellationToken);
        if (result.IsFailure)
        {
            _logger.LogError("Could not load admin hubs");
            return Result.Failure<IReadOnlyCollection<AdminHubModel>, ManageAdminHubOutcome>(ManageAdminHubOutcome.AdminHubsNotFound);
        }

        var models = ModelMapping.ToAdminHubModels(result.Value);

        _logger.LogDebug("Successfully retrieved all data of Admin Hubs");
        return Result.Success<IReadOnlyCollection<AdminHubModel>, ManageAdminHubOutcome>(models);
    }

    ///<inheritdoc />
    public async Task<UnitResult<ManageAdminHubOutcome>> AddAdminOfficerAsync(
        AddAdminOfficerToAdminHubRequestModel requestModel,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Request received to add Admin Officer with Id of {AdminOfficerId} to Admin hub with Id {AdminHubId}",
            requestModel.UserId, requestModel.AdminHubId);

        var validAdminHub = await CheckValidAdminHubAsync(
            requestModel.AdminHubId, requestModel.PerformingUserId, requestModel.PerformingUserAccountType, cancellationToken);
        if (validAdminHub.IsFailure)
        {
            return UnitResult.Failure(validAdminHub.Error);
        }

        if (validAdminHub.Value.Any(x => x.AdminOfficers.Any(y => y.UserAccountId == requestModel.UserId)))
        {
            _logger.LogError("Officer with id {SelectedOfficerId} is already assigned to an admin hub", requestModel.UserId);
            return UnitResult.Failure(ManageAdminHubOutcome.InvalidAssignment);
        }

        var result = await _adminHubRepository.AddAdminOfficerAsync(requestModel.AdminHubId, requestModel.UserId, cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogError("Unable to add a user having Id of {id} to Admin hub with id of {adminHubId}.",
                requestModel.UserId, requestModel.AdminHubId);
            return UnitResult.Failure(ManageAdminHubOutcome.UpdateFailure);
        }

        return UnitResult.Success<ManageAdminHubOutcome>();
    }

    ///<inheritdoc />
    public async Task<UnitResult<ManageAdminHubOutcome>> RemoveAdminOfficerAsync(
        RemoveAdminOfficerFromAdminHubRequestModel requestModel,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Request received to remove Admin Officer with Id of {AdminOfficerUserId} from Admin hub having Id {AdminHubId}.",
            requestModel.UserId, requestModel.AdminHubId);

        var validAdminHub = await CheckValidAdminHubAsync(requestModel.AdminHubId, requestModel.PerformingUserId, requestModel.PerformingUserAccountType, cancellationToken);
        if (validAdminHub.IsFailure)
        {
            return UnitResult.Failure(validAdminHub.Error);
        }

        if (!validAdminHub.Value.Any(x => x.Id == requestModel.AdminHubId && x.AdminOfficers.Any(y => y.UserAccountId == requestModel.UserId)))
        {
            _logger.LogError("Officer with id {SelectedOfficerId} is not assigned to the given admin hub", requestModel.UserId);
            return UnitResult.Failure(ManageAdminHubOutcome.InvalidAssignment);
        }

        var result = await _adminHubRepository.RemoveAdminOfficerAsync(requestModel.AdminHubId, requestModel.UserId, cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogError("Unable to remove Admin Officer user having Id of {AdminOfficerId} from Admin hub with id of {AdminHubId}.",
                requestModel.UserId, requestModel.AdminHubId);

            return UnitResult.Failure(ManageAdminHubOutcome.UpdateFailure);
        }

        return UnitResult.Success<ManageAdminHubOutcome>();
    }

    ///<inheritdoc />
    public async Task<UnitResult<ManageAdminHubOutcome>> UpdateAdminHubDetailsAsync(
        UpdateAdminHubDetailsRequestModel requestModel,
        CancellationToken cancellationToken
    )
    {
        _logger.LogDebug("Request received to update Admin hub with Id {AdminHubId}.", requestModel.AdminHubId);

        var validAdminHub = await CheckValidAdminHubAsync(requestModel.AdminHubId, requestModel.PerformingUserId, requestModel.PerformingUserAccountType, cancellationToken);
        if (validAdminHub.IsFailure)
        {
            return UnitResult.Failure(validAdminHub.Error);
        }

        var checkAdminHubs = validAdminHub.Value.First(x=>x.Id == requestModel.AdminHubId);
        if (checkAdminHubs.AdminManagerUserAccountId == requestModel.UserId
            && checkAdminHubs.Name == requestModel.NewAdminHubName
            && checkAdminHubs.Address == requestModel.NewAdminHubAddress)
        {
            _logger.LogError("No changes given for admin hub with id {AdminHubId}", requestModel.AdminHubId);
            return UnitResult.Failure(ManageAdminHubOutcome.NoChangeSubmitted);
        }

        var result = await _adminHubRepository.UpdateAdminHubDetailsAsync(
            requestModel.AdminHubId,
            requestModel.UserId, 
            requestModel.NewAdminHubName,
            requestModel.NewAdminHubAddress,
            cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogError("Unable to update Admin Hub with id of {AdminHubId}.", requestModel.AdminHubId);
            return UnitResult.Failure(ManageAdminHubOutcome.UpdateFailure);
        }

        return UnitResult.Success<ManageAdminHubOutcome>();
    }

    private async Task<Result<IReadOnlyCollection<AdminHubModel>, ManageAdminHubOutcome>> CheckValidAdminHubAsync(
        Guid adminHubId,
        Guid managerId,
        AccountTypeInternal accountType,
        CancellationToken cancellationToken)
    {
        if (AssertCurrentUserIsAdminHubManager(accountType) == false)
        {
            _logger.LogError("User without Admin Hub Manager Access attempted to manage an Admin Hub");
            return Result.Failure<IReadOnlyCollection<AdminHubModel>, ManageAdminHubOutcome>(ManageAdminHubOutcome.Unauthorized);
        }

        var result = await _adminHubRepository.GetAllAsync(cancellationToken);
        if (result.IsFailure)
        {
            _logger.LogError("Could not load admin hubs");
            return Result.Failure<IReadOnlyCollection<AdminHubModel>, ManageAdminHubOutcome>(ManageAdminHubOutcome.AdminHubsNotFound);
        }

        var adminHubModels = ModelMapping.ToAdminHubModels(result.Value);

        if (adminHubModels.All(x => x.Id != adminHubId))
        {
            _logger.LogError("Could not locate admin hub with id {AdminHubId}", adminHubId);
            return Result.Failure<IReadOnlyCollection<AdminHubModel>, ManageAdminHubOutcome>(ManageAdminHubOutcome.AdminHubNotFound);
        }

        if (adminHubModels.Any(x => x.Id == adminHubId && x.AdminManagerUserAccountId == managerId))
        {
            return Result.Success<IReadOnlyCollection<AdminHubModel>, ManageAdminHubOutcome>(adminHubModels);
        }
        
        _logger.LogError("Admin Hub Manager attempted to manage an Admin Hub they do not have access to manage");
        return Result.Failure<IReadOnlyCollection<AdminHubModel>, ManageAdminHubOutcome>(ManageAdminHubOutcome.Unauthorized);
    }

    private bool AssertCurrentUserIsAdminHubManager(AccountTypeInternal currentUserType)
    {
        return currentUserType == AccountTypeInternal.AdminHubManager;
    }
    
}