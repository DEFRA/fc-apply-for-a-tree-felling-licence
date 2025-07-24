using Microsoft.AspNetCore.Mvc;

namespace Forestry.Flo.Internal.Web.Models.WoodlandOfficerReview;

public class ConfirmedRestockingSpeciesModel
{
    [HiddenInput]
    public Guid? Id { get; set; }

    public string Species { get; set; }

    public int? Percentage { get; set; }

    public bool Deleted { get; set; } = false;
}