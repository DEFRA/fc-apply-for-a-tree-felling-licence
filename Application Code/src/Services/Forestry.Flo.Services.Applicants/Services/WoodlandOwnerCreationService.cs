using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Applicants.Entities.WoodlandOwner;
using Forestry.Flo.Services.Applicants.Models;
using Forestry.Flo.Services.Applicants.Repositories;
using Microsoft.Extensions.Logging;

namespace Forestry.Flo.Services.Applicants.Services;

/// <summary>
/// Implementation of <see cref="IWoodlandOwnerCreationService"/> that uses an <see cref="IWoodlandOwnerRepository"/>
/// to interact with the database.
/// </summary>
public class WoodlandOwnerCreationService : IWoodlandOwnerCreationService
{
    private readonly IWoodlandOwnerRepository _woodlandOwnerRepository;
    private readonly ILogger<WoodlandOwnerCreationService> _logger;

    public WoodlandOwnerCreationService(
        IWoodlandOwnerRepository woodlandOwnerRepository,
        ILogger<WoodlandOwnerCreationService> logger)
    {
        _woodlandOwnerRepository = Guard.Against.Null(woodlandOwnerRepository);
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<AddWoodlandOwnerDetailsResponse>> AddWoodlandOwnerDetails(AddWoodlandOwnerDetailsRequest request, CancellationToken cancellationToken)
    {
        Guard.Against.Null(request);
        _logger.LogDebug("Received request to add a new Agent Authority entry in the system from user with id {UserId}", request.CreatedByUser);

        var entity = new WoodlandOwner
        {
            ContactAddress = request.WoodlandOwner.ContactAddress,
            ContactEmail = request.WoodlandOwner.ContactEmail,
            ContactName = request.WoodlandOwner.ContactName,
            ContactTelephone = request.WoodlandOwner.ContactTelephone,
            IsOrganisation = request.WoodlandOwner.IsOrganisation,
            OrganisationAddress = request.WoodlandOwner.OrganisationAddress,
            OrganisationName = request.WoodlandOwner.OrganisationName
        };

        var saveToDbResult = await _woodlandOwnerRepository.AddWoodlandOwnerAsync(entity, cancellationToken);

        if (saveToDbResult.IsSuccess)
        {
            return Result.Success(new AddWoodlandOwnerDetailsResponse { WoodlandOwnerId = saveToDbResult.Value.Id });
        }

        _logger.LogError("Could not save Woodland Owner entity to database, error {Error}", saveToDbResult.Error);
        return Result.Failure<AddWoodlandOwnerDetailsResponse>(saveToDbResult.Error.ToString());
    }

    /// <inheritdoc />
    public async Task<Result<bool>> AmendWoodlandOwnerDetailsAsync(
        WoodlandOwnerModel model,
        CancellationToken cancellationToken)
    {
        Guard.Against.Null(model.Id);
        
        var (_, isFailure, woodlandOwner, error) = await _woodlandOwnerRepository.GetAsync(model.Id.Value, cancellationToken);

        if (isFailure)
        {
            _logger.LogError("Unable to retrieve woodland owner with id {id}, error: {error}", model.Id, error);
            return Result.Failure<bool>("Unable to retrieve woodland owner");
        }

        woodlandOwner.ContactAddress = model.ContactAddress;
        woodlandOwner.ContactEmail = model.ContactEmail;
        woodlandOwner.ContactName = model.ContactName;
        woodlandOwner.ContactTelephone = model.ContactTelephone;
        woodlandOwner.IsOrganisation = model.IsOrganisation;
        woodlandOwner.OrganisationAddress = model.OrganisationAddress;
        woodlandOwner.OrganisationName = model.OrganisationName;

        return await _woodlandOwnerRepository.UnitOfWork.SaveChangesAsync(cancellationToken) > 0;
    }
}