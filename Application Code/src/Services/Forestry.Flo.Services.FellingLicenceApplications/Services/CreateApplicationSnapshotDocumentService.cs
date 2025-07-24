using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text;
using Forestry.Flo.Services.Common.Infrastructure;
using Microsoft.Extensions.Options;

namespace Forestry.Flo.Services.FellingLicenceApplications.Services;

public class CreateApplicationSnapshotDocumentService : ICreateApplicationSnapshotDocumentService
{
    private readonly IFellingLicenceApplicationInternalRepository _fellingLicenceApplicationInternalRepository;
    private readonly ILogger<CreateApplicationSnapshotDocumentService> _logger;
    private readonly HttpClient _client;
    private readonly PDFGeneratorAPIOptions _options;

    public CreateApplicationSnapshotDocumentService(
        IFellingLicenceApplicationInternalRepository fellingLicenceApplicationInternalRepository,
        ILogger<CreateApplicationSnapshotDocumentService> logger,
        HttpClient client,
        IOptions<PDFGeneratorAPIOptions> options)
    {
        _fellingLicenceApplicationInternalRepository = Guard.Against.Null(fellingLicenceApplicationInternalRepository);
        _logger = logger;
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _options = Guard.Against.Null(options.Value);
    }

    /// <inheritdoc />
    public async Task<Result<byte[]>> CreateApplicationSnapshotAsync(Guid applicationId, PDFGeneratorRequest pdfGeneratorRequest,CancellationToken cancellationToken)
    {
        try
        {
            var fla = await _fellingLicenceApplicationInternalRepository.GetAsync(applicationId, cancellationToken);
            if (fla.HasNoValue)
            {
                _logger.LogError("Could not retrieve application with id {ApplicationId}", applicationId);
                return Result.Failure<byte[]>($"Could not retrieve application with id {applicationId}");
            }

            _logger.LogDebug("Attempting to send request for pdf of application with id {ApplicationId}", applicationId);

            var request = new HttpRequestMessage(HttpMethod.Post, _options.BaseUrl);
            request.Content = new StringContent(JsonSerializer.Serialize(pdfGeneratorRequest), Encoding.UTF8, "application/json");
            var response = await _client.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            _logger.LogDebug("Successful request for pdf of application with id {ApplicationId}", applicationId);
            return await response.Content.ReadAsByteArrayAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError("Could not generate new bytes of pdf application with id {applicationId} with exception: {error}", applicationId, ex.Message);
            return Result.Failure<byte[]>(ex.Message);
        }
    }
}