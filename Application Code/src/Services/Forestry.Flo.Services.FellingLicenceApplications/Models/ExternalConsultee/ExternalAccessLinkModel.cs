namespace Forestry.Flo.Services.FellingLicenceApplications.Models.ExternalConsultee;

public record ExternalAccessLinkModel(
    string ContactName,
    string ContactEmail,
    string Purpose,
    DateTime CreatedTimeStamp,
    DateTime ExpiresTimeStamp,
    Guid ApplicationId);