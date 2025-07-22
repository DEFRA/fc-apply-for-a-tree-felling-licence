namespace Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview;

/// <summary>
/// Model class for the confirmed felling and restocking details of an application.
/// </summary>
public record CombinedConfirmedFellingAndRestockingDetailRecord
(
    List<ConfirmedFellingAndRestockingDetailModel> ConfirmedFellingAndRestockingDetailModels,
    bool ConfirmedFellingAndRestockingComplete
);