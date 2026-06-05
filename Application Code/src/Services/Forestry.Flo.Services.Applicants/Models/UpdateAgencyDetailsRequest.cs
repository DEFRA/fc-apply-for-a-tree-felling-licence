namespace Forestry.Flo.Services.Applicants.Models;


public class UpdateAgencyDetailsRequest
{
    /// <summary>
    /// Gets and sets the id of the user adding the Agency entry.
    /// </summary>
    public Guid UpdatedByUser { get; set; }

    /// <summary>
    /// Gets and sets the ID of the agency to be updated in the system.
    /// </summary>
    public Guid AgencyId { get; set; }

    /// <summary>
    /// Gets and sets the new details of the agency to be updated in the system.
    /// </summary>
    public AgencyModel AgencyModel { get; set; }
}