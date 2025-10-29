namespace Forestry.Flo.Services.Notifications.Models;

/// <summary>
/// Model class for data to merge into a ExternalConsulteeInvite notification.
/// </summary>
public class ExternalConsulteeInviteDataModel : IApplicationNotification
{
    public string ConsulteeName { get; set; }
    
    public string EmailText { get; set; }
    
    public string ViewApplicationURL { get; set; }
    
    public string ApplicationReference { get; set; }
    
    public string SenderName { get; set; }

    public string SenderEmail { get; set; }

    public string? CommentsEndDate { get; set; }

    /// <summary>
    /// Gets and sets the name & address of the admin hub that the application is managed by.
    /// </summary>
    public string AdminHubFooter { get; set; }

    /// <summary>
    /// Gets and sets the application id.
    /// </summary>
    public required Guid ApplicationId { get; set; }
}