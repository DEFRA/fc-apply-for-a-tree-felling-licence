namespace Forestry.Flo.Services.Applicants.Models;

/// <summary>
/// Request model class to retrieve the AAF for an agency and woodland owner combination, optionally
/// for a specific point in time.
/// </summary>
public class GetAgentAuthorityFormRequest
{
    /// <summary>
    /// Gets and sets the ID of the Agency to retrieve the valid AAF for.
    /// </summary>
    public Guid AgencyId { get; set; }

    /// <summary>
    /// Gets and sets the ID of the Woodland Owner to retrieve the valid AAF for.
    /// </summary>
    public Guid WoodlandOwnerId { get; set; }

    /// <summary>
    /// Gets and sets the point in time that the valid AAF should be retrieved for.
    /// </summary>
    /// <remarks>If left null, then the current valid AAF will be retrieved.</remarks>
    public DateTime? PointInTime { get; set; }
}