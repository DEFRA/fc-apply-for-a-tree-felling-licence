using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.PropertyProfiles.Entities;
using Forestry.Flo.Services.PropertyProfiles.Repositories;

namespace Forestry.Flo.Services.PropertyProfiles.Services;

/// <summary>
/// Implementation of a <see cref="IGetPropertyProfiles"/> service
/// </summary>
public class GetPropertyProfilesService : IGetPropertyProfiles
{
    private readonly IPropertyProfileRepository _repository;

    public GetPropertyProfilesService(IPropertyProfileRepository repository)
    {
        _repository = Guard.Against.Null(repository);
    }

    /// <inheritdoc />>
    public async Task<Result<PropertyProfile>> GetPropertyByIdAsync(Guid propertyProfileId, UserAccessModel userAccessModel, CancellationToken cancellationToken)
    {
        var checkAccessResult = await _repository
            .CheckUserCanAccessPropertyProfileAsync(propertyProfileId, userAccessModel, cancellationToken)
            .ConfigureAwait(false);

        if (checkAccessResult.IsFailure || checkAccessResult.Value is false)
        {
            return Result.Failure<PropertyProfile>("Could not access property reference");
        }

        var property = await _repository.GetByIdAsync(propertyProfileId, cancellationToken);

        return property.IsSuccess
            ? Result.Success(property.Value)
            : Result.Failure<PropertyProfile>("Unable to get property by its id");
    }

    /// <inheritdoc />>
    public async Task<Result<IEnumerable<PropertyProfile>>> ListAsync(
        ListPropertyProfilesQuery query, 
        UserAccessModel userAccessModel, 
        CancellationToken cancellationToken)
    {
        var checkAccessResult = await _repository
            .CheckUserCanAccessPropertyProfilesAsync(query, userAccessModel, cancellationToken)
            .ConfigureAwait(false);

        if (checkAccessResult.IsFailure || checkAccessResult.Value is false)
        {
            return Result.Failure<IEnumerable<PropertyProfile>>("Could not access property reference");
        }

        var properties = await _repository.ListAsync(query, cancellationToken);
        return Result.Success(properties);
    }

    public async Task<Result<IEnumerable<PropertyProfile>>> ListByWoodlandOwnerAsync(
    Guid woodlandOwnerId,
    UserAccessModel userAccessModel,
    CancellationToken cancellationToken)
    {
        var properties = await _repository.ListAsync(woodlandOwnerId, cancellationToken);

        if (properties.IsFailure)
        {
            return Result.Failure<IEnumerable<PropertyProfile>>($"Could not get properties for woodland owner {woodlandOwnerId}");
        }

        var query = new ListPropertyProfilesQuery(woodlandOwnerId, properties.Value.Select(p => p.Id).ToList() );

        var checkAccessResult = await _repository
            .CheckUserCanAccessPropertyProfilesAsync(query, userAccessModel, cancellationToken)
            .ConfigureAwait(false);

        if (checkAccessResult.IsFailure || checkAccessResult.Value is false)
        {
            return Result.Failure<IEnumerable<PropertyProfile>>("Could not access property reference");
        }

        return Result.Success(properties.Value);
    }
}
