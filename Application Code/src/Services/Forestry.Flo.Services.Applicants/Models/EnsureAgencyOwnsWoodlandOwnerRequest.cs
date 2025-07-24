namespace Forestry.Flo.Services.Applicants.Models;

/// <summary>
/// Request model for checking that an Agency has an approved AAF for a particular woodland owner.
/// </summary>
public record EnsureAgencyOwnsWoodlandOwnerRequest(Guid AgencyId, Guid WoodlandOwnerId);