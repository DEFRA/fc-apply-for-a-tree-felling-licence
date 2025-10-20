using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Forestry.Flo.Internal.Web.Services.Interfaces;

/// <summary>
/// Contract for a use case that allows an external system to add documents to a specified Felling Licence application.
/// </summary>
public interface IAddDocumentFromExternalSystemUseCase
{
    /// <summary>
    /// Adds a LIS Constraint report document to the specified Felling Licence application.
    /// </summary>
    /// <param name="applicationId">The unique identifier of the Felling Licence application.</param>
    /// <param name="fileBytes">The byte array representing the contents of the document to be saved.</param>
    /// <param name="fileName">The original filename of the document.</param>
    /// <param name="contentType">The MIME content-type of the document.</param>
    /// <param name="documentPurpose">The purpose of the document being added.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>An <see cref="IActionResult"/> indicating the outcome of the operation.</returns>
    Task<IActionResult> AddLisConstraintReportAsync(
        Guid applicationId,
        byte[] fileBytes,
        string fileName,
        string contentType,
        DocumentPurpose documentPurpose,
        CancellationToken cancellationToken);
}