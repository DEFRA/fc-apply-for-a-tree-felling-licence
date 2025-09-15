using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.Services.FellingLicenceApplications.Models;

public record EnvironmentalImpactAssessmentRequestHistoryRecord
{
    /// <summary>
    /// Gets and sets the Environmental Impact Assessment id this notification relates to.
    /// </summary>
    public Guid ApplicationId { get; set; }

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