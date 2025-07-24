using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;

namespace Forestry.Flo.Services.FellingLicenceApplications.Services;

public interface IViewCaseNotesService
{
    /// <summary>
    /// Fetch applicant visible case notes for a felling licence application.
    /// </summary>
    /// <param name="applicationId"></param>
    /// <param name="visibleToApplicantOnly"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<IList<CaseNoteModel>> GetCaseNotesAsync(Guid applicationId, bool visibleToApplicantOnly, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieve case notes of a specific case note type or types for a felling licence application.
    /// </summary>
    /// <param name="applicationId">The id of the application to retrieve case notes for.</param>
    /// <param name="caseNoteTypes">An array of <see cref="CaseNoteType"/> to filter the case notes by.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A list of case notes for the given application Id that match any of the given case note types.</returns>
    Task<IList<CaseNoteModel>> GetSpecificCaseNotesAsync(Guid applicationId, CaseNoteType[] caseNoteTypes, CancellationToken cancellationToken);
}