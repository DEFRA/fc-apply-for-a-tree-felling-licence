using Forestry.Flo.Services.FellingLicenceApplications.Entities;

namespace Forestry.Flo.Services.FellingLicenceApplications.Models;

/// <summary>
/// Representation of an assignee history entry.
/// </summary>
/// <param name="Id">The ID of the assignee history entry.</param>
/// <param name="ApplicationId">The ID of the application.</param>
/// <param name="AssignedUserId">The ID of the assigned user.</param>
/// <param name="TimestampAssigned">The date and time the assignment was created.</param>
/// <param name="TimestampUnassigned">The date and time the assignment was removed, if any.</param>
/// <param name="Role">The role of the assignment.</param>
public record ApplicationAssigneeModel(
    Guid Id,
    Guid ApplicationId,
    Guid AssignedUserId,
    DateTime TimestampAssigned,
    DateTime? TimestampUnassigned,
    AssignedUserRole Role);