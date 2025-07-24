using Forestry.Flo.External.Web.Models.Compartment;

namespace Forestry.Flo.External.Web.Models.PropertyProfile;

public class PropertyProfileDetails: PageWithBreadcrumbsViewModel, IPropertyWithBreadcrumbsViewModel
{
    /// <summary>
    /// Gets and Sets the property profile id
    /// </summary>
    public Guid  Id { get; init; }
    
    /// <summary>
    /// Gets and Sets the property profile name
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Gets and Sets the nearest town.
    /// </summary>
    public string? NearestTown { get; set; }

    /// <summary>
    /// Gets and Sets the name of the wood.
    /// </summary>
    public string? NameOfWood { get; set; }

    /// <summary>
    /// Gets and Sets the Woodland Management Plan reference.
    /// </summary>
    public string? WoodlandManagementPlanReference { get; set; }

    /// <summary>
    /// Gets and Sets the Woodland Certification Scheme reference.
    /// </summary>
    public string? WoodlandCertificationSchemeReference { get; set; }

    /// <summary>
    /// Gets and Sets the list of Compartment objects
    /// </summary>
    public IEnumerable<CompartmentModel> Compartments { get; init; } = null!;

    public Guid WoodlandOwnerId { get; set; }

    public Guid? AgencyId { get; set; }
}