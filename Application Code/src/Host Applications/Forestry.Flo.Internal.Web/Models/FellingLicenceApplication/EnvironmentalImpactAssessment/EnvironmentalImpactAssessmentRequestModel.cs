using Forestry.Flo.Services.FellingLicenceApplications.Entities;

namespace Forestry.Flo.Internal.Web.Models.FellingLicenceApplication.EnvironmentalImpactAssessment;

public class EnvironmentalImpactAssessmentRequestModel
{
    /// <summary>
    /// Gets and Sets the Id of this entity.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets and sets the Environmental Impact Assessment id this notification relates to.
    /// </summary>
    public Guid EnvironmentalImpactAssessmentId { get; set; }

    /// <summary>
    /// Gets or sets the user ID of the user who requested this notification, if applicable.
    /// </summary>
    public Guid? RequestingUserId { get; set; }

    /// <summary>
    /// Gets or sets the time the notification was requested.
    /// </summary>
    public DateTime NotificationTime { get; set; }

    /// <summary>
    /// Gets or sets the type of request.
    /// </summary>
    public RequestType RequestType { get; set; }
}