using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.External.Web.Models.UserAccount.AccountTypeViewModels;

/// <summary>
/// View model for details pertaining to a Crown Land tenant's landlord.
/// </summary>
public class LandlordDetailsViewModel : PageWithBreadcrumbsViewModel
{
    /// <summary>
    /// Gets and Sets the first name of a landlord.
    /// </summary>
    [Required(ErrorMessage = "Enter your landlord's first name")]
    [DisplayName("First name")]
    [MaxLength(DataValueConstants.NamePartMaxLength)]
    public string? FirstName { get; set; }

    /// <summary>
    /// Gets and Sets the last name of a landlord.
    /// </summary>
    [Required(ErrorMessage = "Enter your landlord's last name")]
    [DisplayName("Last name")]
    [MaxLength(DataValueConstants.NamePartMaxLength)]
    public string? LastName { get; set; }
}