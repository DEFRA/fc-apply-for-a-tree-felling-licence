using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Applicants.Entities.Agent;
using Forestry.Flo.Services.Applicants.Models;
using Forestry.Flo.Services.Applicants.Repositories;
using Microsoft.Extensions.Logging;

namespace Forestry.Flo.Services.Applicants.Services;

/// <summary>
/// Implementation of <see cref="IAgencyCreationService"/> that uses an <see cref="AgencyRepository"/>
/// to interact with the database.
/// </summary>
public class AgencyCreationService : IAgencyCreationService
{
    private readonly IAgencyRepository _agencyRepository;
    private readonly ILogger<AgencyCreationService> _logger;

    public AgencyCreationService(
        IAgencyRepository agencyRepository, 
        ILogger<AgencyCreationService> logger)
    {
        _agencyRepository = Guard.Against.Null(agencyRepository);
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<AddAgencyDetailsResponse>> AddAgencyAsync(
        AddAgencyDetailsRequest request, 
        CancellationToken cancellationToken)
    {
        Guard.Against.Null(request);
        _logger.LogDebug("Received request to add a new Agency in the system from user with id {UserId}", request.CreatedByUser);

        var entity = new Agency
        {
            IsFcAgency = false,
            ShouldAutoApproveThinningApplications = false,
            Address = request.agencyModel.Address,
            ContactEmail = request.agencyModel.ContactEmail,
            ContactName = request.agencyModel.ContactName,
            OrganisationName = request.agencyModel.OrganisationName,
            IsOrganisation = !string.IsNullOrEmpty(request.agencyModel.OrganisationName)
        };

        var saveToDbResult = await _agencyRepository.AddAgencyAsync(entity, cancellationToken);

        if (saveToDbResult.IsSuccess)
        {
            return Result.Success(new AddAgencyDetailsResponse { AgencyId = saveToDbResult.Value.Id });
        }

        _logger.LogError("Could not save Agency entity to database, error {Error}", saveToDbResult.Error);
        return Result.Failure<AddAgencyDetailsResponse>(saveToDbResult.Error.ToString());
    }
}