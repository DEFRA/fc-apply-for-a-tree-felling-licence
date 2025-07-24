using Forestry.Flo.Services.FellingLicenceApplications.Entities;

namespace Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;

public class StatusHistoryModel
{
    /// <summary>
    /// Gets and Sets the status History ID.
    /// </summary>
    public Guid Id { get; protected set; }

    /// <summary>
    /// Gets or sets the felling licence application ID.
    /// </summary>
     public Guid FellingLicenceApplicationId { get; set; }

    /// <summary>
    /// Gets or sets the created date / time.
    /// </summary>
    public DateTime Created { get; set; }

    /// <summary>
    /// Gets and Sets the Id of the user who changed the status of the application.
    /// </summary>
    public Guid CreatedById { get; set; }

    /// <summary>
    /// Gets or sets the Felling Licence Status.
    /// </summary>
    public FellingLicenceStatus Status { get; set; }
}