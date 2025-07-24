using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common.Infrastructure;
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
}
