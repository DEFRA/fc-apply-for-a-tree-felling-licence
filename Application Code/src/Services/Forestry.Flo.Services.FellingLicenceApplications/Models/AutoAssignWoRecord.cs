using Forestry.Flo.Services.FellingLicenceApplications.Entities;

namespace Forestry.Flo.Services.FellingLicenceApplications.Models;

public record AutoAssignWoRecord
(
    Guid AssignedUserId,
    string AssignedUserFirstName,
    string AssignedUserLastName,
    string AssignedUserEmail,
    Guid? UnassignedUserId
);