using Microsoft.AspNetCore.Mvc;

namespace Forestry.Flo.Internal.Web.Models.WoodlandOfficerReview;

public class ConfirmedFellingRestockingDetailsModel : WoodlandOfficerReviewModelBase
{
    [HiddenInput]
    public Guid ApplicationId { get; set; }

    public CompartmentConfirmedFellingRestockingDetailsModel[] Compartments { get; set; } = [];

    public bool ConfirmedFellingAndRestockingComplete { get; set; }

    public bool IsAmended
    {
        get
        {
            if (Compartments == null)
                return false;

            return Compartments
                .Where(c => c?.ConfirmedFellingDetails != null)
                .SelectMany(c => c.ConfirmedFellingDetails)
                .Any(d => d?.AmendedProperties != null && d.AmendedProperties.Count > 0);
        }
    }
}