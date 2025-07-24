using CSharpFunctionalExtensions;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FileStorage.Model;

namespace Forestry.Flo.Services.FellingLicenceApplications.Services;

/// <summary>
/// Defines the contract for a service that retrieves a stored document for an application
/// for the internal users web app only.
/// </summary>
public interface IGetDocumentServiceInternal
{
    /// <summary>
    /// Retrieve a specific supporting document.
    /// </summary>
    /// <param name="request">A populated <see cref="GetDocumentRequest"/> record with the parameters for the request.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A populated <see cref="FileToStoreModel"/>.</returns>
    Task<Result<FileToStoreModel>> GetDocumentAsync(
        GetDocumentRequest request,
        CancellationToken cancellationToken);
}