using Forestry.Flo.Services.FellingLicenceApplications.Entities;

namespace Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview;

/// <summary>
/// Represents a combined model containing a collection of confirmed felling and restocking details,
/// a collection of submitted FLA property compartments, and a flag indicating whether the confirmation process is complete.
/// </summary>
/// <param name="ConfirmedFellingAndRestockingDetailModels">
/// The list of confirmed felling and restocking detail models.
/// </param>
/// <param name="SubmittedFlaPropertyCompartments">
/// The list of submitted FLA property compartments associated with the application.
/// </param>
/// <param name="ConfirmedFellingAndRestockingComplete">
/// Indicates whether the confirmation of felling and restocking is complete.
/// </param>
public record CombinedConfirmedFellingAndRestockingDetailRecord
(
    List<FellingAndRestockingDetailModel> ConfirmedFellingAndRestockingDetailModels,
    List<SubmittedFlaPropertyCompartment> SubmittedFlaPropertyCompartments,
    bool ConfirmedFellingAndRestockingComplete
);