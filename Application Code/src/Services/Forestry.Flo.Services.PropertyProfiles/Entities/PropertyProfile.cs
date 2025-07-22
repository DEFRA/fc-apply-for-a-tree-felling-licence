using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Forestry.Flo.Services.PropertyProfiles.Entities;

public class PropertyProfile
{
    /// <summary>
    /// Gets and Sets the property profile id.
    /// </summary>
    [Key]
    public Guid Id { get; protected set; }

    /// <summary>
    /// Gets and Sets the property profile name.
    /// </summary>
    [Required]
    public string Name { get;  protected set; } = null!;

    /// <summary>
    /// Gets and Sets the name of the wood
    /// </summary>
    public string? NameOfWood { get; protected set; }

    /// <summary>
    /// Gets and Sets the OS Grid reference.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public string? OSGridReference { get; protected set; }
    
    /// <summary>
    /// Gets and Sets the nearest town.
    /// </summary>
    public string? NearestTown { get;  protected set; }

    /// <summary>
    /// Gets and sets a flag indicating the property profile has Woodland Management Plan.
    /// </summary>
    [Required]
    public bool HasWoodlandManagementPlan { get; protected set; }

    /// <summary>
    /// Gets and Sets the Woodland Management Plan reference.
    /// </summary>
    public string? WoodlandManagementPlanReference { get; protected set; }

    /// <summary>
    /// Gets and sets a flag indicating the property profile has Woodland Certification Scheme.
    /// </summary>
    [Required]
    public bool IsWoodlandCertificationScheme { get; protected set; }

    /// <summary>
    /// Gets and Sets the Woodland Certification Scheme reference.
    /// </summary>
    public string? WoodlandCertificationSchemeReference { get; set; }

    /// <summary>
    /// Gets and sets a flag indicating the Woodland Owner id.
    /// </summary>
    [Required]
    public Guid WoodlandOwnerId { get; set; }
    
    /// <summary>
    /// Gets and Sets the list of Compartment objects
    /// </summary>
    public List<Compartment> Compartments { get; protected set; } = null!;

    protected PropertyProfile()
    {
    }

    public PropertyProfile(
        string name, 
        string? osGridReference,
        string? nearestTown,
        string? nameOfWood,
        bool hasWoodlandManagementPlan, 
        string? woodlandManagementPlanReference,
        bool isWoodlandCertificationScheme, 
        string? woodlandCertificationSchemeReference,  
        Guid woodlandOwnerId, 
        IEnumerable<Compartment> compartments)
    {
        Name = name;
        OSGridReference = osGridReference;
        NearestTown = nearestTown;
        NameOfWood = nameOfWood;
        HasWoodlandManagementPlan = hasWoodlandManagementPlan;
        WoodlandManagementPlanReference = woodlandManagementPlanReference;
        IsWoodlandCertificationScheme = isWoodlandCertificationScheme;
        WoodlandCertificationSchemeReference = woodlandCertificationSchemeReference;
        WoodlandOwnerId = woodlandOwnerId;
        Compartments = compartments.ToList();
    }
}