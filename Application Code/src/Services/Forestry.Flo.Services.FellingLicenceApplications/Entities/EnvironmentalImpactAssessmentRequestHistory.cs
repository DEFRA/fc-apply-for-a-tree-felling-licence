using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.Services.FellingLicenceApplications.Entities;

/// <summary>
/// Environmental Impact Notification Request entity class
/// </summary>
public class EnvironmentalImpactAssessmentRequestHistory
{
    /// <summary>
    /// Gets and Sets the Id of this entity.
    /// </summary>
    [Key]
    public Guid Id { get; protected set; }

    /// <summary>
    /// Gets and sets the Environmental Impact Assessment id this notification relates to.
    /// </summary>
    [Required]
    public Guid EnvironmentalImpactAssessmentId { get; set; }

    /// <summary>
    /// Navigation property to the Environmental Impact Assessment this notification relates to.
    /// </summary>
    [Required]
    public EnvironmentalImpactAssessment? EnvironmentalImpactAssessment { get; set; }

    /// <summary>
    /// Gets or sets the user ID of the user who requested this notification, if applicable.
    /// </summary>
    [Required]
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

/// <summary>
/// An enumeration of the types of requests that can be made for Environmental Impact Assessments.
/// </summary>
public enum RequestType
{
    /// <summary>
    /// A request to send a reminder to the applicant to complete and return their EIA form.
    /// </summary>
    Reminder,
    /// <summary>
    /// A request to notify the applicant that their application is missing required EIA documents.
    /// </summary>
    MissingDocuments,
}