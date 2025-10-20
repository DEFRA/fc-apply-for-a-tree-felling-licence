using CSharpFunctionalExtensions;
using Forestry.Flo.Services.FileStorage.Services;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Forestry.Flo.Internal.Web.Services.Interfaces;

/// <summary>
/// Handles the use case for an internal user uploading one or more supporting documents as part
/// of a felling licence application.
/// </summary>
public interface IAddSupportingDocumentsUseCase
{
    /// <summary>
    /// Saves each uploaded file in <see cref="FormFileCollection"/> using the configured <see cref="IFileStorageService"/>, firstly validating the file with <see cref="FileValidator"/>.
    /// </summary>
    /// <remarks>
    /// Adds a <see cref="ModelError"/> for each <see cref="FormFile"/> that could not be saved either due to validation failure or error.
    /// </remarks>
    /// <param name="user">The internal user uploading files.</param>
    /// <param name="applicationId">The application id.</param>
    /// <param name="supportingDocumentationFiles">The supporting document files.</param>
    /// <param name="modelState">The model state.</param>
    /// <param name="visibleToApplicant">A flag indicating whether supporting documents are visible to external applicants.</param>
    /// <param name="visibleToConsultees">A flag indicating whether supporting documents are visible to consultees.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns></returns>
    Task<Result> AddDocumentsToApplicationAsync(
        InternalUser user,
        Guid applicationId,
        FormFileCollection supportingDocumentationFiles,
        ModelStateDictionary modelState,
        bool visibleToApplicant,
        bool visibleToConsultees,
        CancellationToken cancellationToken);
}