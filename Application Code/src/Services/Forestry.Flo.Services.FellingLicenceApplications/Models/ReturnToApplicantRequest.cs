using Forestry.Flo.Services.Common.Models;

namespace Forestry.Flo.Services.FellingLicenceApplications.Models;

public record ReturnToApplicantRequest(
    Guid ApplicationId,
    UserAccessModel ApplicantToReturnTo,
    Guid PerformingUserId,
    bool PerformingUserIsAccountAdmin,
    ApplicationStepStatusRecord SectionsRequiringAttention,
    string? CaseNoteContent);