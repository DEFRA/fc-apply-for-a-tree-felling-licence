using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Applicants.Entities.Agent;
using Forestry.Flo.Services.Applicants.Models;
using Forestry.Flo.Services.Applicants.Repositories;
using Forestry.Flo.Services.Common.User;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Forestry.Flo.Services.Applicants.Services;

public class RetrieveAgenciesService : IRetrieveAgencies
{
    private readonly IUserAccountRepository _userAccountRepository;
    private readonly IAgencyRepository _agencyRepository;
    private readonly ILogger<RetrieveAgenciesService> _logger;

    public RetrieveAgenciesService(
        IUserAccountRepository userAccountRepository,
        IAgencyRepository agencyRepository,
        ILogger<RetrieveAgenciesService> logger)
    {
        ArgumentNullException.ThrowIfNull(userAccountRepository);
        ArgumentNullException.ThrowIfNull(agencyRepository);

        _userAccountRepository = userAccountRepository;
        _agencyRepository = agencyRepository;
        _logger = logger ?? new NullLogger<RetrieveAgenciesService>();
    }

    /// <inheritdoc />
    public async Task<Result<List<AgencyFcModel>>> GetAllAgenciesForFcAsync(Guid performingUserId, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Received request for all agencies in the system for the FC dashboard");

        var user = await _userAccountRepository
            .GetAsync(performingUserId, cancellationToken)
            .ConfigureAwait(false);
        if (user.IsFailure)
        {
            _logger.LogWarning("Could not retrieve user with id {PerformingUserId}", performingUserId);
            return Result.Failure<List<AgencyFcModel>>("Could not retrieve user to assert permissions");
        }

        if (user.Value.AccountType != AccountTypeExternal.FcUser)
        {
            _logger.LogWarning("User with id {PerformingUserId} has account type {AccountType} and should not be retrieving this data",
                performingUserId, user.Value.AccountType);
            return Result.Failure<List<AgencyFcModel>>("User does not have permissions to retrieve all agencies");
        }

        var result = new List<AgencyFcModel>();

        // get agencies linked directly to an external applicant account
        var applicantAgencies = await _agencyRepository
            .GetAgenciesForActiveAccountsAsync(cancellationToken)
            .ConfigureAwait(false);
        _logger.LogDebug("Retrieved {Count} agencies linked directly to an active external applicant account",
            applicantAgencies.Count);

        foreach (var agency in applicantAgencies.Where(x => !x.IsFcAgency))
        {
            result.Add(ToAgencyFcModel(agency));
        }

        // get all other agencies that are managed by FC
        var agencyIds = result.Select(x => x.Id).ToList();
        var fcManagedAgencies = await _agencyRepository
            .GetAgenciesWithIdNotIn(agencyIds, cancellationToken)
            .ConfigureAwait(false);
        _logger.LogDebug("Retrieved {Count} agencies not linked to an external applicant",
            fcManagedAgencies.Count);

        foreach (var fcManagedAgency in fcManagedAgencies.Where(x => !x.IsFcAgency))
        {
            result.Add(ToAgencyFcModel(fcManagedAgency, false));
        }

        return Result.Success(result);

        AgencyFcModel ToAgencyFcModel(Agency agency, bool hasActiveUserAccounts = true)
        {
            return new AgencyFcModel
            {
                Id = agency.Id,
                OrganisationName = agency.OrganisationName,
                ContactEmail = agency.ContactEmail,
                ContactName = agency.ContactName,
                HasActiveUserAccounts = hasActiveUserAccounts
            };
        }
    }
}