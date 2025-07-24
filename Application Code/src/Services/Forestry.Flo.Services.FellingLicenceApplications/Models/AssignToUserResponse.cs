using CSharpFunctionalExtensions;

namespace Forestry.Flo.Services.FellingLicenceApplications.Models;

/// <summary>
/// Response model returned after assigning an application to an internal user.
/// </summary>
/// <param name="UpdatedApplicationReference">The current application reference of the updated application.</param>
/// <param name="OriginalApplicationReference">The original application reference of the application, returned in case the area code was changed.</param>
/// <param name="ApplicationAlreadyAssignedToThisUser">A flag indicating whether the application was already assigned
/// <param name="IdOfUnassignedUser">The id of any user that was unassigned for the role and replaced with the new user.</param>
/// for the selected role to the selected user.</param>
public record AssignToUserResponse(
    string UpdatedApplicationReference,
    string OriginalApplicationReference,
    bool ApplicationAlreadyAssignedToThisUser,
    Maybe<Guid> IdOfUnassignedUser);