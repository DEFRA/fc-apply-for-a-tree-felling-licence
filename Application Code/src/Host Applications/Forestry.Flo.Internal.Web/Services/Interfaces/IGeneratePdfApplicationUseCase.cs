using CSharpFunctionalExtensions;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;

namespace Forestry.Flo.Internal.Web.Services.Interfaces;

/// <summary>
/// Defines the contract for generating a PDF of a felling licence application and attaching it as a supporting document.
/// </summary>
public interface IGeneratePdfApplicationUseCase
{
    /// <summary>
    /// Generates a PDF for the current submitted version of the application and adds it as a supporting document.
    /// </summary>
    /// <param name="internalUserId"> The identifier for the internal user generating the PDF.</param>
    /// <param name="applicationId">The identifier for the application.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// A <see cref="Result{T}"/> containing a <see cref="Document"/> representing the generated PDF, or an error if unsuccessful.
    /// </returns>
    Task<Result<Document>> GeneratePdfApplicationAsync(
        Guid internalUserId,
        Guid applicationId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Calculates the licence expiry date for the given application.
    /// </summary>
    /// <param name="applicationId">The ID of the application to calculate the expiry date for.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>Returns the calculated expiry date, or null if it cannot be determined.</returns>
    Task<DateTime?> CalculateLicenceExpiryDateAsync(Guid applicationId, CancellationToken cancellationToken);
}