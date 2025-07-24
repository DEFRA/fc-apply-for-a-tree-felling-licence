using Forestry.Flo.Services.FellingLicenceApplications.Entities;

namespace Forestry.Flo.Services.FellingLicenceApplications.Models;

public record AssignToUserRequest(
    Guid ApplicationId,
    Guid PerformingUserId,
    Guid AssignToUserId,
    AssignedUserRole AssignedRole,
    string FcAreaCostCode,
    string? CaseNoteContent,
    bool VisibleToApplicant = false,
    bool VisibleToConsultee = false
);