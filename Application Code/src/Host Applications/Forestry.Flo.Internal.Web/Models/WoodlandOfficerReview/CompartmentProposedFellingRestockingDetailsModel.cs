using Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview;
using Microsoft.AspNetCore.Mvc;

namespace Forestry.Flo.Internal.Web.Models.WoodlandOfficerReview;

public class CompartmentProposedFellingRestockingDetailsModel : CompartmentConfirmedFellingRestockingDetailsModelBase
{
    /// <summary>
    /// A collection of proposed felling details for the compartment.
    /// </summary>
    public ProposedFellingDetailModel[] ProposedFellingDetails { get; set; } = [];
}