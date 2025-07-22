using Forestry.Flo.External.Web.Models.Compartment;
using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.External.Web.Models.PropertyProfile;

public class PropertyProfileModel : PageWithBreadcrumbsViewModel, IPropertyWithBreadcrumbsViewModel
{
    /// <summary>
    /// Gets and Sets the property profile id.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets and Sets the property profile name.
    /// </summary>
    [Required(ErrorMessage = "Enter the property name")]
    public string Name { get; set; } = null!;

    /// <summary>
    /// Gets and Sets the name of the wood.
    /// </summary>
    [MaxLength(DataValueConstants.PropertyNameMaxLength)]
    public string? NameOfWood { get; set; }

    /// <summary>
    /// Gets and Sets the OS Grid reference.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public string? OSGridReference { get; set; }
    
    /// <summary>
    /// Gets and Sets the nearest town.
    /// </summary>
    public string? NearestTown { get; set; }

    /// <summary>
    /// Gets and sets a flag indicating the property profile has Woodland Management Plan.
    /// </summary>
    [Required(ErrorMessage = "Select whether this property is covered by a woodland management plan")]
    public bool? HasWoodlandManagementPlan { get; set; }

    /// <summary>
    /// Gets and Sets the Woodland Management Plan reference.
    /// </summary>
    public string? WoodlandManagementPlanReference { get; set; }

    /// <summary>
    /// Gets and sets a flag indicating the property profile has Woodland Certification Scheme.
    /// </summary>
    [Required(ErrorMessage = "Select whether this property is covered by a woodland certification scheme")]
    public bool? IsWoodlandCertificationScheme { get; set; }

    /// <summary>
    /// Gets and Sets the Woodland Certification Scheme reference.
    /// </summary>
    public string? WoodlandCertificationSchemeReference { get; set; }

    /// <summary>
    /// Gets and sets the id of the woodland owner details for this property.
    /// </summary>
    public Guid WoodlandOwnerId { get; set; }

    public Guid ApplicationId { get; set; }

    public bool? IsApplication { get; set; }

    public string? ClientName { get; set; }

    public Guid? AgencyId { get; set; }
    
    public string? GIS { get; set; }

    /// <summary>
    /// Gets and Sets the list of Compartment objects
    /// </summary>
    public IEnumerable<CompartmentModel> Compartments { get; set; } = [];
}