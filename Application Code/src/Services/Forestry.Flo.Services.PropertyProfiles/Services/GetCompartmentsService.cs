using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.PropertyProfiles.Entities;
using Forestry.Flo.Services.PropertyProfiles.Repositories;

namespace Forestry.Flo.Services.PropertyProfiles.Services;

/// <summary>
/// Implementation of a <see cref="IGetCompartments"/> service
/// </summary>
public class GetCompartmentsService : IGetCompartments
{
    private readonly ICompartmentRepository _repository;

    public GetCompartmentsService(ICompartmentRepository repository)
    {
        _repository = Guard.Against.Null(repository);
    }

    /// <inheritdoc />>
    public async Task<Result<Compartment>> GetCompartmentByIdAsync(Guid compartmentId, UserAccessModel userAccessModel, CancellationToken cancellationToken)
    {
        var checkAccessResult = await _repository
            .CheckUserCanAccessCompartmentAsync(compartmentId, userAccessModel, cancellationToken)
            .ConfigureAwait(false);

        if (checkAccessResult.IsFailure || checkAccessResult.Value is false)
        {
            return Result.Failure<Compartment>("Could not access compartment reference");
        }

        var compartment = await _repository.GetByIdAsync(compartmentId, cancellationToken);

        return compartment.IsSuccess
            ? Result.Success(compartment.Value)
            : Result.Failure<Compartment>("Unable to get compartment by its id");
    }
}