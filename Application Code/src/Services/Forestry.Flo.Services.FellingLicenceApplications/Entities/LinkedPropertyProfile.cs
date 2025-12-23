using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.Services.FellingLicenceApplications.Entities;

/// <summary>
/// LinkedPropertyProfile entity class
/// </summary>
public class LinkedPropertyProfile
{
    /// <summary>
    /// Gets and Sets the property document ID.
    /// </summary>
    [Key]
    public Guid Id { get; protected set; } // TODO: Naming, have deviated from spec, but Id is consistent with other tables in schema

    /// <summary>
    /// Gets and Sets the property profile ID.
    /// </summary>
    [Required]
    public Guid PropertyProfileId { get; set; }

    /// <summary>
    /// Gets or sets the felling licence application ID.
    /// </summary>
    [Required]
    public Guid FellingLicenceApplicationId { get; set; }

    /// <summary>
    /// Gets or sets the felling licence application.
    /// </summary>
    [Required]
    public FellingLicenceApplication FellingLicenceApplication { get; set; }

    /// <summary>
    /// Gets or sets the proposed felling details.
    /// </summary>
    public IList<ProposedFellingDetail>? ProposedFellingDetails { get; set; }

    /// <summary>
    /// Gets or sets the proposed compartment designations.
    /// </summary>
    public IList<ProposedCompartmentDesignations>? ProposedCompartmentDesignations { get; set; }
}