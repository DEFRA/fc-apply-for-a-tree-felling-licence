using Forestry.Flo.Services.Applicants.Entities.UserAccount;
using System.ComponentModel;
using Forestry.Flo.External.Web.Models.Agency;
using Forestry.Flo.External.Web.Models.UserAccount.AccountTypeViewModels;
using Forestry.Flo.External.Web.Models.WoodlandOwner;

namespace Forestry.Flo.External.Web.Models.UserAccount;

/// <summary>
/// Model class representing the full set of data entered by a user in account registration.
/// </summary>
public class UserAccountSummaryModel : PageWithBreadcrumbsViewModel
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
    /// Gets and sets a model representing the Woodland Owner entity linked to this account, if
    /// the user is a woodland owner.
    /// </summary>
    public WoodlandOwnerModel? WoodlandOwner { get; set; }

    /// <summary>
    /// Gets and sets a model representing the Agency entity linked to this account, if the user is an agency user. 
    /// </summary>
    public AgencyModel? Agency { get; set; }

    /// <summary>
    /// Gets and sets a model representing the landlord details linked to this account, if the user is a tenant.
    /// </summary>
    public LandlordDetails? LandlordDetails { get; set; }

    /// <summary>
    /// Gets and sets a flag that disables the sign-up pages from being edited.
    /// </summary>
    public bool AccountTypeReadOnly { get; set; }

    /// <summary>
    /// Gets and sets a flag that disables the organisation details from being changed from the account summary page.
    /// </summary>
    public bool OrganisationDetailsReadOnly { get; set; }
}