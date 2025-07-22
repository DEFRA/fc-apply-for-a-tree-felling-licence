namespace Forestry.Flo.Services.FellingLicenceApplications.Models;

public record CompleteAdminOfficerReviewNotificationsModel(
    string ApplicationReference,
    Guid ApplicantId,
    Guid WoodlandOfficerId,
    string AdminHubName);