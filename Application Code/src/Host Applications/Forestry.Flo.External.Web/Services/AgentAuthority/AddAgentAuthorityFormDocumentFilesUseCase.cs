using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Applicants.Models;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.FileStorage.Configuration;
using Forestry.Flo.Services.FileStorage.Services;
using Microsoft.Extensions.Options;

namespace Forestry.Flo.External.Web.Services.AgentAuthority;

/// <summary>
/// Handles the use case for adding files to an agent authority form.
/// </summary>
public class AddAgentAuthorityFormDocumentFilesUseCase
{
    private readonly IAgentAuthorityService _agentAuthorityService;
    private readonly ILogger<AddAgentAuthorityFormDocumentFilesUseCase> _logger;
    private readonly UserFileUploadOptions _userFileUploadOptions;
    private readonly FileTypesProvider _fileTypesProvider;
    private readonly IAuditService<AddAgentAuthorityFormDocumentFilesUseCase> _audit;
    private readonly RequestContext _requestContext;

    /// <summary>
    /// Use case for adding a new agent authority form to
    /// an existing agent authority.
    /// </summary>
    public AddAgentAuthorityFormDocumentFilesUseCase(
        IAgentAuthorityService agentAuthorityService,
        IOptions<UserFileUploadOptions> userFileUploadOptions,
        FileTypesProvider fileTypesProvider,
        IAuditService<AddAgentAuthorityFormDocumentFilesUseCase> audit,
        RequestContext requestContext,
        ILogger<AddAgentAuthorityFormDocumentFilesUseCase> logger)
    {
        _agentAuthorityService = Guard.Against.Null(agentAuthorityService);
        _userFileUploadOptions = Guard.Against.Null(userFileUploadOptions).Value;
        _fileTypesProvider = Guard.Against.Null(fileTypesProvider);
        _audit = Guard.Against.Null(audit);
        _requestContext = Guard.Against.Null(requestContext);
        _logger = logger;
    }

    /// <summary>
    /// Adds a new agent authority form to an existing agent authority.
    /// </summary>
    /// <param name="user">The user performing the request.</param>
    /// <param name="agentAuthorityId">The id of the agent authority to add the document files to.</param>
    /// <param name="agentAuthorityDocumentFiles">The files to be added.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>Returns a <see cref="Result"/> indicating success or failure of this action.</returns>
    public async Task<Result> AddAgentAuthorityFormDocumentFilesAsync(
        ExternalApplicant user,
        Guid agentAuthorityId,
        FormFileCollection agentAuthorityDocumentFiles,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Attempting to add {fileCount} files for the agent authority form document, for user with id {UserId} and " +
                         " agent authority of {agentAuthorityId} for agency id {agency}",
            agentAuthorityDocumentFiles.Count, user.UserAccountId, agentAuthorityId, user.AgencyId);

        var fileValidationResult = ValidateFiles(user, agentAuthorityId, agentAuthorityDocumentFiles);

        if (fileValidationResult.IsFailure)
        {
            _logger.LogWarning("Failed to validate the collection of files being uploaded for an AAF document");

            await _audit.PublishAuditEventAsync(
                new AuditEvent(
                    AuditEvents.AddAgentAuthorityFormFilesValidationFailure,
                    new Guid(user.AgencyId!),
                    user.UserAccountId,
                    _requestContext,
                    new
                    {
                        user.WoodlandOwnerName,
                        user.FullName,
                        agentAuthorityId,
                        uploadedByUser = user.UserAccountId!.Value,
                        fileCount = agentAuthorityDocumentFiles.Count,
                        fileNames = string.Join(", ", agentAuthorityDocumentFiles.Select(f => f.FileName)),
                        fileSizes = string.Join(", ", agentAuthorityDocumentFiles.Select(f => f.Length))
                    }
                ),
                cancellationToken);

            return Result.Failure("Unable to successfully validate the files for upload");
        }

        var request = new AddAgentAuthorityFormRequest
        {
            AgentAuthorityId = agentAuthorityId, 
            AafDocuments = ModelMapping.ToFileToStoreModel(agentAuthorityDocumentFiles),
            UploadedByUser = user.UserAccountId!.Value
        };

        var addAgentAuthorityFormFiles = await _agentAuthorityService.AddAgentAuthorityFormAsync(
                    request, 
                    cancellationToken)
                .ConfigureAwait(false);

        if (addAgentAuthorityFormFiles.IsFailure)
        {
            await _audit.PublishAuditEventAsync(
                new AuditEvent(
                    AuditEvents.AddAgentAuthorityFormFilesFailure,
                    new Guid(user.AgencyId!),
                    user.UserAccountId,
                    _requestContext,
                    new
                    {
                        user.WoodlandOwnerName,
                        user.FullName,
                        agentAuthorityId = request.AgentAuthorityId,
                        uploadedByUser = request.UploadedByUser,
                        fileCount = request.AafDocuments.Count,
                        fileNames = string.Join(", ", request.AafDocuments.Select(f => f.FileName))
                    }
                ),
                cancellationToken);

            return Result.Failure($"Could not add new agent authority form document files, error: {addAgentAuthorityFormFiles.Error}");
        }

        await _audit.PublishAuditEventAsync(
            new AuditEvent(
                AuditEvents.AddAgentAuthorityFormFiles,
                new Guid(user.AgencyId!),
                user.UserAccountId,
                _requestContext,
                new
                {
                    user.WoodlandOwnerName,
                    user.FullName,
                    agentAuthorityId = request.AgentAuthorityId,
                    newAgentAuthorityFormId = addAgentAuthorityFormFiles.Value.Id,
                    uploadedByUser = request.UploadedByUser,
                    fileCount = request.AafDocuments.Count,
                    fileNames = string.Join(", ", request.AafDocuments.Select(f => f.FileName))
                }
            ),
            cancellationToken);

        return Result.Success();
    }

    private Result ValidateFiles(
       ExternalApplicant user,
       Guid agentAuthorityId,
       FormFileCollection formFileCollection)
    {
        if (!formFileCollection.Any())
        {
            _logger.LogDebug("No files were submitted for the agent authority form document, for user with id {UserId} and " +
                             " agent authority of {agentAuthorityId} for agency id {agency}", user.UserAccountId, agentAuthorityId, user.AgencyId);

            return Result.Failure("No files found in file collection");
        }

        if (formFileCollection.Count > _userFileUploadOptions.MaxNumberDocuments)
        {
            _logger.LogWarning("The collection of files submitted by user with id {UserId} and " +
                               "agent authority of {agentAuthorityId} for agency id {agency} contains too many files {fileCount} for upload based on configured settings.",
                 user.UserAccountId, agentAuthorityId, user.AgencyId, formFileCollection.Count);

            return Result.Failure("File collection is too large.");
        }

        foreach (var documentFile in formFileCollection)
        {
            if (documentFile.Length > _userFileUploadOptions.MaxFileSizeBytes)
            {
                _logger.LogWarning("The file submitted with name {fileName} having file size of {fileSize}, by user with id {UserId} and " +
                "agent authority of {agentAuthorityId} for agency id {agency} is too large for upload based on configured settings",
                    documentFile.FileName, documentFile.Length, user.UserAccountId, agentAuthorityId, user.AgencyId);

                return Result.Failure("A file that is too large found in file collection");
            }

            var contentType = documentFile.ContentType;
            var result = _fileTypesProvider.FindFileTypeByMimeTypeWithFallback(contentType).Extension.ToUpperInvariant().Replace(".", "");
            var isValidFileType = _userFileUploadOptions.AllowedFileTypes
                .Any(allowedFileType => allowedFileType.Extensions.Contains(result) && allowedFileType.FileUploadReasons.Contains(FileUploadReason.AgentAuthorityForm));

            if (isValidFileType) continue;

            _logger.LogWarning("The file submitted with name {fileName} and content-type {fileContentType}, by user with id {UserId} and " +
                             "agent authority of {agentAuthorityId} for agency id {agency} is not a permitted file type for upload based on configured settings",
                documentFile.FileName, contentType, user.UserAccountId, agentAuthorityId, user.AgencyId);

            return Result.Failure("An invalid file type found in file collection.");
        }

        return Result.Success();
    }
}
