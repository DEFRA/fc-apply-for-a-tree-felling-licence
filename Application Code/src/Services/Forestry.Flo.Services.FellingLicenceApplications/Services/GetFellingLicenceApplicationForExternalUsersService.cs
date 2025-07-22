using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;

namespace Forestry.Flo.Services.FellingLicenceApplications.Services;

/// <summary>
/// Service class for retrieving <see cref="FellingLicenceApplication"/>. 
/// </summary>
public class GetFellingLicenceApplicationForExternalUsersService : IGetFellingLicenceApplicationForExternalUsers
{
    private readonly IFellingLicenceApplicationExternalRepository _repository;

    public GetFellingLicenceApplicationForExternalUsersService(IFellingLicenceApplicationExternalRepository repository)
    {
        _repository = Guard.Against.Null(repository);
    }

    /// <inheritdoc />>
    public async Task<Result<IEnumerable<FellingLicenceApplication>>> GetApplicationsForWoodlandOwnerAsync(
        Guid woodlandOwnerId, 
        UserAccessModel userAccessModel,
        CancellationToken cancellationToken)
    {
        if (userAccessModel.CanManageWoodlandOwner(woodlandOwnerId) == false)
        {
            return Result.Failure<IEnumerable<FellingLicenceApplication>>("User cannot access applications for this woodland owner");
        }

        var results = await _repository.ListAsync(woodlandOwnerId, cancellationToken).ConfigureAwait(false);
        return Result.Success(results);
    }

    /// <inheritdoc />>
    public async Task<Result<FellingLicenceApplication>> GetApplicationByIdAsync(
        Guid applicationId,
        UserAccessModel userAccessModel,
        CancellationToken cancellationToken)
    {
        var checkAccessResult = await _repository
            .CheckUserCanAccessApplicationAsync(applicationId, userAccessModel, cancellationToken)
            .ConfigureAwait(false);

        if (checkAccessResult.IsFailure || checkAccessResult.Value is false)
        {
            return Result.Failure<FellingLicenceApplication>("Could not access application reference");
        }

        var application = await _repository.GetAsync(applicationId, cancellationToken);

        return application.HasValue
            ? Result.Success(application.Value)
            : Result.Failure<FellingLicenceApplication>($"Unable to retrieve application with id {applicationId}");
    }

    public async Task<Result<bool>> GetIsEditable(
        Guid fellingLicenceApplicationId, 
        UserAccessModel userAccessModel,
        CancellationToken cancellationToken)
    {
        var checkAccessResult = await _repository
            .CheckUserCanAccessApplicationAsync(fellingLicenceApplicationId, userAccessModel, cancellationToken)
            .ConfigureAwait(false);

        if (checkAccessResult.IsFailure || checkAccessResult.Value is false)
        {
            return Result.Failure<bool>("Could not access application with given id");
        }

        return await _repository.GetIsEditable(fellingLicenceApplicationId, cancellationToken);
    }
}