using Ardalis.GuardClauses;
using Azure.Core;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Applicants.Entities;
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
            Address = request.AgencyModel.Address,
            ContactEmail = request.AgencyModel.ContactEmail,
            ContactName = request.AgencyModel.ContactName,
            OrganisationName = request.AgencyModel.OrganisationName,
            IsOrganisation = request.AgencyModel.IsOrganisation
        };

        var saveToDbResult = await _agencyRepository.AddAgencyAsync(entity, cancellationToken);

        if (saveToDbResult.IsSuccess)
        {
            return Result.Success(new AddAgencyDetailsResponse { AgencyId = saveToDbResult.Value.Id });
        }

        _logger.LogError("Could not save Agency entity to database, error {Error}", saveToDbResult.Error);
        return Result.Failure<AddAgencyDetailsResponse>(saveToDbResult.Error.ToString());
    }

    /// <inheritdoc />
    public async Task<Result> UpdateAgencyAsync(
        UpdateAgencyDetailsRequest request, 
        CancellationToken cancellationToken)
    {
        Guard.Against.Null(request);

        _logger.LogDebug("Received request to update Agency with id {AgencyId} from user {UserId}", request.AgencyId, request.UpdatedByUser);

        var existingAgency = await _agencyRepository.GetAsync(request.AgencyId, cancellationToken);

        if (existingAgency.IsFailure)
        {
            _logger.LogError("Could not locate agency with id {AgencyId}", request.AgencyId);
            return Result.Failure("Could not locate agency with given ID");
        }

        existingAgency.Value.Address = request.AgencyModel.Address;
        existingAgency.Value.ContactEmail = request.AgencyModel.ContactEmail;
        existingAgency.Value.ContactName = request.AgencyModel.ContactName;
        existingAgency.Value.OrganisationName = request.AgencyModel.OrganisationName;
        existingAgency.Value.IsOrganisation = request.AgencyModel.IsOrganisation;

        var saveResult = await _agencyRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        if (saveResult.IsFailure)
        {
            _logger.LogError("Could not save updated agency details to database, error {Error}", saveResult.Error);
            return Result.Failure(saveResult.Error.ToString());
        }

        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result<AgencyModel>> GetAgencyDetailsAsync(
        Guid agencyId, 
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Received request to retrieve Agency with id {AgencyId}", agencyId);

        var existingAgency = await _agencyRepository.GetAsync(agencyId, cancellationToken);

        if (existingAgency.IsFailure)
        {
            _logger.LogError("Could not locate agency with id {AgencyId}", agencyId);
            return Result.Failure<AgencyModel>("Could not locate agency with given ID");
        }

        var result = new AgencyModel
        {
            AgencyId = existingAgency.Value.Id,
            ContactEmail = existingAgency.Value.ContactEmail,
            ContactName = existingAgency.Value.ContactName,
            IsOrganisation = existingAgency.Value.IsOrganisation,
            OrganisationName = existingAgency.Value.OrganisationName,
            IsFcAgency = existingAgency.Value.IsFcAgency,
            Address = existingAgency.Value.Address == null
                ? null
                : new Address(existingAgency.Value.Address.Line1, existingAgency.Value.Address.Line2,
                    existingAgency.Value.Address.Line3, existingAgency.Value.Address.Line4,
                    existingAgency.Value.Address.PostalCode)
        };

        return Result.Success(result);
    }
}