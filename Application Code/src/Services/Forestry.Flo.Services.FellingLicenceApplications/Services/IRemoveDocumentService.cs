using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Applicants.Entities.WoodlandOwner;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;

namespace Forestry.Flo.Services.FellingLicenceApplications.Services;

/// <summary>
/// Contract for services that both soft and hard deletes supporting documentation from applications.
/// </summary>
public interface IRemoveDocumentService
{
    /// <summary>
    /// Soft deletes supporting documentation from an application as an external applicant.
    /// </summary>
    /// <param name="removeDocumentExternalRequest">A populated <see cref="RemoveDocumentExternalRequest"/> record with the parameters for the request.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The result of the operation.</returns>

    Task<Result> RemoveDocumentAsExternalApplicantAsync(
        RemoveDocumentExternalRequest removeDocumentExternalRequest,
        CancellationToken cancellationToken);

    /// <summary>
    /// Soft deletes supporting documentation from an application as an internal user.
    /// </summary>
    /// <param name="removeDocumentRequest">A populated <see cref="RemoveDocumentRequest"/> record with the parameters for the request.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The result of the operation.</returns>
    Task<Result> RemoveDocumentAsInternalUserAsync(
        RemoveDocumentRequest removeDocumentRequest,
        CancellationToken cancellationToken);

    /// <summary>
    /// Hard deletes supporting documentation from an application.
    /// </summary>
    /// <param name="applicationId">The id of the application to hard delete the document from.</param>
    /// <param name="documentId">The id of the document to hard delete.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The result of the operation.</returns>
    Task<Result> PermanentlyRemoveDocumentAsync(
        Guid applicationId,
        Guid documentId,
        CancellationToken cancellationToken);
}