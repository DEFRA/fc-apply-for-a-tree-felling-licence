using Forestry.Flo.Services.FellingLicenceApplications.Entities;

namespace Forestry.Flo.External.Web.Models.FellingLicenceApplication;

public record FellingLicenceApplicationSummary(
    Guid Id,
    string ApplicationReference,
    FellingLicenceStatus Status,
    string? PropertyName,
    Guid PropertyProfileId,
    string? NameOfWood,
    Guid? WoodlandOwnerId,
    string? WoodlandOwnerName, 
    string? AgencyName,
    string? PreviousReference = null);