using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.External.Web.Models.UserAccount.AccountTypeViewModels;

/// <summary>
/// View model for selecting the type of tenant.
/// </summary>
public class TenantTypeViewModel : PageWithBreadcrumbsViewModel
{
    /// <summary>
    /// Gets and Sets the type of tenant.
    /// </summary>
    [Required]
    public TenantType? TenantType { get; set; }
}