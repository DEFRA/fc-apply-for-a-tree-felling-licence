using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Applicants.Models;
using Forestry.Flo.Services.Applicants.Repositories;
using Forestry.Flo.Services.Common.Models;
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
}