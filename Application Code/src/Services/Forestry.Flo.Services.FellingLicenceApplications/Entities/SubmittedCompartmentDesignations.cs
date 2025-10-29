using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.Services.FellingLicenceApplications.Entities;

/// <summary>
/// Entity class representing the designations for a submitted compartment on an FLA.
/// </summary>
public class SubmittedCompartmentDesignations
{
    [Key] 
    public Guid Id { get; set; }

    /// <summary>
    /// Gets and sets the ID of the <see cref="SubmittedFlaPropertyCompartment"/> that these designations belong to.
    /// </summary>
    public Guid SubmittedFlaPropertyCompartmentId { get; set; }

    /// <summary>
    /// Navigation property to the parent <see cref="SubmittedFlaPropertyCompartment"/>.
    /// </summary>
    public SubmittedFlaPropertyCompartment SubmittedFlaPropertyCompartment { get; set; }

    /// <summary>
    /// Gets and sets a value indicating whether the compartment is within an SSSI.
    /// </summary>
    public bool Sssi { get; set; }

    /// <summary>
    /// Gets and sets a value indicating whether the compartment is within an SAC.
    /// </summary>
    public bool Sacs { get; set; }

    /// <summary>
    /// Gets and sets a value indicating whether the compartment is within an SPA.
    /// </summary>
    public bool Spa { get; set; }

    /// <summary>
    /// Gets and sets a value indicating whether the compartment is within a RAMSAR.
    /// </summary>
    public bool Ramsar { get; set; }

    /// <summary>
    /// Gets and sets a value indicating whether the compartment is within an SBI.
    /// </summary>
    public bool Sbi { get; set; }

    /// <summary>
    /// Gets and sets a value indicating whether the compartment has other designations.
    /// </summary>
    public bool Other { get; set; }

    /// <summary>
    /// Gets and sets details about other designations if applicable.
    /// </summary>
    public string? OtherDesignationDetails { get; set; }

    /// <summary>
    /// Gets and sets a value indicating whether the compartment has no designations.
    /// </summary>
    public bool None { get; set; }
}