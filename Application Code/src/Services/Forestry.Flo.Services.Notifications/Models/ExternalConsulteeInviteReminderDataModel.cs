namespace Forestry.Flo.Services.Notifications.Models;

/// <summary>
/// Model class for data to merge into a ExternalConsulteeInviteReminder notification.
/// </summary>
public class ExternalConsulteeInviteReminderDataModel : IApplicationNotification
{
    public string ApplicationReference { get; set; }

    public string ViewApplicationURL { get; set; }
    
    public string PropertyName { get; set; }

    public string ConsultationEndDate { get; set; }

    /// <summary>
    /// Gets and sets the name & address of the admin hub that the application is managed by.
    /// </summary>
    public string AdminHubFooter { get; set; }

    /// <summary>
    /// Gets and sets the application id.
    /// </summary>
    public required Guid ApplicationId { get; set; }
}