using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Applicants.Entities.Agent;
using Forestry.Flo.Services.Applicants.Entities.WoodlandOwner;
using Forestry.Flo.Services.Applicants.Models;
using Forestry.Flo.Services.Applicants.Repositories;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.Common.User;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Forestry.Flo.Services.Applicants.Services;

/// <summary>
/// Implementation of <see cref="IWoodlandOwnerCreationService"/> that uses an <see cref="IWoodlandOwnerRepository"/>
/// to interact with the database.
/// </summary>
public class RetrieveWoodlandOwnersService : IRetrieveWoodlandOwners
{
    private readonly IWoodlandOwnerRepository _woodlandOwnerRepository;
    private readonly IUserAccountRepository _userAccountRepository;
    private readonly IAgencyRepository _agencyRepository;
    private readonly ILogger<RetrieveWoodlandOwnersService> _logger;

    public RetrieveWoodlandOwnersService(
        IWoodlandOwnerRepository woodlandOwnerRepository,
        IUserAccountRepository userAccountRepository,
        IAgencyRepository agencyRepository,
        ILogger<RetrieveWoodlandOwnersService> logger)
    {
        ArgumentNullException.ThrowIfNull(woodlandOwnerRepository);
        ArgumentNullException.ThrowIfNull(userAccountRepository);
        ArgumentNullException.ThrowIfNull(agencyRepository);
        
        _woodlandOwnerRepository = woodlandOwnerRepository;
        _userAccountRepository = userAccountRepository;
        _agencyRepository = agencyRepository;
        _logger = logger ?? new NullLogger<RetrieveWoodlandOwnersService>();
    }

    /// <inheritdoc />
    public async Task<Result<WoodlandOwnerModel>> RetrieveWoodlandOwnerByIdAsync(
        Guid id,
        UserAccessModel userAccessModel,
        CancellationToken cancellationToken)
    {
        if (userAccessModel.CanManageWoodlandOwner(id) == false)
        {
            _logger.LogWarning("User does not have permission to access woodland owner with id {WoodlandOwnerId}", id);
            return Result.Failure<WoodlandOwnerModel>("User cannot access this woodland owner");
        }

        var (_, isFailure, woodlandOwner) = await _woodlandOwnerRepository.GetAsync(id, cancellationToken);

        if (isFailure)
        {
            return Result.Failure<WoodlandOwnerModel>($"Unable to retrieve woodland owner with id {id}");
        }

        var woodlandOwnerModel = new WoodlandOwnerModel
        {
            ContactAddress = woodlandOwner.ContactAddress,
            ContactEmail = woodlandOwner.ContactEmail,
            ContactName = woodlandOwner.ContactName,
            ContactTelephone = woodlandOwner.ContactTelephone,
            Id = woodlandOwner.Id,
            IsOrganisation = woodlandOwner.IsOrganisation,
            OrganisationAddress = woodlandOwner.OrganisationAddress,
            OrganisationName = woodlandOwner.OrganisationName
        };

        return Result.Success(woodlandOwnerModel);

    }

    /// <inheritdoc />
    public async Task<Result<List<WoodlandOwnerFcModel>>> GetAllWoodlandOwnersForFcAsync(Guid performingUserId, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Received request for all woodland owners in the system for the FC dashboard");

        var user = await _userAccountRepository
            .GetAsync(performingUserId, cancellationToken)
            .ConfigureAwait(false);
        if (user.IsFailure)
        {
            _logger.LogWarning("Could not retrieve user with id {PerformingUserId}", performingUserId);
            return Result.Failure<List<WoodlandOwnerFcModel>>("Could not retrieve user to assert permissions");
        }

        if (user.Value.AccountType != AccountTypeExternal.FcUser)
        {
            _logger.LogWarning("User with id {PerformingUserId} has account type {AccountType} and should not be retrieving this data",
                performingUserId, user.Value.AccountType);
            return Result.Failure<List<WoodlandOwnerFcModel>>("User does not have permissions to retrieve all woodland owners");
        }

        var result = new List<WoodlandOwnerFcModel>();

        // get woodland owners linked directly to an external applicant account
        var applicantOwnerWoodlandOwners = await _woodlandOwnerRepository
            .GetWoodlandOwnersForActiveAccountsAsync(cancellationToken)
            .ConfigureAwait(false);
        _logger.LogDebug("Retrieved {Count} woodland owners linked directly to an active external applicant account", 
            applicantOwnerWoodlandOwners.Count);

        foreach (var woodlandOwner in applicantOwnerWoodlandOwners)
        {
            result.Add(ToWoodlandOwnerFcModel(woodlandOwner));
        }

        // get woodland owners managed by an agency
        var agentAuthorities = await _agencyRepository
            .GetActiveAgentAuthoritiesAsync(cancellationToken)
            .ConfigureAwait(false);
        _logger.LogDebug("Retrieved {Count} active agent authority linked woodland owners", agentAuthorities.Count);

        // get agencies linked to an external applicant account - to check if the WO's with AA's are external applicant agents or FC managed agency
        var externalApplicantAgencies = await _agencyRepository
            .GetAgenciesForActiveAccountsAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var agentAuthority in agentAuthorities)
        {
            var hasActiveUserAccounts = externalApplicantAgencies.Any(x => x.Id == agentAuthority.Agency.Id);

            result.Add(ToWoodlandOwnerFcModel(agentAuthority.WoodlandOwner, hasActiveUserAccounts, agentAuthority.Agency));
        }

        // finally get all the remaining woodland owners that are non-agency and have no applicant account therefore FC managed
        var woodlandOwnerIds = result.Select(x => x.Id).ToList();
        var fcManagedOwners = await _woodlandOwnerRepository
            .GetWoodlandOwnersWithIdNotIn(woodlandOwnerIds, cancellationToken)
            .ConfigureAwait(false);
        _logger.LogDebug("Retrieved {Count} woodland owners not linked to an external applicant or an agent authority", 
            fcManagedOwners.Count);

        foreach (var fcManagedOwner in fcManagedOwners)
        {
            result.Add(ToWoodlandOwnerFcModel(fcManagedOwner, false));
        }

        return Result.Success(result);


        WoodlandOwnerFcModel ToWoodlandOwnerFcModel(
            WoodlandOwner woodlandOwner, 
            bool hasActiveUserAccounts = true, 
            Agency? agency = null)
        {
            return new WoodlandOwnerFcModel
            {
                Id = woodlandOwner.Id,
                IsOrganisation = woodlandOwner.IsOrganisation,
                ContactEmail = woodlandOwner.ContactEmail,
                ContactName = woodlandOwner.ContactName,
                OrganisationName = woodlandOwner.OrganisationName,
                HasActiveUserAccounts = hasActiveUserAccounts,
                AgencyId = agency?.Id,
                AgencyContactName = agency?.ContactName,
                AgencyName = agency?.OrganisationName
            };
        }
    }
}