using System;

namespace Forestry.Flo.Services.FellingLicenceApplications.Models;

/// <summary>
/// Model representing an application whose active amendment review response deadline
/// is within the reminder period and requires a reminder notification to be sent
/// (warning of automatic withdrawal if no response is received).
/// </summary>
public class LateAmendmentResponseWithdrawalModel
{
    /// <summary>
    /// Gets or sets the application Id.
    /// </summary>
    public Guid ApplicationId { get; set; }

    /// <summary>
    /// Gets or sets the application reference.
    /// </summary>
    public string? ApplicationReference { get; set; }

    /// <summary>
    /// Gets or sets the property name (if available).
    /// </summary>
    public string? PropertyName { get; set; }

    /// <summary>
    /// Gets or sets the Id of the user that created the application.
    /// </summary>
    public Guid CreatedById { get; set; }

    /// <summary>
    /// Gets or sets the Id of the woodland owner for the application.
    /// </summary>
    public Guid WoodlandOwnerId { get; set; }

    /// <summary>
    /// Gets or sets the Id of the amendment review triggering the reminder.
    /// </summary>
    public Guid AmendmentReviewId { get; set; }

    /// <summary>
    /// Gets or sets the date the amendments were sent to the applicant.
    /// </summary>
    public DateTime AmendmentsSentDate { get; set; }

    /// <summary>
    /// Gets or sets the deadline by which the applicant must respond.
    /// </summary>
    public DateTime ResponseDeadline { get; set; }

    /// <summary>
    /// Gets or sets the timestamp the reminder notification is being sent.
    /// (Set by caller when notification dispatched.)
    /// </summary>
    public DateTime ReminderNotificationDateSent { get; set; }

    /// <summary>
    /// Gets or sets the administrative region / hub name.
    /// </summary>
    public string? AdministrativeRegion { get; set; }

    /// <summary>
    /// Gets or sets the Id of the Woodland Officer (LastUpdatedById on the WO review) responsible for the latest review update.
    /// </summary>
    public Guid WoodlandOfficerReviewLastUpdatedById { get; set; }
}