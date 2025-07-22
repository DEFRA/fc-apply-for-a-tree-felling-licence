namespace Forestry.Flo.Services.Notifications.Models;

/// <summary>
/// Model class for data to merge into an InformApplicantOfReturnedApplication notification.
/// </summary>

public class InformApplicantOfReturnedLarchApplicationDataModel : IApplicationNotification
{
    /// <summary>
    /// Gets and sets the name of the individual the notification is being sent to.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets and sets the application reference id.
    /// </summary>
    public string ApplicationReference { get; set; }

    /// <summary>
    /// Gets and sets the name of the property the application is for.
    /// </summary>
    public string? PropertyName { get; set; }

    /// <summary>
    /// Gets and sets the list of the larch species in the application.
    /// </summary>
    public List<string> IdentifiedSpeciesList { get; set; }

    /// <summary>
    /// Gets and sets the list of the compartments and zones, e.g., ["A1 - Zone 1", "A2 - Zone 1, Zone 2"].
    /// </summary>
    public List<string> IdentifiedCompartmentsList { get; set; }

    /// <summary>
    /// Gets and sets the submission date of the application.
    /// </summary>
    public string? SubmissionDate { get; set; }

    /// <summary>
    /// Gets and sets the final action date of the application.
    /// </summary>
    public string? FinalActionDate { get; set; }

    /// <summary>
    /// Gets and sets the initial final action date of the application.
    /// </summary>
    public string? InitialFinalActionDate { get; set; }

    /// <summary>
    /// Gets and sets the Moratorium Dates of the application.
    /// </summary>
    public string? MoratoriumDates { get; set; }

    /// <summary>
    /// Gets and sets the full URL for the user to view the application on the external site.
    /// </summary>
    public string? ViewApplicationURL { get; set; }

    /// <summary>
    /// Gets and sets the name & address of the admin hub that the application is managed by.
    /// </summary>
    public string AdminHubFooter { get; set; }

}