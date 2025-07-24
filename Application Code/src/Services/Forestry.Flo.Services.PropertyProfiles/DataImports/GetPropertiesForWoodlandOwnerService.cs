using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.PropertyProfiles.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Forestry.Flo.Services.PropertyProfiles.DataImports;

/// <summary>
/// Implementation of <see cref="IGetPropertiesForWoodlandOwner"/> service using the
/// <see cref="IPropertyProfileRepository"/> to retrieve property and compartment information
/// </summary>
public class GetPropertiesForWoodlandOwnerService : IGetPropertiesForWoodlandOwner
{
    private readonly IPropertyProfileRepository _repository;
    private readonly ILogger<GetPropertiesForWoodlandOwnerService> _logger;

    /// <summary>
    /// Creates a new instance of <see cref="GetPropertiesForWoodlandOwnerService"/>
    /// </summary>
    /// <param name="repository">A <see cref="IPropertyProfileRepository"/> with which to retrieve properties
    /// and their compartments from the database.</param>
    /// <param name="logger">A logging instance.</param>
    public GetPropertiesForWoodlandOwnerService(
        IPropertyProfileRepository repository,
        ILogger<GetPropertiesForWoodlandOwnerService> logger)
    {
        ArgumentNullException.ThrowIfNull(repository);

        _repository = repository;
        _logger = logger ?? new NullLogger<GetPropertiesForWoodlandOwnerService>();
    }

    /// <inheritdoc />
    public async Task<Result<IEnumerable<PropertyIds>>> GetPropertiesForDataImport(
        UserAccessModel userAccessModel, 
        Guid woodlandOwnerId,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("GetPropertiesForDataImport called for woodland owner {WoodlandOwnerId}", woodlandOwnerId);

        var properties = await _repository.ListAsync(woodlandOwnerId, cancellationToken);

        if (properties.IsFailure)
        {
            _logger.LogError(
                "Failed to retrieve properties for woodland owner {WoodlandOwnerId}: {ErrorMessage}",
                woodlandOwnerId, properties.Error);
            return Result.Failure<IEnumerable<PropertyIds>>($"Could not get properties for woodland owner {woodlandOwnerId}");
        }

        var query = new ListPropertyProfilesQuery(woodlandOwnerId, properties.Value.Select(p => p.Id).ToList());

        var checkAccessResult = await _repository
            .CheckUserCanAccessPropertyProfilesAsync(query, userAccessModel, cancellationToken)
            .ConfigureAwait(false);

        if (checkAccessResult.IsFailure || checkAccessResult.Value is false)
        {
            _logger.LogError(
                "User {UserId} does not have access to properties for woodland owner {WoodlandOwnerId}",
                userAccessModel.UserAccountId, woodlandOwnerId);
            return Result.Failure<IEnumerable<PropertyIds>>("User does not have access to specified woodland owner id");
        }

        var propertiesList = properties.Value.ToList();

        _logger.LogDebug("Returning {PropertyCount} properties for woodland owner {WoodlandOwnerId}",
            propertiesList.Count, woodlandOwnerId);

        var mapped = propertiesList
            .Select(p => new PropertyIds
            {
                Id = p.Id,
                Name = p.Name,
                CompartmentIds = p.Compartments.Select(c => new CompartmentIds
                {
                    Id = c.Id,
                    CompartmentName = c.CompartmentNumber,
                    Area = c.TotalHectares
                })
            });
        return Result.Success(mapped);
    }
}