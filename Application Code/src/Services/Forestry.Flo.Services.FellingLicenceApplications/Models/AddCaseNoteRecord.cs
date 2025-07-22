using Forestry.Flo.Services.FellingLicenceApplications.Entities;

namespace Forestry.Flo.Services.FellingLicenceApplications.Models;

public record AddCaseNoteRecord
(
    Guid FellingLicenceApplicationId,
    CaseNoteType Type,
    string Text,
    bool VisibleToApplicant,
    bool VisibleToConsultee
);