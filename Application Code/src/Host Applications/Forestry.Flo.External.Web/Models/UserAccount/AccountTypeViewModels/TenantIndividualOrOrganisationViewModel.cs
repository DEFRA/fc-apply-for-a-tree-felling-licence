using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.External.Web.Models.UserAccount.AccountTypeViewModels;

/// <summary>
/// View model for selecting whether a tenant user is an organisational representative.
/// </summary>
public class TenantIndividualOrOrganisationViewModel : PageWithBreadcrumbsViewModel
{
    /// <summary>
    /// Gets and Sets the organisation status of a trust user.
    /// </summary>
    [Required(ErrorMessage = "Select a type of tenant")]
    public OrganisationStatus? OrganisationStatus { get; set; }
}