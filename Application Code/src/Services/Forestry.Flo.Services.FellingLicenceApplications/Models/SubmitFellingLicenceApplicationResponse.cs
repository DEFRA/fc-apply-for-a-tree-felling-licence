using Forestry.Flo.Services.FellingLicenceApplications.Entities;

namespace Forestry.Flo.Services.FellingLicenceApplications.Models;

public record SubmitFellingLicenceApplicationResponse(
    string ApplicationReference,
    Guid WoodlandOwnerId,
    Guid PropertyProfileId,
    FellingLicenceStatus PreviousStatus,
    List<Guid> AssignedInternalUsers,
    string? AdminHubName);