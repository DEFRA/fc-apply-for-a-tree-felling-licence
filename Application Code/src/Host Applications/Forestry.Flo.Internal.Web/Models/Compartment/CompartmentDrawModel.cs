namespace Forestry.Flo.Internal.Web.Models.Compartment;

public class CompartmentDrawModel:PageWithBreadcrumbsViewModel
{
    public CompartmentModel CompartmentModelOfInterest { get; set; }
    
    public string? NearestTown { get; set; }

    public string AllOtherPropertyCompartmentJson { get; set; }

    public Guid? ApplicationId { get; set; }
}