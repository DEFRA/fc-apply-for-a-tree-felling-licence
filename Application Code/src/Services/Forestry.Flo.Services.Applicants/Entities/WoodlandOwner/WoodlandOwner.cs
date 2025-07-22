using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.Services.Applicants.Entities.WoodlandOwner;

/// <summary>
/// Model class representing a Woodland Owner's details.
/// </summary>
public class WoodlandOwner
{
    /// <summary>
    /// Gets the unique internal identifier for the Woodland Owner record on the system.
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Gets and sets the <see cref="Entities.WoodlandOwner.WoodlandOwnerType"/> of the woodland owner.
    /// </summary>
    public WoodlandOwnerType WoodlandOwnerType { get; set; } = WoodlandOwnerType.WoodlandOwner;

    /// <summary>
    /// Gets and Sets the contact address of the Woodland Owner on the system.
    /// </summary>
    public Address? ContactAddress { get; set; }

    /// <summary>
    /// Gets and sets the contact telephone number of the woodland owner.
    /// </summary>
    public string? ContactTelephone { get; set; }
    
    /// <summary>
    /// Gets and Sets the email address of the external user.
    /// </summary>
    public string? ContactEmail { get; set; }

    /// <summary>
    /// Gets and Sets the full name of the Woodland owner.
    /// </summary>
    public string? ContactName { get; set; }

    /// <summary>
    /// Gets and sets a flag indicating whether or not this woodland owner is an individual or an organisation.
    /// </summary>
    [Required]
    public bool IsOrganisation { get; set; }

    /// <summary>
    /// Gets and Sets the Organisation name that the woodland owner is part of.
    /// </summary>
    public string? OrganisationName { get; set; }

    /// <summary>
    /// Gets and Sets the address of the Organisation that the woodland owner is part of.
    /// </summary>
    public Address? OrganisationAddress { get; set; }

    /// <summary>
    /// Gets and sets the <see cref="TenantType"/>
    /// </summary>
    public TenantType TenantType { get; set; } = TenantType.None;

    /// <summary>
    /// Gets and Sets the first name of the tenant's landlord.
    /// </summary>
    public string? LandlordFirstName { get; set; }

    /// <summary>
    /// Gets and Sets the last name of the tenant's landlord.
    /// </summary>
    public string? LandlordLastName { get; set; }
}