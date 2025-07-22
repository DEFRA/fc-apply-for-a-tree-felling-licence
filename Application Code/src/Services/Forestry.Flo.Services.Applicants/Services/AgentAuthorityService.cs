using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Applicants.Entities.AgentAuthority;
using Forestry.Flo.Services.Applicants.Entities.WoodlandOwner;
using Forestry.Flo.Services.Applicants.Models;
using Forestry.Flo.Services.Applicants.Repositories;
using Forestry.Flo.Services.Common.Extensions;
using Forestry.Flo.Services.FileStorage.Model;
using Forestry.Flo.Services.FileStorage.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NodaTime;
using System.IO.Compression;
using Forestry.Flo.Services.Applicants.Entities.UserAccount;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FileStorage.Configuration;
using AgentAuthority = Forestry.Flo.Services.Applicants.Entities.AgentAuthority.AgentAuthority;

namespace Forestry.Flo.Services.Applicants.Services;

/// <summary>
/// Implementation of <see cref="IAgentAuthorityService"/> that uses an <see cref="IAgencyRepository"/>
/// to interact with the database.
/// </summary>
public class AgentAuthorityService: IAgentAuthorityService, IAgentAuthorityInternalService
{
    private readonly IClock _clock;
    private readonly IUserAccountRepository _userAccountRepository;
    private readonly IFileStorageService _storageService;
    private readonly FileTypesProvider _fileTypesProvider;
    private readonly ILogger<AgentAuthorityService> _logger;
    private readonly IAgencyRepository _repository;

    public AgentAuthorityService(
        IAgencyRepository repository, 
        IUserAccountRepository userAccountRepository,
        IFileStorageService storageService,
        FileTypesProvider fileTypesProvider,
        IClock clock,
        ILogger<AgentAuthorityService> logger)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(userAccountRepository);
        ArgumentNullException.ThrowIfNull(storageService);
        ArgumentNullException.ThrowIfNull(fileTypesProvider);
        ArgumentNullException.ThrowIfNull(clock);

        _clock = clock;
        _userAccountRepository = userAccountRepository;
        _storageService = storageService;
        _fileTypesProvider = fileTypesProvider;
        _logger = logger ?? new NullLogger<AgentAuthorityService>();
        _repository = repository;
    }

    /// <inheritdoc />
    public async Task<Result<AddAgentAuthorityResponse>> AddAgentAuthorityAsync(
        AddAgentAuthorityRequest request, 
        CancellationToken cancellationToken)
    {
        Guard.Against.Null(request);
        _logger.LogDebug("Received request to add a new Agent Authority entry in the system from user with id {UserId}", request.CreatedByUser);

        try
        {
            var user = await _userAccountRepository.GetAsync(request.CreatedByUser, cancellationToken);
            
            if (user.IsFailure)
            {
                _logger.LogError("Could not retrieve user account for user id {UserId}", request.CreatedByUser);
                return Result.Failure<AddAgentAuthorityResponse>(user.Error.ToString());
            }

            var agency = await _repository.GetAsync(request.AgencyId, cancellationToken);

            if (agency.IsFailure)
            {
                _logger.LogError("Could not retrieve agency for agency id {agencyId}", request.AgencyId);
                return Result.Failure<AddAgentAuthorityResponse>(agency.Error.ToString());
            }

            var entity = new AgentAuthority
            {
                Agency = agency.Value,
                CreatedTimestamp = _clock.GetCurrentInstant().ToDateTimeUtc(),
                CreatedByUser = user.Value,
                Status = AgentAuthorityStatus.Created,
                WoodlandOwner = new WoodlandOwner
                {
                    ContactAddress = request.WoodlandOwner.ContactAddress,
                    ContactName = request.WoodlandOwner.ContactName,
                    ContactEmail = request.WoodlandOwner.ContactEmail,
                    ContactTelephone = request.WoodlandOwner.ContactTelephone,
                    IsOrganisation = request.WoodlandOwner.IsOrganisation,
                    OrganisationAddress = request.WoodlandOwner.OrganisationAddress,
                    OrganisationName = request.WoodlandOwner.OrganisationName
                }
            };

            var saveToDbResult = await _repository.AddAgentAuthorityAsync(entity, cancellationToken);

            if (saveToDbResult.IsFailure)
            {
                _logger.LogError("Could not save Agent Authority entity to database, error {Error}", saveToDbResult.Error);
                return Result.Failure<AddAgentAuthorityResponse>(saveToDbResult.Error.ToString());
            }

            var result = new AddAgentAuthorityResponse
            {
                AgentAuthorityId = saveToDbResult.Value.Id,
                AgencyName = agency.Value!.OrganisationName,
                AgencyId = agency.Value.Id
            };

            return Result.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception caught in AddAgentAuthorityAsync");
            return Result.Failure<AddAgentAuthorityResponse>(ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<Result> DeleteAgentAuthorityAsync(Guid agentAuthorityId, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Received request to delete agent authority with id {AgentAuthorityId}", agentAuthorityId);
        try
        {
            var deleteResult = await _repository.DeleteAgentAuthorityAsync(agentAuthorityId, cancellationToken);

            if (deleteResult.IsFailure)
            {
                _logger.LogError("Could not delete Agent Authority with id {AgentAuthorityId}, error: {Error}", agentAuthorityId, deleteResult.Error);
                return Result.Failure("Could not delete agent authority");
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception caught in DeleteAgentAuthorityAsync");
            return Result.Failure(ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<Result<AgentAuthorityFormsWithWoodlandOwnerResponseModel>> GetAgentAuthorityFormDocumentsByAuthorityIdAsync(
        Guid userId,
        Guid agentAuthorityId,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "Received request to retrieve an agent authority entry having id {agentAuthorityId} in the system for user with id {UserId}",
            agentAuthorityId, userId);

        try
        {
            var user = await _userAccountRepository.GetAsync(userId, cancellationToken);
       
            if (user.IsFailure)
            {
                _logger.LogError("Could not retrieve user account for user id {UserId}", userId);
                return Result.Failure<AgentAuthorityFormsWithWoodlandOwnerResponseModel>(user.Error.ToString());
            }

            var isFcUser = user.Value.AccountType == AccountTypeExternal.FcUser;

            if (!isFcUser && user.Value.Agency is null)
            {
                _logger.LogError("User account with id {UserId} is not associated with an agency", userId);
                return Result.Failure<AgentAuthorityFormsWithWoodlandOwnerResponseModel>("User not associated with an agency");
            }

            //todo - This isn't going to work when an FC user does this they should be able to view the data for other agencies.
            //See FLOV2-1218

            var authorityFormsEntity =
                await _repository.GetAgentAuthorityAsync(agentAuthorityId, cancellationToken);

            if (authorityFormsEntity.IsFailure)
            {
                _logger.LogWarning("No agent authority forms found for agency id {agencyId} and authorityForm id {authorityFormId}",
                user.Value.Agency.Id, agentAuthorityId);

                return Result.Failure<AgentAuthorityFormsWithWoodlandOwnerResponseModel>("Could not find authority form");
            }
            
            if (!isFcUser && authorityFormsEntity.Value.Agency.Id != user.Value.AgencyId)
            {
                _logger.LogWarning("User agency {userAgencyId} does not match agency id on agent authority {agentAuthorityAgencyId}",
                    user.Value.Agency.Id, authorityFormsEntity.Value.Agency.Id);
                return Result.Failure<AgentAuthorityFormsWithWoodlandOwnerResponseModel>("Could not find authority form");
            }

            var authority = authorityFormsEntity.Value;

            var responseModel = authority.AgentAuthorityForms.Select( x=> new AgentAuthorityFormResponseModel
            {
                Id = x.Id,
                ValidToDate = x.ValidToDate,
                ValidFromDate = x.ValidFromDate,
                AafDocuments = x.AafDocuments.Select(aafDoc => new AafDocumentResponseModel
                {
                    FileName = aafDoc.FileName,
                    FileSize = aafDoc.FileSize,
                    FileType = aafDoc.FileType,
                    Location = aafDoc.Location,
                    MimeType = aafDoc.MimeType
                }).ToList()
            });

            var woodlandOwnerModel = new WoodlandOwnerModel
            {
                Id = authority.WoodlandOwner.Id,
                ContactName = authority.WoodlandOwner.ContactName,
                ContactAddress = authority.WoodlandOwner.ContactAddress,
                ContactEmail = authority.WoodlandOwner.ContactEmail,
                ContactTelephone = authority.WoodlandOwner.ContactTelephone,
                OrganisationName = authority.WoodlandOwner.OrganisationName,
                OrganisationAddress = authority.WoodlandOwner.OrganisationAddress,
                IsOrganisation = authority.WoodlandOwner.IsOrganisation
            };

            var result = new AgentAuthorityFormsWithWoodlandOwnerResponseModel
            {
                AgencyId = authority.Agency.Id,
                WoodlandOwnerModel = woodlandOwnerModel,
                AgentAuthorityFormResponseModels = responseModel.ToList()
            };

            return Result.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception caught while trying to retrieve agent authority form, having id of {agentAuthorityId}", agentAuthorityId);
            return Result.Failure<AgentAuthorityFormsWithWoodlandOwnerResponseModel>("Error occurred when retrieving agent authority form.");
        }
    }

    /// <inheritdoc />
    public async Task<Result<GetAgentAuthoritiesResponse>> GetAgentAuthoritiesAsync(
        Guid userId,
        Guid agencyId,
        AgentAuthorityStatus[]? filter,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Received request to retrieve all Agent Authority entries in the system for user with id {UserId}", userId);

        try
        {
            var user = await _userAccountRepository.GetAsync(userId, cancellationToken);

            if (user.IsFailure)
            {
                _logger.LogError("Could not retrieve user account for user id {UserId}", userId);
                return Result.Failure<GetAgentAuthoritiesResponse>(user.Error.ToString());
            }

            if (user.Value.AccountType != AccountTypeExternal.FcUser
                && user.Value.Agency?.Id != agencyId)
            {
                _logger.LogError("User account with id {UserId} is not associated with agency with id {AgencyId}", userId, agencyId);
                return Result.Failure<GetAgentAuthoritiesResponse>("User not associated with correct agency");
            }

            var authorityEntities =
                await _repository.ListAuthoritiesByAgencyAsync(agencyId, filter, cancellationToken);

            if (authorityEntities.IsFailure)
            {
                _logger.LogError("Could not retrieve agent authorities for agency id {AgencyId}", agencyId);
                return Result.Failure<GetAgentAuthoritiesResponse>(authorityEntities.Error.ToString());
            }

            var results = authorityEntities.Value.Select(x => new AgentAuthorityModel
            {
                Id = x.Id,
                CreatedByName = x.CreatedByUser.FullName(),
                CreatedTimestamp = x.CreatedTimestamp,
                Status = x.Status,
                WoodlandOwner = new WoodlandOwnerModel
                {
                    Id = x.WoodlandOwner.Id,
                    ContactName = x.WoodlandOwner.ContactName,
                    ContactAddress = x.WoodlandOwner.ContactAddress,
                    ContactEmail = x.WoodlandOwner.ContactEmail,
                    ContactTelephone = x.WoodlandOwner.ContactTelephone,
                    OrganisationName = x.WoodlandOwner.OrganisationName,
                    OrganisationAddress = x.WoodlandOwner.OrganisationAddress,
                    IsOrganisation = x.WoodlandOwner.IsOrganisation
                },
                AgentAuthorityForms = x.AgentAuthorityForms.Select(form => new AgentAuthorityFormResponseModel
                {
                    ValidToDate = form.ValidToDate,
                    ValidFromDate = form.ValidFromDate,
                    AafDocuments = form.AafDocuments.Select(aafDoc => new AafDocumentResponseModel
                    {
                        FileName = aafDoc.FileName,
                        FileSize = aafDoc.FileSize,
                        FileType = aafDoc.FileType,
                        Location = aafDoc.Location,
                        MimeType = aafDoc.MimeType
                    }).ToList()
                }).ToList()
            });

            return Result.Success(new GetAgentAuthoritiesResponse { AgentAuthorities = results.ToList() });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception caught in GetAgentAuthoritiesAsync");
            return Result.Failure<GetAgentAuthoritiesResponse>(ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<Result<AgentAuthorityFormResponseModel>> AddAgentAuthorityFormAsync(
        AddAgentAuthorityFormRequest request, 
        CancellationToken cancellationToken)
    {
        AgentAuthorityForm? newAaf = null;

        _logger.LogDebug(
            "Received request to add a new Agent Authority form to Agent Authority with id {AgentAuthorityId} by user with id {UserId}",
            request.AgentAuthorityId, request.UploadedByUser);

        try
        {
            var now = _clock.GetCurrentInstant().ToDateTimeUtc();

            var user = await _userAccountRepository.GetAsync(request.UploadedByUser, cancellationToken);

            if (user.IsFailure)
            {
                _logger.LogError("Could not retrieve user account for user id {UserId}", request.UploadedByUser);
                return Result.Failure<AgentAuthorityFormResponseModel>(user.Error.ToString());
            }

            var agentAuthority = await _repository.GetAgentAuthorityAsync(request.AgentAuthorityId, cancellationToken);

            if (agentAuthority.IsFailure)
            {
                _logger.LogWarning("Could not locate AgentAuthority with ID {AgentAuthorityId} to upload an AAF document", request.AgentAuthorityId);
                return Result.Failure<AgentAuthorityFormResponseModel>("Could not locate Agent Authority to upload AAF");
            }

            if (agentAuthority.Value.Status == AgentAuthorityStatus.Deactivated)
            {
                _logger.LogWarning("Agent Authority with ID {AgentAuthorityId} status is Deactivated, no AAF document can be uploaded to it", request.AgentAuthorityId);
                return Result.Failure<AgentAuthorityFormResponseModel>("Cannot upload AAF documents to a deactivated Agent Authority");
            }

            if (agentAuthority.Value.Agency.Id != user.Value.AgencyId && user.Value.Agency?.IsFcAgency is not true)
            {
                _logger.LogWarning(
                    "User with ID {UserId} uploading AAF document is not associated with the Agency of the Agent Authority with ID {AgentAuthorityId}", 
                    request.UploadedByUser, request.AgentAuthorityId);
                return Result.Failure<AgentAuthorityFormResponseModel>(
                    "Current user does not have access to upload AAF document to the specified Agent Authority");
            }

            // if there is already a valid AAF on the AA then set it's ValidToDate
            var existingAaf = agentAuthority.Value.AgentAuthorityForms
                .SingleOrDefault(x => x.ValidToDate.HasNoValue());
            if (existingAaf != null)
            {
                existingAaf.ValidToDate = now;
            }

            // add the new AAF entity onto the AA entity
            newAaf = new AgentAuthorityForm
            {
                AgentAuthorityId = agentAuthority.Value.Id,
                UploadedBy = user.Value,
                ValidFromDate = now,
                AafDocuments = new List<AafDocument>()
            };
            agentAuthority.Value.AgentAuthorityForms.Add(newAaf);
            agentAuthority.Value.Status = AgentAuthorityStatus.FormUploaded;
            agentAuthority.Value.ChangedByUser = user.Value;
            agentAuthority.Value.ChangedTimestamp = now;

            // upload the document files to File Storage and add them to the AAF entity
            var storedLocationPath = Path.Combine(agentAuthority.Value.Id.ToString(), "AAF_document");
            foreach (var file in request.AafDocuments)
            {
                _logger.LogDebug("Attempting to store AAF document with filename {FileName}", file.FileName);

                var (isSuccess, _, savedFile, error) = await _storageService.StoreFileAsync(
                    file.FileName,
                    file.FileBytes,
                    storedLocationPath,
                    false,
                    FileUploadReason.AgentAuthorityForm,
                    cancellationToken);

                if (isSuccess)
                {
                    newAaf.AafDocuments.Add(new AafDocument
                    {
                        FileName = file.FileName,
                        FileSize = savedFile.FileSize,
                        FileType = _fileTypesProvider.FindFileTypeByMimeTypeWithFallback(file.ContentType).Extension,
                        MimeType = file.ContentType,
                        Location = savedFile.Location
                    });
                }
                else
                {
                    _logger.LogError("Could not store AAF document with filename {FileName}, error: {Error}", file.FileName, error);
                    await RemoveStoredFilesAsync();
                    return Result.Failure<AgentAuthorityFormResponseModel>("Could not store the provided AAF documents");
                }
            }

            var saveResult = await _repository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

            if (saveResult.IsFailure)
            {
                _logger.LogError("Could not save changes to database, error: {Error}", saveResult.Error);
                await RemoveStoredFilesAsync();
                return Result.Failure<AgentAuthorityFormResponseModel>("Could not store provided AAF documents");
            }

            var responseModel = new AgentAuthorityFormResponseModel
            {
                Id = newAaf.Id,
                ValidFromDate = newAaf.ValidFromDate,
                AafDocuments = newAaf.AafDocuments.Select(x => new AafDocumentResponseModel
                {
                    FileName = x.FileName,
                    FileSize = x.FileSize,
                    Location = x.Location,
                    FileType = x.FileType,
                    MimeType = x.MimeType
                }).ToList()
            };

            return Result.Success(responseModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception caught in AddAgentAuthorityFormAsync");
            await RemoveStoredFilesAsync();
            return Result.Failure<AgentAuthorityFormResponseModel>(ex.Message);
        }

        async Task RemoveStoredFilesAsync()
        {
            if (newAaf == null) return;

            //remove any of the files that we did manage to store
            foreach (var savedFileLocation in newAaf.AafDocuments.Select(x => x.Location))
            {
                var removeResult = await _storageService.RemoveFileAsync(savedFileLocation!, cancellationToken);
                if (removeResult.IsFailure)
                {
                    _logger.LogError("Could not remove file {FileLocation}", savedFileLocation);
                }
            }
        }
    }

    /// <inheritdoc />
    public async Task<Result> RemoveAgentAuthorityFormAsync(
        RemoveAgentAuthorityFormRequest request, 
        CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "Received request to remove Agent Authority form with id {AgentAuthorityFormId} from Agent Authority with id {AgentAuthorityId} by user with id {UserId}",
            request.AgentAuthorityFormId, request.AgentAuthorityId, request.RemovedByUser);

        try
        {
            var now = _clock.GetCurrentInstant().ToDateTimeUtc();

            var user = await _userAccountRepository.GetAsync(request.RemovedByUser, cancellationToken);

            if (user.IsFailure)
            {
                _logger.LogError("Could not retrieve user account for user id {UserId}", request.RemovedByUser);
                return Result.Failure(user.Error.ToString());
            }

            var agentAuthority = await _repository.GetAgentAuthorityAsync(request.AgentAuthorityId, cancellationToken);

            if (agentAuthority.IsFailure)
            {
                _logger.LogWarning("Could not locate AgentAuthority with ID {AgentAuthorityId} to remove an AAF document", request.AgentAuthorityId);
                return Result.Failure("Could not locate Agent Authority to remove AAF");
            }

            if (agentAuthority.Value.Status == AgentAuthorityStatus.Deactivated)
            {
                _logger.LogWarning("Agent Authority with ID {AgentAuthorityId} status is Deactivated, no AAF document can be removed from it", request.AgentAuthorityId);
                return Result.Failure("Cannot remove AAF documents from a deactivated Agent Authority");
            }

            if (agentAuthority.Value.Agency.Id != user.Value.AgencyId && user.Value.Agency?.IsFcAgency is not true)
            {
                _logger.LogWarning(
                    "User with ID {UserId} removing AAF document is not associated with the Agency of the Agent Authority with ID {AgentAuthorityId}",
                    request.RemovedByUser, request.AgentAuthorityId);
                return Result.Failure("Current user does not have access to remove AAF document to the specified Agent Authority");
            }

            var existingAaf =
                agentAuthority.Value.AgentAuthorityForms.SingleOrDefault(x => x.Id == request.AgentAuthorityFormId);

            if (existingAaf == null)
            {
                _logger.LogWarning("Could not find Agent Authority Form with ID {AgentAuthorityFormId} on Agent Authority with ID {AgentAuthorityId}",
                    request.AgentAuthorityFormId, request.AgentAuthorityId);
                return Result.Failure("No agent authority form found with provided id on the agent authority with the provided id");
            }

            existingAaf.ValidToDate = now;
            
            agentAuthority.Value.ChangedByUser = user.Value;
            agentAuthority.Value.ChangedTimestamp = now;

            if (agentAuthority.Value.AgentAuthorityForms.NotAny(x => x.ValidToDate.HasNoValue()))
            {
                _logger.LogDebug("No valid AAFs remain on Agent Authority with ID {AgentAuthorityId}, resetting status to Created", agentAuthority.Value.Id);
                agentAuthority.Value.Status = AgentAuthorityStatus.Created;
            }

            var saveResult = await _repository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

            if (saveResult.IsFailure)
            {
                _logger.LogError("Could not save changes to database, error: {Error}", saveResult.Error);
                return Result.Failure("Could not remove AAF with given ID");
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception caught in RemoveAgentAuthorityFormAsync");
            return Result.Failure(ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<Result<FileToStoreModel>> GetAgentAuthorityFormDocumentsAsync(
        GetAgentAuthorityFormDocumentsRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "Received request to retrieve the files for Agent Authority form with id {AgentAuthorityFormId} from Agent Authority with id {AgentAuthorityId} by user with id {UserId}",
            request.AgentAuthorityFormId, request.AgentAuthorityId, request.AccessedByUser);

        var user = await _userAccountRepository.GetAsync(request.AccessedByUser, cancellationToken);

        if (user.IsFailure)
        {
            _logger.LogError("Could not retrieve user account for user id {UserId}", request.AccessedByUser);
            return Result.Failure<FileToStoreModel>(user.Error.ToString());
        }

        return await GetAgentAuthorityFormDocumentsAsync(
            request.AgentAuthorityId, request.AgentAuthorityFormId, AgentAuthorityValidationFunc, cancellationToken);

        Result AgentAuthorityValidationFunc(AgentAuthority agentAuthority)
        {
            if (agentAuthority.Status == AgentAuthorityStatus.Deactivated)
            {
                _logger.LogWarning("Agent Authority with ID {AgentAuthorityId} status is Deactivated, no AAF document can be retrieved from it", agentAuthority.Id);
                return Result.Failure("Cannot retrieve AAF documents from a deactivated Agent Authority");
            }

            if (agentAuthority.Agency.Id != user.Value.AgencyId && user.Value.Agency?.IsFcAgency is not true)
            {
                _logger.LogWarning(
                    "User with ID {UserId} retrieving AAF document is not associated with the Agency of the Agent Authority with ID {AgentAuthorityId}",
                    user.Value.Id, agentAuthority.Id);
                return Result.Failure("Current user does not have access to retrieve AAF document for the specified Agent Authority");
            }

            return Result.Success();
        }
    }
    
    /// <inheritdoc />
    public async Task<Result<FileToStoreModel>> GetAgentAuthorityFormDocumentsAsync(
        GetAgentAuthorityFormDocumentsInternalRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "Received request to retrieve the files for Agent Authority form with id {AgentAuthorityFormId} from Agent Authority with id {AgentAuthorityId} from the internal app",
            request.AgentAuthorityFormId, request.AgentAuthorityId);

        return await GetAgentAuthorityFormDocumentsAsync(
            request.AgentAuthorityId, request.AgentAuthorityFormId, _ => Result.Success(), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result<GetAgentAuthorityFormResponse>> GetAgentAuthorityFormAsync(
        GetAgentAuthorityFormRequest request, 
        CancellationToken cancellationToken)
    {
        try
        {
            var entity = await _repository.FindAgentAuthorityAsync(
                request.AgencyId, request.WoodlandOwnerId, cancellationToken);

            if (entity.HasNoValue)
            {
                return Result.Failure<GetAgentAuthorityFormResponse>(
                    "Could not find an Agent Authority for the given agency and woodland owner ids");
            }

            var timeStamp = request.PointInTime ?? _clock.GetCurrentInstant().ToDateTimeUtc();

            var pointInTimeAaf = entity.Value.AgentAuthorityForms.OrderBy(x => x.ValidFromDate)
                .Select(a => new AgentAuthorityFormDetailsModel(a.Id, a.ValidFromDate, a.ValidToDate))
                .FirstOrDefault(a => a.ValidFromDate <= timeStamp && (a.ValidToDate.HasNoValue() || a.ValidToDate.Value >= timeStamp));
            var currentAaf = entity.Value.AgentAuthorityForms.OrderBy(x => x.ValidFromDate)
                .Select(a => new AgentAuthorityFormDetailsModel(a.Id, a.ValidFromDate, a.ValidToDate))
                .FirstOrDefault(x => x.ValidToDate.HasNoValue());

            var result = new GetAgentAuthorityFormResponse
            {
                AgentAuthorityId = entity.Value.Id,
                CurrentAgentAuthorityForm = currentAaf.AsMaybe(),
                SpecificTimestampAgentAuthorityForm = pointInTimeAaf.AsMaybe()
            };

            return Result.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception caught in GetAgentAuthorityFormAsync");
            return Result.Failure<GetAgentAuthorityFormResponse>(ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<Result<bool>> EnsureAgencyAuthorityStatusAsync(
        EnsureAgencyOwnsWoodlandOwnerRequest request,
        AgentAuthorityStatus[] validStatuses,
        CancellationToken cancellationToken)
    {
        try
        {
            Guard.Against.Null(request);
            _logger.LogDebug("Received request to validate that agency with id {AgencyId} has an approved AAF for woodland owner with id {WoodlandOwnerId}",
                request.AgencyId, request.WoodlandOwnerId);

            var status = await _repository.FindAgentAuthorityStatusAsync(
                request.AgencyId, 
                request.WoodlandOwnerId,
                cancellationToken);

            if (status.HasValue && validStatuses.Contains(status.Value))
            {
                return Result.Success(true);
            }

            return Result.Success(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception caught in EnsureAgencyManagesWoodlandOwnerAsync");
            return Result.Failure<bool>(ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<Maybe<AgencyModel>> GetAgencyForWoodlandOwnerAsync(
        Guid woodlandOwnerId, 
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Received request to look up agency for woodland owner with id {WoodlandOwnerId}", woodlandOwnerId);

            var authority = await _repository.GetActiveAuthorityByWoodlandOwnerIdAsync(woodlandOwnerId, cancellationToken);

            if (authority.HasNoValue)
            {
                return Maybe<AgencyModel>.None;
            }

            var result = new AgencyModel
            {
                AgencyId = authority.Value.Agency.Id,
                ContactName = authority.Value.Agency.ContactName,
                OrganisationName = authority.Value.Agency.OrganisationName,
                ContactEmail = authority.Value.Agency.ContactEmail,
                Address = authority.Value.Agency.Address,
                IsFcAgency = authority.Value.Agency.IsFcAgency
            };
            return Maybe<AgencyModel>.From(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception caught in GetAgencyForWoodlandOwnerAsync");
            return Maybe<AgencyModel>.None;
        }
    }

    private async Task<Result<FileToStoreModel>> GetAgentAuthorityFormDocumentsAsync(
        Guid agentAuthorityId,
        Guid agentAuthorityFormId,
        Func<AgentAuthority, Result> validationFunction,
        CancellationToken cancellationToken)
    {
        try
        {
            var agentAuthority = await _repository.GetAgentAuthorityAsync(agentAuthorityId, cancellationToken);

            if (agentAuthority.IsFailure)
            {
                _logger.LogWarning("Could not locate AgentAuthority with ID {AgentAuthorityId} to access an AAF document", agentAuthorityId);
                return Result.Failure<FileToStoreModel>("Could not locate Agent Authority to access AAF");
            }

            var validationResult = validationFunction(agentAuthority.Value);
            if (validationResult.IsFailure)
            {
                return validationResult.ConvertFailure<FileToStoreModel>();
            }

            var existingAaf =
                agentAuthority.Value.AgentAuthorityForms.SingleOrDefault(x => x.Id == agentAuthorityFormId);

            if (existingAaf == null)
            {
                _logger.LogWarning("Could not find Agent Authority Form with ID {AgentAuthorityFormId} on Agent Authority with ID {AgentAuthorityId}",
                    agentAuthorityFormId, agentAuthorityId);
                return Result.Failure<FileToStoreModel>("No agent authority form found with provided id on the agent authority with the provided id");
            }

            if (existingAaf.AafDocuments.Count == 1)
            {
                var document = existingAaf.AafDocuments.Single();
                var getFileResult = await _storageService.GetFileAsync(document.Location, cancellationToken);
                if (getFileResult.IsFailure)
                {
                    _logger.LogWarning("Could not retrieve file contents for AAF document with ID {AafDocumentID}", document.Id);
                    return Result.Failure<FileToStoreModel>("Could not retrieve file contents from storage service");
                }

                var result = new FileToStoreModel
                {
                    ContentType = document.MimeType,
                    FileName = document.FileName,
                    FileBytes = getFileResult.Value.FileBytes
                };
                return Result.Success(result);
            }

            using var compressedFileStream = new MemoryStream();
            using (var zipArchive = new ZipArchive(compressedFileStream, ZipArchiveMode.Create, false))
            {
                foreach (var aafDocument in existingAaf.AafDocuments)
                {
                    var getFileResult = await _storageService.GetFileAsync(aafDocument.Location, cancellationToken);
                    if (getFileResult.IsFailure)
                    {
                        _logger.LogWarning("Could not retrieve file contents for AAF document with ID {AafDocumentID}", aafDocument.Id);
                        return Result.Failure<FileToStoreModel>("Could not retrieve file contents from storage service");
                    }

                    var zipEntry = zipArchive.CreateEntry(aafDocument.FileName);
                    using var originalFileStream = new MemoryStream(getFileResult.Value.FileBytes);
                    await using var zipEntryStream = zipEntry.Open();
                    await originalFileStream.CopyToAsync(zipEntryStream, cancellationToken);
                }
            }

            var zipFileResult = new FileToStoreModel
            {
                ContentType = "application/zip",
                FileName = "AAF Document.zip",
                FileBytes = compressedFileStream.ToArray()
            };

            return Result.Success(zipFileResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception caught in GetAgentAuthorityFormDocumentsAsync");
            return Result.Failure<FileToStoreModel>(ex.Message);
        }
    }

    ///<inheritdoc />
    public async Task<Result<AgencyModel>> GetAgencyAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var (_, isFailure, agency, error) = await _repository.GetAsync(id, cancellationToken);

        if (isFailure)
        {
            _logger.LogError("Unable to retrieve agency due to error: {error}", error);
            return Result.Failure<AgencyModel>("Unable to retrieve agency");
        }

        return new AgencyModel
        {
            Address = agency.Address,
            ContactEmail = agency.ContactEmail,
            ContactName = agency.ContactName,
            OrganisationName = agency.OrganisationName,
            AgencyId = agency.Id,
            IsFcAgency = agency.IsFcAgency
        };
    }

    ///<inheritdoc />
    public async Task<Result> RemoveNewAgencyAsync(
        UserAccount userAccount,
        CancellationToken cancellationToken)
    {
        if (userAccount is
            {
                DateAcceptedPrivacyPolicy: not null, 
                DateAcceptedTermsAndConditions: not null
            })
        {
            _logger.LogWarning("Unable to remove agency for account that has completed registration");
            return Result.Failure("Unable to remove agency for account that has completed registration");
        }

        if (userAccount.Agency is null)
        {
            _logger.LogError("User account {id} has no associated agency to remove", userAccount.Id);
            return Result.Failure("User account has no associated agency to remove");
        }

        var agencyId = userAccount.AgencyId!.Value;

        userAccount.Agency = null;
        userAccount.AgencyId = null;
        userAccount.LastChanged = _clock.GetCurrentInstant().ToDateTimeUtc();

        var deletionResult = await _repository.DeleteAgencyAsync(agencyId, cancellationToken);

        if (deletionResult.IsFailure)
        {
            _logger.LogError("Unable to delete agency {id}, error: {error}", agencyId, deletionResult.Error);
            return Result.Failure("Unable to delete agency");
        }

        await _repository.UnitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}