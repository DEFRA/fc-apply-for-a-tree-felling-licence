using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.Services.Applicants.Entities.Agent;

/// <summary>
/// Model class representing the details for an Agency.
/// </summary>
public class Agency
{
    /// <summary>
    /// Gets the unique internal identifier for the Agency record on the system.
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Gets and Sets the address of the Agency on the system.
    /// </summary>
    public Address? Address { get; set; }
    
    /// <summary>
    /// Gets and Sets the email address of the external user agency.
    /// </summary>
    public string? ContactEmail { get; set; }

    /// <summary>
    /// Gets and Sets the name of the main contact at the agency.
    /// </summary>
    public string? ContactName { get; set; }

    /// <summary>
    /// Gets and Sets the Organisation name of the agency.
    /// </summary>
    public string? OrganisationName { get; set; }

    /// <summary>
    /// Get and sets a flag indicating whether the agent should auto approve thinning applications
    /// </summary>
    public bool ShouldAutoApproveThinningApplications { get; set; }

    /// <summary>
    /// Get and sets a flag indicating whether this Agency is the Internal FC agency.
    /// </summary>
    /// <remarks>
    /// Note that this property value must be set to false in code for EF to insert or update the entity in the repository.
    /// See the <see cref="ApplicantsContext"/> which enforces this rule via use
    /// of <see cref="Microsoft.EntityFrameworkCore.Metadata.IMutableProperty.SetBeforeSaveBehavior"/>. 
    /// </remarks>
    public bool IsFcAgency = false;

    /// <summary>
    /// Gets and sets a flag indicating whether this agency represents an organisation.
    /// </summary>
    public bool IsOrganisation { get; set; }
}
