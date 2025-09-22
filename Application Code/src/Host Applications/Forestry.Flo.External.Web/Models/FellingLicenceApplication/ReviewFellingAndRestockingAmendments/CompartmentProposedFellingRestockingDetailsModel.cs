namespace Forestry.Flo.External.Web.Models.FellingLicenceApplication.ReviewFellingAndRestockingAmendments;

public class CompartmentProposedFellingRestockingDetailsModel : CompartmentConfirmedFellingRestockingDetailsModelBase
{
    /// <summary>
    /// A collection of proposed felling details for the compartment.
    /// </summary>
    public Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview.ProposedFellingDetailModel[] ProposedFellingDetails { get; set; } = [];
}