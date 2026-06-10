using System.ComponentModel;
using Forestry.Flo.Services.Applicants.Entities.UserAccount;

namespace Forestry.Flo.External.Web.Models.UserAccount;

/// <summary>
/// Model class representing a user's account in the system.
/// </summary>
public class UserAccountModel : PageWithBreadcrumbsViewModel
{
    /// <summary>
    /// The user's email address; this is readonly and cannot be changed after account creation,
    /// as it is used as the unique identifier for the account and links to the external identity provider.
    /// </summary>
    public string AccountEmailAddress { get; set; }

    /// <summary>
    /// Gets and sets a model representing the person's name details, including title, first name, and last name.
    /// </summary>
    public AccountPersonNameModel? PersonName { get; set; }

    /// <summary>
    /// Gets and sets a model representing the person's contact details, such as phone number and email address.
    /// </summary>
    public AccountPersonContactModel? PersonContactsDetails { get; set; }

    /// <summary>
    /// Gets and sets the account type of the external user, which may determine the user's permissions and access levels within the system.
    /// </summary>
    [DisplayName("Account Type")]
    public UserTypeModel UserTypeModel { get; set; } = new();

    /// <summary>
    /// Gets and Sets the account status of the external user.
    /// </summary>
    public UserAccountStatus Status { get; set; } = UserAccountStatus.Invited;

    /// <summary>
    /// Gets and sets the accepts terms and conditions statuses of the external user.
    /// </summary>
    public AccountTermsAndConditionsModel AcceptsTermsAndConditions { get; set; } = new();

    /// <summary>
    /// Gets and sets a flag that disables the sign-up pages from being edited.
    /// </summary>
    public bool PageIsDisabled { get; set; }

}