namespace Forestry.Flo.Services.FellingLicenceApplications.Entities;

/// <summary>
/// Entity class representing the status of designations data entry for a compartment
/// </summary>
public class CompartmentDesignationStatus
{
    /// <summary>
    /// The property profile compartment id of the compartment needing designations data entry.
    /// </summary>
    public Guid CompartmentId { get; set; }

    /// <summary>
    /// The status of the data entry for the designations data for the compartment.
    /// </summary>
    public bool? Status { get; set; }
}