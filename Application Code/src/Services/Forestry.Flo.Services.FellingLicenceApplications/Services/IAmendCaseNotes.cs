using CSharpFunctionalExtensions;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;

namespace Forestry.Flo.Services.FellingLicenceApplications;

/// <summary>
/// Contract for services that amend case notes.
/// </summary>
public interface IAmendCaseNotes
{
    /// <summary>
    /// Add a case note to a felling licence application.
    /// </summary>
    /// <param name="addCaseNoteRecord">A populated <see cref="AddCaseNoteRecord"/> representing the case note to be added.</param>
    /// <param name="userId">The id of the user creating the case note.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Result"/> indicating the outcome of the operation.</returns>
    Task<Result> AddCaseNoteAsync(
        AddCaseNoteRecord addCaseNoteRecord,
        Guid userId,
        CancellationToken cancellationToken);
}