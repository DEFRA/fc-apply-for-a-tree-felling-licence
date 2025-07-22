using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Applicants.Entities.WoodlandOwner;
using Forestry.Flo.Services.Applicants.Models;
using Forestry.Flo.Services.Applicants.Repositories;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.Extensions;
using Forestry.Flo.Services.FileStorage.Model;
using Forestry.Flo.Services.FileStorage.Services;
using Microsoft.Extensions.Logging;

namespace Forestry.Flo.Services.Applicants.Services;

/// <summary>
/// Implementation of <see cref="IRetrieveLegacyDocuments"/> that uses repository classes to
/// retrieve the legacy documents.
/// </summary>
public class RetrieveLegacyDocumentsService : IRetrieveLegacyDocuments
{
    private readonly ILegacyDocumentsRepository _legacyDocumentsRepository;
    private readonly IWoodlandOwnerRepository _woodlandOwnerRepository;
    private readonly IUserAccountRepository _userAccountRepository;
    private readonly IAgencyRepository _agencyRepository;
    private readonly IFileStorageService _fileStorageService;
    private readonly IAuditService<RetrieveLegacyDocumentsService> _auditService;
    private readonly RequestContext _requestContext;
    private readonly ILogger<RetrieveLegacyDocumentsService> _logger;

    /// <summary>
    /// Creates a new instance of <see cref="RetrieveLegacyDocumentsService"/>.
    /// </summary>
    /// <param name="legacyDocumentsRepository">An implementation of <see cref="ILegacyDocumentsRepository"/>.</param>
    /// <param name="woodlandOwnerRepository">An implementation of <see cref="IWoodlandOwnerRepository"/>.</param>
    /// <param name="userAccountRepository">An implementation of <see cref="IUserAccountRepository"/>.</param>
    /// <param name="agencyRepository">An implementation of <see cref="IAgencyRepository"/>.</param>
    /// <param name="fileStorageService">An implementation of <see cref="IFileStorageService"/>.</param>
    /// <param name="auditService">An <see cref="IAuditService{T}"/> implementation.</param>
    /// <param name="requestContext">A request context.</param>
    /// <param name="logger">A logging instance.</param>
    public RetrieveLegacyDocumentsService(
        ILegacyDocumentsRepository legacyDocumentsRepository,
        IWoodlandOwnerRepository woodlandOwnerRepository,
        IUserAccountRepository userAccountRepository,
        IAgencyRepository agencyRepository,
        IFileStorageService fileStorageService,
        IAuditService<RetrieveLegacyDocumentsService> auditService,
        RequestContext requestContext,
        ILogger<RetrieveLegacyDocumentsService> logger)
    {
        ArgumentNullException.ThrowIfNull(legacyDocumentsRepository);
        ArgumentNullException.ThrowIfNull(woodlandOwnerRepository);
        ArgumentNullException.ThrowIfNull(userAccountRepository);
        ArgumentNullException.ThrowIfNull(agencyRepository);
        ArgumentNullException.ThrowIfNull(fileStorageService);
        ArgumentNullException.ThrowIfNull(auditService);
        ArgumentNullException.ThrowIfNull(requestContext);

        _legacyDocumentsRepository = legacyDocumentsRepository;
        _woodlandOwnerRepository = woodlandOwnerRepository;
        _userAccountRepository = userAccountRepository;
        _agencyRepository = agencyRepository;
        _fileStorageService = fileStorageService;
        _auditService = auditService;
        _requestContext = requestContext;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<IList<LegacyDocumentModel>>> RetrieveLegacyDocumentsForUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Request received to retrieve legacy documents for user with id {UserId}", userId);

        try
        {
            var userAccount = await _userAccountRepository.GetAsync(userId, cancellationToken).ConfigureAwait(false);

            if (userAccount.IsFailure)
            {
                _logger.LogError("Could not locate a user account for the given id {UserId}", userId);
                return Result.Failure<IList<LegacyDocumentModel>>("Could not retrieve legacy documents for the given user id.");
            }

            // woodland owner user
            if (userAccount.Value.AgencyId.HasNoValue())
            {
                _logger.LogDebug("User account with id {UserId} is not linked to an agency, returning for woodland owner id", userId);

                if (userAccount.Value.WoodlandOwnerId.HasNoValue())
                {
                    _logger.LogError("User account with id {UserId} does not have an agency id or a woodland owner id", userId);
                    return Result.Failure<IList<LegacyDocumentModel>>("Could not retrieve legacy documents for the given user id.");
                }

                var documents =
                    await _legacyDocumentsRepository.GetAllForWoodlandOwnerIdsAsync(
                        new List<Guid> { userAccount.Value.WoodlandOwnerId!.Value }, cancellationToken)
                        .ConfigureAwait(false);

                return await MapLegacyDocumentsToModels(documents, cancellationToken).ConfigureAwait(false);
            }

            // fc agent user?
            var fcAgency = await _agencyRepository.FindFcAgency(cancellationToken).ConfigureAwait(false);
            if (fcAgency.HasValue && userAccount.Value.AgencyId == fcAgency.Value.Id)
            {
                _logger.LogDebug("User account for user id {UserId} is part of the FC agency, returning all legacy documents in storage", userId);
                var allLegacyDocuments = await _legacyDocumentsRepository.GetAllLegacyDocumentsAsync(cancellationToken).ConfigureAwait(false);
                return await MapLegacyDocumentsToModels(allLegacyDocuments, cancellationToken).ConfigureAwait(false);
            }

            // standard agency user

            _logger.LogDebug("User account with id {UserId} is linked to an agency, returning legacy documents for woodland owners linked to their agency", userId);

            var authorities = await _agencyRepository.ListAuthoritiesByAgencyAsync(
                userAccount.Value.AgencyId!.Value, null, cancellationToken).ConfigureAwait(false);

            if (authorities.IsFailure)
            {
                _logger.LogError("Could not retrieve agent authorities for user account with id {UserId}", userId);
                return Result.Failure<IList<LegacyDocumentModel>>("Could not retrieve legacy documents for the given user id.");
            }

            var woodlandOwners = authorities.Value
                .Select(x => x.WoodlandOwner)
                .DistinctBy(y => y.Id)
                .ToList();

            var agencyLegacyDocuments = await _legacyDocumentsRepository.GetAllForWoodlandOwnerIdsAsync(
                woodlandOwners.Select(x => x.Id).ToArray(), cancellationToken).ConfigureAwait(false);

            return MapLegacyDocumentsToModels(agencyLegacyDocuments, woodlandOwners);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception caught in RetrieveLegacyDocumentsForUserAsync");
            return Result.Failure<IList<LegacyDocumentModel>>(ex.Message);
        }
    }

    public async Task<Result<FileToStoreModel>> RetrieveLegacyDocumentContentAsync(Guid userId, Guid legacyDocumentId, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Request received to retrieve a legacy document file content for user with id {UserId} and document with id {DocumentId}", userId, legacyDocumentId);

        LegacyDocument? entity = null;

        try
        {
            var legacyDocument = await _legacyDocumentsRepository.GetAsync(legacyDocumentId, cancellationToken)
                .ConfigureAwait(false);

            if (legacyDocument.IsFailure)
            {
                _logger.LogError("Could not locate a legacy document with the given id {UserId}", userId);
                return await HandleGetContentFailureAsync(userId, null, cancellationToken).ConfigureAwait(false);
            }

            entity = legacyDocument.Value;

            // firstly verify that the user has access to view the file
            var userAccount = await _userAccountRepository.GetAsync(userId, cancellationToken).ConfigureAwait(false);

            if (userAccount.IsFailure)
            {
                _logger.LogError("Could not locate a user account for the given id {UserId}", userId);
                return await HandleGetContentFailureAsync(userId, entity, cancellationToken).ConfigureAwait(false);
            }

            if (userAccount.Value.WoodlandOwnerId.HasValue)
            {
                _logger.LogDebug("User account with id {UserId} is directly linked to woodland owner", userId);

                if (userAccount.Value.WoodlandOwnerId.Value == entity.WoodlandOwnerId)
                {
                    return await GetDocumentAsync(userId, entity, cancellationToken).ConfigureAwait(false);
                }

                _logger.LogError(
                    "User with id {UserId} is linked to woodland owner id {UserWoodlandOwnerId} but document belongs to woodland owner id {DocumentWoodlandOwnerId}",
                    userId, userAccount.Value.WoodlandOwnerId!, entity.WoodlandOwnerId);
                return await HandleGetContentFailureAsync(userId, entity, cancellationToken).ConfigureAwait(false);
            }

            if (userAccount.Value.AgencyId.HasNoValue())
            {
                _logger.LogError("User account with id {UserId} does not have an agency id or a woodland owner id", userId);
                return await HandleGetContentFailureAsync(userId, entity, cancellationToken).ConfigureAwait(false);
            }

            // fc agent user?
            var fcAgency = await _agencyRepository.FindFcAgency(cancellationToken).ConfigureAwait(false);
            if (fcAgency.HasValue && userAccount.Value.AgencyId == fcAgency.Value.Id)
            {
                _logger.LogDebug("User account for user id {UserId} is part of the FC agency, returning legacy document in storage", userId);
                return await GetDocumentAsync(userId, entity, cancellationToken).ConfigureAwait(false);
            }

            var authority = await _agencyRepository
                .FindAgentAuthorityStatusAsync(userAccount.Value.AgencyId!.Value, entity.WoodlandOwnerId, cancellationToken)
                .ConfigureAwait(false);

            if (authority.HasNoValue)
            {
                _logger.LogError("User account with ID {UserId} at agency with ID {AgencyId} has no authority for woodland owner with id {WoodlandOwnerId}",
                    userId, userAccount.Value.AgencyId.Value, entity.WoodlandOwnerId);
                return await HandleGetContentFailureAsync(userId, entity, cancellationToken).ConfigureAwait(false);
            }

            return await GetDocumentAsync(userId, entity, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception caught in RetrieveLegacyDocumentContentAsync");
            return await HandleGetContentFailureAsync(userId, entity, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<Result<IList<LegacyDocumentModel>>> MapLegacyDocumentsToModels(
        IEnumerable<LegacyDocument> documents, 
        CancellationToken cancellationToken)
    {
        var woodlandOwners = await _woodlandOwnerRepository.GetAllAsync(cancellationToken).ConfigureAwait(false);

        if (woodlandOwners.IsFailure)
        {
            _logger.LogError("Could not retrieve woodland owners in mapping legacy document entities to models");
            return Result.Failure<IList<LegacyDocumentModel>>("Could not retrieve legacy documents");
        }

        return MapLegacyDocumentsToModels(documents, woodlandOwners.Value);
    }

    private Result<IList<LegacyDocumentModel>> MapLegacyDocumentsToModels(
        IEnumerable<LegacyDocument> documents,
        IEnumerable<WoodlandOwner> woodlandOwners)
    {
        try
        {
            IList<LegacyDocumentModel> results = new List<LegacyDocumentModel>();

            foreach (var document in documents)
            {
                var woodlandOwner = woodlandOwners.SingleOrDefault(x => x.Id == document.WoodlandOwnerId);
                var woodlandOwnerName = woodlandOwner?.OrganisationName ?? woodlandOwner?.ContactName;

                results.Add(new LegacyDocumentModel
                {
                    Id = document.Id,
                    DocumentType = document.DocumentType,
                    FileName = document.FileName,
                    FileSize = document.FileSize,
                    FileType = document.FileType,
                    Location = document.Location,
                    MimeType = document.MimeType,
                    WoodlandOwnerId = document.WoodlandOwnerId,
                    WoodlandOwnerName = woodlandOwnerName
                });
            }

            _logger.LogDebug("Returning list of {Count} legacy document models", results.Count);

            return Result.Success(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception caught mapping legacy document entities to models");
            return Result.Failure<IList<LegacyDocumentModel>>(ex.Message);
        }
    }

    private async Task<Result<FileToStoreModel>> GetDocumentAsync(Guid userId, LegacyDocument entity, CancellationToken cancellationToken)
    {
        var getFileResult = await _fileStorageService.GetFileAsync(entity.Location, cancellationToken);

        if (getFileResult.IsFailure)
        {
            _logger.LogError("Could not retrieve document content for file {FileName}", entity.FileName);
            return await HandleGetContentFailureAsync(userId, entity, cancellationToken).ConfigureAwait(false);
        }

        var result = new FileToStoreModel
        {
            ContentType = entity.MimeType,
            FileName = entity.FileName,
            FileBytes = getFileResult.Value.FileBytes
        };

        await _auditService.PublishAuditEventAsync(new AuditEvent(
            AuditEvents.AccessLegacyDocumentEvent,
            entity.WoodlandOwnerId,
            userId,
            _requestContext,
            new
            {
                entity.FileName,
                entity.FileType,
                entity.DocumentType
            }
        ), cancellationToken).ConfigureAwait(false);

        return Result.Success(result);
    }

    private async Task<Result<FileToStoreModel>> HandleGetContentFailureAsync(
        Guid userId, LegacyDocument? entity, CancellationToken cancellationToken)
    {
        await _auditService.PublishAuditEventAsync(new AuditEvent(
            AuditEvents.AccessLegacyDocumentFailureEvent,
            entity?.WoodlandOwnerId,
            userId,
            _requestContext,
            new
            {
                entity?.FileName,
                entity?.FileType,
                entity?.DocumentType
            }
        ), cancellationToken).ConfigureAwait(false);

        return Result.Failure<FileToStoreModel>("Could not access legacy document file content");
    }

}