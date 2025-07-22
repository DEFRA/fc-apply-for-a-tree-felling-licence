using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.External.Web.Models.UserAccount.AccountTypeViewModels;

/// <summary>
/// View model for selecting whether a woodland owner user is part of an organisation.
/// </summary>
public class OwnerTypeViewModel : PageWithBreadcrumbsViewModel
{
    /// <summary>
    /// Gets and Sets the organisation status of a woodland owner user.
    /// </summary>
    [Required]
    public OrganisationStatus? OrganisationStatus { get; set; }
}