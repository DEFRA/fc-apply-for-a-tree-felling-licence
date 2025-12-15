using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.Services.FellingLicenceApplications.Entities;

/// <summary>
/// Entity class representing proposed compartment designations
/// </summary>
public class ProposedCompartmentDesignations
{
    /// <summary>
    /// Gets and Sets the proposed compartment designations ID.
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the linked property profile identifier.
    /// </summary>
    public Guid LinkedPropertyProfileId { get; set; }

    /// <summary>
    /// Gets or sets the linked property profile.
    /// </summary>
    public LinkedPropertyProfile LinkedPropertyProfile { get; set; }

    /// <summary>
    /// Gets or sets the property profile compartment identifier.
    /// </summary>
    public Guid PropertyProfileCompartmentId { get; set; }

    /// <summary>
    /// Gets and sets a value indicating which of the configured PAWS zones of interest that
    /// this compartment crosses.
    /// </summary>
    public List<string> CrossesPawsZones { get; set; } = [];

    /// <summary>
    /// Gets and sets the <see cref="NativeTreeSpeciesProportion"/> for the compartment before the
    /// felling operation is carried out.
    /// </summary>
    public NativeTreeSpeciesProportion? ProportionBeforeFelling { get; set; }

    /// <summary>
    /// Gets and sets the <see cref="NativeTreeSpeciesProportion"/> for the compartment after the
    /// felling operation is carried out.
    /// </summary>
    public NativeTreeSpeciesProportion? ProportionAfterFelling { get; set; }

    /// <summary>
    /// Gets and sets a value indicating whether the compartment is being restored.
    /// </summary>
    public bool? IsRestoringCompartment { get; set; }

    /// <summary>
    /// Gets and sets details about the restoration plans for the compartment.
    /// </summary>
    public string? RestorationDetails { get; set; }
}