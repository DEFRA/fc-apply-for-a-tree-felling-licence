namespace Forestry.Flo.Services.Notifications.Models;

/// <summary>
/// Model class for data to merge into a InformAdminOfNewAccountSignup notification.
/// </summary>
public class InformAdminOfNewAccountSignupDataModel
{
    /// <summary>
    /// Gets and sets the recipient name.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets and sets the name of the individual that has registered a new account.
    /// </summary>
    public string? AccountName { get; set; }

    /// <summary>
    /// Gets and sets the email address of the individual that has registered a new account.
    /// </summary>
    public string? AccountEmail { get; set; }

    /// <summary>
    /// Gets and sets the full URL for the admin user to access the account confirmation page.
    /// </summary>
    public string? ConfirmAccountUrl { get; set; }
}