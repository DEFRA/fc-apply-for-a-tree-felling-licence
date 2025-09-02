using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.Services.FellingLicenceApplications.Models;

/// <summary>
/// Represents a case note at the form level, including its content and visibility settings.
/// </summary>
public record FormLevelCaseNote
{
    /// <summary>
    /// The content of the case note.
    /// </summary>
    public string? CaseNote { get; init; }

    /// <summary>
    /// Indicates whether the case note is visible to the applicant.
    /// </summary>
    public bool VisibleToApplicant { get; init; }

    /// <summary>
    /// Indicates whether the case note is visible to the consultee.
    /// </summary>
    public bool VisibleToConsultee { get; init; }

    /// <summary>
    /// The heading for the inset text section where the case note is displayed.
    /// </summary>
    public string InsetTextHeading { get; init; } = "Use this section to record internal notes.";
}