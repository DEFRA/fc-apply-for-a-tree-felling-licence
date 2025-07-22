using Forestry.Flo.Services.FellingLicenceApplications.Entities;

namespace Forestry.Flo.External.Web.Models.Compartment;

public class CompartmentDrawModel:PageWithBreadcrumbsViewModel
{
    public CompartmentModel CompartmentModelOfInterest { get; set; }
    
    public string? NearestTown { get; set; }

    public string AllOtherPropertyCompartmentJson { get; set; }

    public Guid? ApplicationId { get; set; }
    public bool IsForRestockingCompartmentSelection { get; set; }
    public Guid? FellingCompartmentId { get; set; }
    public string? FellingCompartmentName { get; set; }
    public Guid? ProposedFellingDetailsId { get; set; }
    public FellingOperationType? FellingOperationType { get; set; }
}