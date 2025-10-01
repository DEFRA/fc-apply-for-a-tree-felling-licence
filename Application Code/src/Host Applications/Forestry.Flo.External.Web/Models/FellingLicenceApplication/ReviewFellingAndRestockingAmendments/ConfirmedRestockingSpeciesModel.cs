using Microsoft.AspNetCore.Mvc;

namespace Forestry.Flo.External.Web.Models.FellingLicenceApplication.ReviewFellingAndRestockingAmendments;

public class ConfirmedRestockingSpeciesModel
{
    [HiddenInput]
    public Guid? Id { get; set; }

    public string Species { get; set; }

    public int? Percentage { get; set; }

    public bool Deleted { get; set; } = false;
}