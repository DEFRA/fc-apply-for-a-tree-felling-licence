using Forestry.Flo.Services.FellingLicenceApplications.Entities;

namespace Forestry.Flo.Services.FellingLicenceApplications.Models;

/// <summary>
/// A model of the PAWS data within a proposed compartment designations.
/// </summary>
public class PawsCompartmentDesignationsModel
{
    /// <summary>
    /// Gets and sets the proposed compartment designations ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets and sets the property profile compartment identifier.
    /// </summary>
    public Guid PropertyProfileCompartmentId { get; set; }

    /// <summary>
    /// Gets and sets the name of the property profile compartment.
    /// </summary>
    public string PropertyProfileCompartmentName { get; set; }

    /// <summary>
    /// Gets and sets a value indicating how many of the configured PAWS zones of interest that
    /// this compartment crosses.
    /// </summary>
    public List<string> CrossesPawsZones { get; set; }

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