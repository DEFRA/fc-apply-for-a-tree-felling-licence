using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common.Infrastructure;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.FileStorage.Model;
using Microsoft.Extensions.Options;

namespace Forestry.Flo.Internal.Web.Infrastructure.Fakes;

public class FakeCreateApplicationSnapshotDocumentService
{
    private readonly IFellingLicenceApplicationInternalRepository _fellingLicenceApplicationInternalRepository;
    private readonly ILogger<FakeCreateApplicationSnapshotDocumentService> _logger;
    private readonly IAddDocumentService _addDocumentService;
    private readonly DocumentVisibilityOptions _options;
    private const string _fileName = "Preview_Felling_Licence_0179612021.pdf";

    public FakeCreateApplicationSnapshotDocumentService(
        IFellingLicenceApplicationInternalRepository fellingLicenceApplicationInternalRepository,
        IAddDocumentService addDocumentService,
        ILogger<FakeCreateApplicationSnapshotDocumentService> logger,
        IOptions<DocumentVisibilityOptions> options)
    {
        _fellingLicenceApplicationInternalRepository = Guard.Against.Null(fellingLicenceApplicationInternalRepository);
        _logger = logger;
        _options = Guard.Against.Null(options.Value);
        _addDocumentService = Guard.Against.Null(addDocumentService);
    }

    public async Task<Result<Guid>> CreateApplicationSnapshotAsync(Guid applicationId, Guid userId, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Attempting to add embedded sample document to application with id {ApplicationId}", applicationId);

        var fla = await _fellingLicenceApplicationInternalRepository.GetAsync(applicationId, cancellationToken);

        if (fla.HasNoValue)
        {
            _logger.LogError("Could not retrieve application with id {ApplicationId}", applicationId);
            return Result.Failure<Guid>("Could not generate and attach new application document.");
        }
        
        var fileBytes = await GetSampleFileBytes(cancellationToken);
        
        var fileModel = new FileToStoreModel
        {
            ContentType = "application/pdf", 
            FileBytes = fileBytes,
            FileName = _fileName
        };

        var addDocumentRequest = new AddDocumentsRequest
        {
            ActorType = ActorType.InternalUser,
            ApplicationDocumentCount = fla.Value.Documents!.Count(x => x.DeletionTimestamp is null),
            DocumentPurpose = DocumentPurpose.ApplicationDocument,
            FellingApplicationId = fla.Value.Id,
            FileToStoreModels = new List<FileToStoreModel> { fileModel },
            ReceivedByApi = false,
            UserAccountId = userId,
            VisibleToApplicant = _options.ApplicationDocument.VisibleToApplicant,
            VisibleToConsultee = _options.ApplicationDocument.VisibleToConsultees
        };

        var addDocResult = await _addDocumentService.AddDocumentsAsInternalUserAsync(
            addDocumentRequest,
            cancellationToken);

        if (addDocResult.IsFailure)
        {
            _logger.LogError("Could not add document to application with id {ApplicationId}", applicationId);
            return Result.Failure<Guid>("Could not generate and attach new application document.");
        }

        fla = await _fellingLicenceApplicationInternalRepository.GetAsync(applicationId, cancellationToken);
        var newDoc = fla.Value.Documents
            .OrderByDescending(x => x.CreatedTimestamp)
            .First(x => x.Purpose == DocumentPurpose.ApplicationDocument);

        return Result.Success(newDoc.Id);
    }

    private async Task<byte[]> GetSampleFileBytes(CancellationToken cancellationToken)
    {
        var assembly = typeof(FakeCreateApplicationSnapshotDocumentService).Assembly;
        var resourceName = assembly
            .GetManifestResourceNames()
            .Single(x => x.EndsWith(_fileName));

        await using var stream = assembly.GetManifestResourceStream(resourceName);
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms, cancellationToken);
        return ms.ToArray();
    }
}