namespace Forestry.Flo.Services.Applicants.Models;

public class AddAgencyDetailsRequest
{
    /// <summary>
    /// Gets and sets the id of the user adding the Agency entry.
    /// </summary>
    public Guid CreatedByUser { get; set; }

    /// <summary>
    /// Gets and sets the details of the agency to be entered into the system.
    /// </summary>
    public AgencyModel agencyModel { get; set; }
}