using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.External.Web.Models.UserAccount.AccountTypeViewModels;

/// <summary>
/// View model for selecting whether a trust user is an organisational representative.
/// </summary>
public class TrustTypeViewModel : PageWithBreadcrumbsViewModel
{
    /// <summary>
    /// Gets and Sets the organisation status of a trust user.
    /// </summary>
    [Required(ErrorMessage = "Select which type of trust you are")]
    public OrganisationStatus? OrganisationStatus { get; set; }
}