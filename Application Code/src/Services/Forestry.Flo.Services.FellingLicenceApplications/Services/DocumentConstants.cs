using Forestry.Flo.Services.FellingLicenceApplications.Entities;

namespace Forestry.Flo.Services.FellingLicenceApplications.Services;

/// <summary>
/// Static class to hold constants related to documents uploaded to an application.
/// </summary>
public static class DocumentConstants
{
    /// <summary>
    /// List of <see cref="DocumentPurpose"/> for applicant-uploaded documents that are subject to the
    /// maximum document count limitation for an application.
    /// </summary>
    public static readonly HashSet<DocumentPurpose> ExternalDocumentTypesWithCountLimitation =
    [
        DocumentPurpose.Attachment,
        DocumentPurpose.EiaAttachment,
        DocumentPurpose.TreeHealthAttachment
    ];
}