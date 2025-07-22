using Forestry.Flo.Services.Applicants.Models;

/// <summary>
/// Model class representing a request to create a new <see cref="WoodlandOwner"/> within the system.
/// </summary>
public class AddWoodlandOwnerDetailsRequest
{
    /// <summary>
    /// Gets and sets the id of the user adding the Woodland Owner entry.
    /// </summary>
    public Guid CreatedByUser { get; set; }

    /// <summary>
    /// Gets and sets the details of the woodland owner to be entered into the system.
    /// </summary>
    public WoodlandOwnerModel WoodlandOwner { get; set; }
}