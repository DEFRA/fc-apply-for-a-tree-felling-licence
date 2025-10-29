using CSharpFunctionalExtensions;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FileStorage.ResultModels;

namespace Forestry.Flo.Services.FellingLicenceApplications.Services;

/// <summary>
/// Contract for services that add documents to applications.
/// </summary>
public interface IAddDocumentService
{
    /// <summary>
    /// Adds a collection of supporting documents to a given felling licence application as an external applicant.
    /// </summary>
    /// <param name="addDocumentsRequest">A populated <see cref="AddDocumentsRequest"/> with parameters for the request.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The result of the operation.</returns>
    Task<Result<AddDocumentsSuccessResult, AddDocumentsFailureResult>> AddDocumentsAsExternalApplicantAsync(
        AddDocumentsExternalRequest addDocumentsRequest,
        CancellationToken cancellationToken);

    /// <summary>
    /// Adds a collection of supporting documents to a given felling licence application as an internal user.
    /// </summary>
    /// <param name="addDocumentsRequest">A populated <see cref="AddDocumentsRequest"/> with parameters for the request.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The result of the operation.</returns>
    Task<Result<AddDocumentsSuccessResult, AddDocumentsFailureResult>> AddDocumentsAsInternalUserAsync(
        AddDocumentsRequest addDocumentsRequest,
        CancellationToken cancellationToken);
}