using System.Net;
using Ardalis.GuardClauses;
using Forestry.Flo.Services.FileStorage.Configuration;
using Forestry.Flo.Services.FileStorage.ResultModels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Forestry.Flo.Services.FileStorage.Services;

/// <summary>
/// An implementation of <see cref="IVirusScannerService"/> that runs against Quicksilva's virus scan service.
/// <para>
/// To use this service an HTTP POST request is sent to the endpoint,
/// with the file content to be scanned included as the request body.
/// The HTTP result indicates if a virus, or other mal-ware, was detected in the provided content.
/// </para>
/// </summary>
public class QuicksilvaVirusScannerService : IVirusScannerService
{
    private readonly HttpClient _httpClient;
    private readonly QuicksilvaVirusScannerServiceOptions _options;
    private readonly ILogger<QuicksilvaVirusScannerService> _logger;

    /// <summary>
    /// Creates a new instance of <see cref="QuicksilvaVirusScannerService"/>.
    /// </summary>
    /// <param name="httpClientFactory">An <see cref="IHttpClientFactory"/> to provide the <see cref="HttpClient"/> with
    /// which to communicate with the Quicksilva virus scanner service endpoint.</param>
    /// <param name="options">Instance of <see cref="QuicksilvaVirusScannerServiceOptions"/> for use with this implementation.</param>
    /// <param name="logger"></param>
    public QuicksilvaVirusScannerService(
        IHttpClientFactory httpClientFactory,
        IOptions<QuicksilvaVirusScannerServiceOptions> options,
        ILogger<QuicksilvaVirusScannerService> logger)
    {
        Guard.Against.Null(httpClientFactory);
        _options = Guard.Against.Null(options.Value);
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient("QuicksilvaVirusScannerServiceClient");
        _httpClient.BaseAddress = new Uri(_options.AvEndpoint);
    }

    /// <summary>
    /// This implementation will always complete with the result that no virus was found.
    /// </summary>
    /// <param name="fileName"></param>
    /// <param name="bytes"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<AntiVirusScanResult> ScanAsync(
        string fileName, 
        byte[] bytes, 
        CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(fileName);
        Guard.Against.Null(bytes);

        if (!_options.IsEnabled)
        {
            _logger.LogWarning("Antivirus scanning is disabled in configuration, file named [{fileName}] will not be scanned.", fileName);
            return AntiVirusScanResult.DisabledInConfiguration;
        }

        _logger.LogDebug("About to scan file named [{fileName}], with length of [{byteLength}] bytes against configured scanner at [{avEndPoint}]."
            ,fileName, bytes.Length, _options.AvEndpoint);

        try
        {
            var response = await _httpClient.SendAsync(
                new HttpRequestMessage(
                    HttpMethod.Post, 
                    string.Empty)
                {
                    Content = new ByteArrayContent(bytes)
                }
                ,cancellationToken);
           
            if (response.IsSuccessStatusCode)
            {
                _logger.LogDebug("Completed AV scan of file named [{fileName}], scan reports file is safe.", fileName);
                return AntiVirusScanResult.VirusFree;
            }

            switch (response.StatusCode)
            {
                case HttpStatusCode.NotAcceptable:
                    //file contains virus/malware
                    _logger.LogWarning("AV scan indicates the file named [{fileName}] does contain a virus / malware.", fileName);
                    return AntiVirusScanResult.VirusFound;
                case HttpStatusCode.InternalServerError:
                    //uncertain whether file contains virus/malware
                    _logger.LogWarning("AV scan of file named [{fileName}] could not be completed successfully against [{AvEndPoint}]."
                        , fileName, _options.AvEndpoint);
                    break;
                default:
                    _logger.LogWarning("AV scan of file named [{fileName}] against [{AvEndPoint}] return with an unexpected HTTP Status Code of [{HttpStatusCode}]."
                        , fileName, _options.AvEndpoint, response.StatusCode);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AV scan of file named [{fileName}] against [{AvEndPoint}] resulted in an exception."
                , fileName, _options.AvEndpoint);
        }

        return AntiVirusScanResult.Undetermined;
    }
}