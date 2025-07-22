using Forestry.Flo.Services.FellingLicenceApplications.Entities;

namespace Forestry.Flo.Services.FellingLicenceApplications.Models;

public record StatusHistoryModel(
    DateTime Created,
    Guid? CreatedById,
    FellingLicenceStatus Status);