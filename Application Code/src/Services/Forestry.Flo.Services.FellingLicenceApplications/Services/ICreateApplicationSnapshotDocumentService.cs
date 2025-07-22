using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;

namespace Forestry.Flo.Services.FellingLicenceApplications.Services;

/// <summary>
/// Contract for a service that will generate an application PDF snapshot and store it as a
/// supporting document for the application.
/// </summary>
public interface ICreateApplicationSnapshotDocumentService
{
    /// <summary>
    /// Sends the request to the pdf generator for the pdf to be returned based on the data provided in the pdfGenerator Request.
    /// The default in options for the api is set as http://localhost:9999/api/v1/generate-pdf, the port forwarding for the API needs to be set to 9999
    /// </summary>
    /// <param name="applicationId">The id of the application used for the </param>
    /// <param name="pdfGeneratorRequest">Model containing the data in string variables and collections ready for JSON serialization and then sent to the API</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Returns a <see cref="byte[]"/> of the pdf generated.</returns>
    Task<Result<byte[]>> CreateApplicationSnapshotAsync(
        Guid applicationId,
        PDFGeneratorRequest pdfGeneratorRequest,
        CancellationToken cancellationToken);
    
}