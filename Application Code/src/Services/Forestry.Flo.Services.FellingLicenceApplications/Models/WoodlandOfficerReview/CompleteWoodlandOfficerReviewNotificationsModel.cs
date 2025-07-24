namespace Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview;

public record CompleteWoodlandOfficerReviewNotificationsModel(
    string ApplicationReference,
    Guid ApplicantId,
    Guid FieldManagerId,
    string AdminHubName);