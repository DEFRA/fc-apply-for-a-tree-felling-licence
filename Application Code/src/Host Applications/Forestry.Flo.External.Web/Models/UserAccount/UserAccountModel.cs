using System.ComponentModel;
using Forestry.Flo.Services.Applicants.Entities.UserAccount;

namespace Forestry.Flo.External.Web.Models.UserAccount;

/// <summary>
/// Model class representing a user's account of the system.
/// </summary>
public class UserAccountModel : PageWithBreadcrumbsViewModel
{

    public AccountPersonNameModel? PersonName { get; set; }

    public AccountPersonContactModel? PersonContactsDetails { get; set; }

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