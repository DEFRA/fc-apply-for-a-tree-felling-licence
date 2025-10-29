using Forestry.Flo.Services.FellingLicenceApplications.Entities;

namespace Forestry.Flo.Services.FellingLicenceApplications.Models.ExternalConsultee;

public record ExternalAccessLinkModel(
    string ContactName,
    string ContactEmail,
    string Purpose,
    DateTime CreatedTimeStamp,
    DateTime ExpiresTimeStamp,
    Guid ApplicationId,
    ExternalAccessLinkType LinkType,
    List<Guid> SharedSupportingDocuments);