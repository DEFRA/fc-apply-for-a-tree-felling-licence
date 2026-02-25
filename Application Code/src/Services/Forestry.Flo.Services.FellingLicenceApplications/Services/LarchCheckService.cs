using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Microsoft.Extensions.Logging;
using NodaTime;

namespace Forestry.Flo.Services.FellingLicenceApplications.Services;

/// <summary>
/// Interface defining the contract for Larch Check Service.
/// </summary>
public interface ILarchCheckService
{
    /// <summary>
    /// Retrieves the larch check details for a given application.
    /// </summary>
    /// <param name="applicationId">The ID of the application.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The larch check details model.</returns>
    Task<Maybe<LarchCheckDetailsModel>> GetLarchCheckDetailsAsync(Guid applicationId, CancellationToken cancellationToken);

    /// <summary>
    /// Saves the larch check details for a given application.
    /// </summary>
    /// <param name="applicationId">The ID of the application.</param>
    /// <param name="model">The larch check details model.</param>
    /// <param name="userId">The ID of the user performing the operation.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The result of the save operation.</returns>
    Task<Result> SaveLarchCheckDetailsAsync(Guid applicationId, LarchCheckDetailsModel model, Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Saves the larch flyover details for a given application.
    /// </summary>
    /// <param name="applicationId">The ID of the application.</param>
    /// <param name="flightDate">The date of the flight.</param>
    /// <param name="flightObservations">The observations made during the flight.</param>
    /// <param name="userId">The ID of the user performing the operation.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The result of the save operation.</returns>
    Task<Result> SaveLarchFlyoverAsync(Guid applicationId, DateTime flightDate, string flightObservations, Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Gets a list of <see cref="TreeSpeciesModel"/> for the application, for the species being felled,
    /// with larch species listed first.
    /// </summary>
    /// <remarks>
    /// If the application Confirmed felling and restocking is not complete yet, this will be based on the
    /// proposed felling and restocking details, otherwise it will be based on the confirmed felling and restocking details.
    /// </remarks>
    /// <param name="applicationId">The id of the application to interrogate.</param>
    /// <param name="forceProposedDetails">Force returning species from the proposed details, even if the
    /// confirmed felling and restocking has been completed.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The relevant list of species models for the application.</returns>
    Task<Result<IEnumerable<TreeSpeciesModel>>> GetAllFellingSpeciesLarchFirstAsync(
        Guid applicationId,
        bool forceProposedDetails,
        CancellationToken cancellationToken);
}

/// <summary>
/// Service implementation for managing Larch Check operations.
/// </summary>
public class LarchCheckService : ILarchCheckService
{
    private readonly IFellingLicenceApplicationInternalRepository _internalFlaRepository;
    private readonly IClock _clock;
    private readonly ILogger<LarchCheckService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="LarchCheckService"/> class.
    /// </summary>
    /// <param name="internalFlaRepository">The internal repository for felling licence applications.</param>
    /// <param name="clock">The clock instance for getting the current time.</param>
    /// <param name="logger">The logger instance.</param>
    public LarchCheckService(
        IFellingLicenceApplicationInternalRepository internalFlaRepository,
        IClock clock,
        ILogger<LarchCheckService> logger)
    {
        _internalFlaRepository = Guard.Against.Null(internalFlaRepository);
        _clock = Guard.Against.Null(clock);
        _logger = Guard.Against.Null(logger);
    }

    /// <inheritdoc/>
    public async Task<Maybe<LarchCheckDetailsModel>> GetLarchCheckDetailsAsync(
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Attempting to retrieve larch check details for application with id {ApplicationId}", applicationId);
        var larchCheckDetails = await _internalFlaRepository.GetLarchCheckDetailsAsync(applicationId, cancellationToken);

        if (larchCheckDetails.HasNoValue)
        {
            return Maybe<LarchCheckDetailsModel>.None;
        }
        _logger.LogDebug("Returning the larch check details for application with id {ApplicationId}", applicationId);
        return larchCheckDetails.Value.ToModel();
    }

    /// <inheritdoc/>
    public async Task<Result> SaveLarchCheckDetailsAsync(
        Guid applicationId,
        LarchCheckDetailsModel model,
        Guid userId,
        CancellationToken cancellationToken)
    {
        Guard.Against.Null(model);
        _logger.LogDebug("Attempting to update the larch check details for application with id {ApplicationId}", applicationId);

        try
        {
            var maybeExistingLarchCheck = await _internalFlaRepository.GetLarchCheckDetailsAsync(applicationId, cancellationToken);
            var entity = maybeExistingLarchCheck.HasValue ? maybeExistingLarchCheck.Value : new LarchCheckDetails
            {
                FellingLicenceApplicationId = applicationId
            };
            model.MapToEntity(entity);

            entity.LastUpdatedById = userId;
            entity.LastUpdatedDate = _clock.GetCurrentInstant().ToDateTimeUtc();

            var saveResult = await _internalFlaRepository.AddOrUpdateLarchCheckDetailsAsync(entity, cancellationToken);

            if (saveResult.IsFailure)
            {
                _logger.LogError("Could not save changes to larch check details, error: {Error}", saveResult.Error);
                return Result.Failure(saveResult.Error.ToString());
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception caught in SaveLarchCheckDetailsAsync");
            return Result.Failure(ex.Message);
        }
    }

    /// <inheritdoc/>
    public async Task<Result> SaveLarchFlyoverAsync(
        Guid applicationId,
        DateTime flightDate,
        string flightObservations,
        Guid userId,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Attempting to update the larch flyover for application with id {ApplicationId}", applicationId);

        try
        {
            var maybeExistingLarchCheck = await _internalFlaRepository.GetLarchCheckDetailsAsync(applicationId, cancellationToken);
            var entity = maybeExistingLarchCheck.HasValue ? maybeExistingLarchCheck.Value : new LarchCheckDetails
            {
                FellingLicenceApplicationId = applicationId
            };

            entity.FlightDate = flightDate;
            entity.FlightObservations = flightObservations;

            entity.LastUpdatedById = userId;
            entity.LastUpdatedDate = _clock.GetCurrentInstant().ToDateTimeUtc();

            var saveResult = await _internalFlaRepository.AddOrUpdateLarchCheckDetailsAsync(entity, cancellationToken);

            if (saveResult.IsFailure)
            {
                _logger.LogError("Could not save changes to larch check details, error: {Error}", saveResult.Error);
                return Result.Failure(saveResult.Error.ToString());
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception caught in SaveLarchFlyoverAsync");
            return Result.Failure(ex.Message);
        }
    }

    /// <inheritdoc/>
    public async Task<Result<IEnumerable<TreeSpeciesModel>>> GetAllFellingSpeciesLarchFirstAsync(
        Guid applicationId,
        bool forceProposedDetails = false,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Attempting to retrieve if there are any larch species in application with id {ApplicationId}", applicationId);

        try
        {
            var entity = await _internalFlaRepository.GetAsync(applicationId, cancellationToken);

            if (entity.HasNoValue)
            {
                return Result.Failure<IEnumerable<TreeSpeciesModel>>("Could not locate application with id " + applicationId);
            }

            List<string> allSpecies;

            //not confirmed f&r yet, so based on proposed details
            if (entity.Value.WoodlandOfficerReview?.ConfirmedFellingAndRestockingComplete is false
                || forceProposedDetails)
            {
                var fellingSpecies =
                    entity.Value.LinkedPropertyProfile?.ProposedFellingDetails?.SelectMany(x => x.FellingSpecies ?? [])
                    ?? [];

                allSpecies = fellingSpecies.Select(x => x.Species)
                    .Distinct()
                    .ToList();
            }
            else
            {
                //confirmed f&r, so based on confirmed details
                var confirmedFellingSpecies =
                    entity.Value.SubmittedFlaPropertyDetail?.SubmittedFlaPropertyCompartments?.SelectMany(x =>
                        x.ConfirmedFellingDetails.SelectMany(y => y.ConfirmedFellingSpecies)) ?? [];

                allSpecies = confirmedFellingSpecies.Select(x => x.Species)
                    .Distinct()
                    .ToList();
            }

            var models = allSpecies
                .Select(x => TreeSpeciesFactory.SpeciesDictionary.Values.FirstOrDefault(treeSpecies => treeSpecies.Code == x))
                .Where(x => x != null)
                .Select(x => x!)
                .OrderByDescending(x => x.IsLarch)
                    .ThenBy(x => x.Name)
                .AsEnumerable();

            return Result.Success(models);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception caught in GetAllFellingSpeciesLarchFirst");
            return Result.Failure<IEnumerable<TreeSpeciesModel>>(ex.Message);
        }
    }
}
