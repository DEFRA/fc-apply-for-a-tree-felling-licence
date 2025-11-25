using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Microsoft.Extensions.Logging;
using NodaTime;

namespace Forestry.Flo.Services.FellingLicenceApplications.Services;

/// <summary>
/// Defines the contract for managing Approved In Error records for felling licence applications.
/// </summary>
public interface IApprovedInErrorService
{
    /// <summary>
    /// Retrieves the Approved In Error record for a specific application.
    /// </summary>
    /// <param name="applicationId">The unique identifier of the application.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>
    /// A <see cref="Maybe{T}"/> containing the <see cref="ApprovedInErrorModel"/> if found; otherwise, <see cref="Maybe{T}.None"/>.
    /// </returns>
    Task<Maybe<ApprovedInErrorModel>> GetApprovedInErrorAsync(Guid applicationId, CancellationToken cancellationToken);

    /// <summary>
    /// Marks a felling licence application as Approved In Error and performs all necessary operations
    /// including validation, document hiding, reference regeneration (if applicable), and persistence.
    /// </summary>
    /// <param name="applicationId">The unique identifier of the application to mark as approved in error.</param>
    /// <param name="model">The model containing the reasons and details for marking the application as approved in error.</param>
    /// <param name="userId">The unique identifier of the user performing the operation.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>
    /// A <see cref="Result"/> indicating success or failure of the operation.
    /// </returns>
    Task<Result> SetToApprovedInErrorAsync(Guid applicationId, ApprovedInErrorModel model, Guid userId, CancellationToken cancellationToken);
}

/// <summary>
/// Service responsible for managing the Approved In Error process for felling licence applications.
/// This service handles the business logic for marking applications as approved in error, including
/// validation, document management, reference regeneration, and data persistence.
/// </summary>
public class ApprovedInErrorService : IApprovedInErrorService
{
    private readonly IFellingLicenceApplicationInternalRepository _internalFlaRepository;
    private readonly IFellingLicenceApplicationReferenceRepository? _flaReferenceRepository;
    private readonly IApplicationReferenceHelper? _applicationReferenceHelper;
    private readonly IClock _clock;
    private readonly ILogger<ApprovedInErrorService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApprovedInErrorService"/> class without reference regeneration capabilities.
    /// </summary>
    /// <param name="internalFlaRepository">The repository for felling licence application data access.</param>
    /// <param name="clock">The clock service for obtaining the current timestamp.</param>
    /// <param name="logger">The logger for diagnostic and error logging.</param>
    /// <remarks>
    /// Use this constructor when reference regeneration is not required. The service will skip reference
    /// regeneration even if <see cref="ApprovedInErrorModel.ReasonOther"/> is false.
    /// </remarks>
    public ApprovedInErrorService(
    IFellingLicenceApplicationInternalRepository internalFlaRepository,
    IClock clock,
    ILogger<ApprovedInErrorService> logger)
    {
        _internalFlaRepository = Guard.Against.Null(internalFlaRepository);
        _clock = Guard.Against.Null(clock);
        _logger = Guard.Against.Null(logger);
        _flaReferenceRepository = null;
        _applicationReferenceHelper = null;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ApprovedInErrorService"/> class with full reference regeneration capabilities.
    /// </summary>
    /// <param name="internalFlaRepository">The repository for felling licence application data access.</param>
    /// <param name="clock">The clock service for obtaining the current timestamp.</param>
    /// <param name="logger">The logger for diagnostic and error logging.</param>
    /// <param name="flaReferenceRepository">The repository for managing application reference sequences.</param>
    /// <param name="applicationReferenceHelper">The helper for generating and updating application references.</param>
    /// <remarks>
    /// Use this constructor when reference regeneration is required. The service will regenerate the application
    /// reference number when <see cref="ApprovedInErrorModel.ReasonOther"/> is false, preserving the original prefix.
    /// </remarks>
    public ApprovedInErrorService(
    IFellingLicenceApplicationInternalRepository internalFlaRepository,
    IClock clock,
    ILogger<ApprovedInErrorService> logger,
    IFellingLicenceApplicationReferenceRepository flaReferenceRepository,
    IApplicationReferenceHelper applicationReferenceHelper)
    {
        _internalFlaRepository = Guard.Against.Null(internalFlaRepository);
        _clock = Guard.Against.Null(clock);
        _logger = Guard.Against.Null(logger);
        _flaReferenceRepository = Guard.Against.Null(flaReferenceRepository);
        _applicationReferenceHelper = Guard.Against.Null(applicationReferenceHelper);
    }

    /// <inheritdoc />
    public async Task<Maybe<ApprovedInErrorModel>> GetApprovedInErrorAsync(Guid applicationId, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Attempting to retrieve ApprovedInError for application with id {ApplicationId}", applicationId);
        var maybeEntity = await _internalFlaRepository.GetApprovedInErrorAsync(applicationId, cancellationToken);
        if (maybeEntity.HasNoValue)
        {
            return Maybe<ApprovedInErrorModel>.None;
        }
        return Maybe.From(maybeEntity.Value.ToModel());
    }

    /// <inheritdoc />
    public async Task<Result> SetToApprovedInErrorAsync(Guid applicationId, ApprovedInErrorModel model, Guid userId, CancellationToken cancellationToken)
    {
        Guard.Against.Null(model);
        _logger.LogDebug("Attempting to save ApprovedInError for application with id {ApplicationId}", applicationId);
        
        try
        {
            // Step 1: Retrieve and validate the application
            var application = await _internalFlaRepository.GetAsync(applicationId, cancellationToken);
            if (!application.HasValue)
            {
                _logger.LogError("Felling licence application not found, application id: {ApplicationId}", applicationId);
                return Result.Failure($"Felling licence application not found, application id: {applicationId}");
            }

            // Step 2: Validate current status - must be Approved
            var currentStatus = application.Value.StatusHistories.OrderByDescending(s => s.Created).FirstOrDefault()?.Status;
            if (currentStatus != FellingLicenceStatus.Approved)
            {
                _logger.LogError("Application {ApplicationId} cannot be marked as approved in error as it is not in the approved state. Current state: {Status}", applicationId, currentStatus);
                return Result.Failure("Application cannot be marked as approved in error as it is not in the approved state.");
            }

            // Step 3: Hide the most recent application document from the applicant (if one exists)
            var mostRecentApplicationDocument = GetMostRecentDocumentOfType(application.Value.Documents, DocumentPurpose.ApplicationDocument);
            if (mostRecentApplicationDocument.HasValue)
            {
                var UpdateDocumentVisibleResult = await _internalFlaRepository.UpdateDocumentVisibleToApplicantAsync(
                    applicationId,
                    mostRecentApplicationDocument.Value.Id,
                    false,
                    cancellationToken);
                if (UpdateDocumentVisibleResult.IsFailure)
                {
                    _logger.LogWarning(
                        "Unable to update document visibility for document [{Id}], received error [{Error}].",
                        mostRecentApplicationDocument.Value.Id, UpdateDocumentVisibleResult.Error);

                    return Result.Failure("Could not update document visibility");
                }
            }

            // Step 4: Update or create ApprovedInError entity
            var existing = await _internalFlaRepository.GetApprovedInErrorAsync(applicationId, cancellationToken);
            var entity = existing.HasValue ? existing.Value : new ApprovedInError { FellingLicenceApplicationId = applicationId };

            // Ensure consistent application id and update entity
            model.ApplicationId = applicationId;
            model.MapToEntity(entity);
            entity.FellingLicenceApplicationId = applicationId;
            entity.LastUpdatedById = userId;
            entity.LastUpdatedDate = _clock.GetCurrentInstant().ToDateTimeUtc();

            var upsertResult = await _internalFlaRepository.AddOrUpdateApprovedInErrorAsync(entity, cancellationToken);
            if (upsertResult.IsFailure)
            {
                _logger.LogError("Could not save ApprovedInError for application {ApplicationId}, error: {Error}", applicationId, upsertResult.Error);
                return Result.Failure(upsertResult.Error.ToString());
            }

            // Step 5: Regenerate reference if needed (when ReasonOther is false)
            if (model.ReasonOther == false)
            {
                var regenResult = await RegenerateReferenceAsync(applicationId, null, cancellationToken);
                if (regenResult.IsFailure)
                {
                    _logger.LogError("Failed to regenerate reference for application {ApplicationId}: {Error}", applicationId, regenResult.Error);
                    return Result.Failure(regenResult.Error);
                }
            }

            // Step 6: Add status history entry for ApprovedInError
            await _internalFlaRepository.AddStatusHistory(
                userId,
                applicationId,
                FellingLicenceStatus.ApprovedInError,
                cancellationToken);

            // Step 7: Persist all changes in a single transaction
            var saveResult = await _internalFlaRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
            if (saveResult.IsFailure)
            {
                _logger.LogWarning("Could not save changes for application with id {ApplicationId}, error: {Error}", applicationId, saveResult.Error);
                return Result.Failure(saveResult.Error.ToString());
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception caught in SetToApprovedInError");
            return Result.Failure(ex.Message);
        }
    }

    /// <summary>
    /// Gets the most recent document of the specified purpose type.
    /// </summary>
    /// <param name="documents">The collection of documents to search.</param>
    /// <param name="purpose">The purpose type to filter by.</param>
    /// <returns>The most recent <see cref="Document"/> of the specified purpose, or None if not found.</returns>
    private static Maybe<Document> GetMostRecentDocumentOfType(IList<Document>? documents, DocumentPurpose purpose)
    {
        if (documents == null) return Maybe<Document>.None;

        var lisDocument = documents.OrderByDescending(x => x.CreatedTimestamp)
            .FirstOrDefault(x => x.Purpose == purpose);

        if (lisDocument == null) return Maybe<Document>.None;

        return Maybe<Document>.From(lisDocument);
    }

    /// <summary>
    /// Regenerates the application reference number while preserving the original prefix.
    /// </summary>
    /// <param name="applicationId">The unique identifier of the application.</param>
    /// <param name="startingOffset">An optional offset to add to the reference counter.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>
    /// A <see cref="Result"/> indicating success or failure of the reference regeneration.
    /// </returns>
    /// <remarks>
    /// Extracts the prefix (e.g., "ABC" from "ABC/2024/12345"), generates a new reference with the next sequence number,
    /// and updates the application entity. Changes are not persisted until the caller saves the unit of work.
    /// </remarks>
    private async Task<Result> RegenerateReferenceAsync(
    Guid applicationId,
    int? startingOffset,
    CancellationToken cancellationToken)
    {
        try
        {
            // Check if reference regeneration dependencies are configured
            if (_flaReferenceRepository is null || _applicationReferenceHelper is null)
            {
                _logger.LogWarning("Reference regeneration dependencies not configured. Skipping regeneration for application {ApplicationId}", applicationId);
                return Result.Success();
            }

            // Retrieve the application
            var maybeApp = await _internalFlaRepository.GetAsync(applicationId, cancellationToken);
            if (maybeApp.HasNoValue)
            {
                return Result.Failure("Application not found");
            }

            var application = maybeApp.Value;
            
            // Get the next reference ID for the application's year
            var referenceId = await _flaReferenceRepository.GetNextApplicationReferenceIdValueAsync(application.CreatedTimestamp.Year, cancellationToken);

            // Extract the prefix from the current application reference (e.g., "ABC" from "ABC/2024/12345")
            string? prefix = null;
            if (!string.IsNullOrWhiteSpace(application.ApplicationReference))
            {
                var trimmed = application.ApplicationReference.Trim();
                var slashIndex = trimmed.IndexOf('/');
                prefix = slashIndex > 0 ? trimmed[..slashIndex] : trimmed;
            }

            // Generate a new reference with the extracted prefix
            var newReference = _applicationReferenceHelper.GenerateReferenceNumber(application, referenceId, null, startingOffset);
            if (!string.IsNullOrWhiteSpace(prefix))
            {
                newReference = _applicationReferenceHelper.UpdateReferenceNumber(newReference, prefix);
            }
            application.ApplicationReference = newReference;

            // Mark the application as updated (will be persisted when SaveEntitiesAsync is called)
            _internalFlaRepository.Update(application);

            _logger.LogDebug("Prepared regenerated application reference for {ApplicationId} to {Reference}", applicationId, newReference);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error regenerating application reference for {ApplicationId}", applicationId);
            return Result.Failure(ex.Message);
        }
    }
}
