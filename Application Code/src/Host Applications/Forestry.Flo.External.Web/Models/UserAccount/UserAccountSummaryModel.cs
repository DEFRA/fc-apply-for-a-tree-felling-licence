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
    public AccountPersonNameModel? PersonName { get; set; }

    public AccountPersonContactModel? PersonContactsDetails { get; set; }

    [DisplayName("Account Type")]
    public UserTypeModel UserTypeModel { get; set; } = new();

    /// <summary>
    /// Gets and Sets the account status of the external user.
    /// </summary>
    public UserAccountStatus Status { get; set; } = UserAccountStatus.Invited;

    public WoodlandOwnerModel? WoodlandOwner { get; set; }

    public AgencyModel? Agency { get; set; }

    public LandlordDetails? LandlordDetails { get; set; }

    public bool AccountTypeReadOnly { get; set; }

    public bool OrganisationDetailsReadOnly { get; set; }
}