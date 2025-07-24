using Forestry.Flo.Internal.Web.Infrastructure;
using Forestry.Flo.Services.Common.User;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.Internal.Web.Models.UserAccount;

/// <summary>
/// Model class representing a user's account of the system.
/// </summary>
public class UserRegistrationDetailsModel : PageWithBreadcrumbsViewModel
{
    /// <summary>
    /// Gets and Sets the title of the external user.
    /// </summary>
    [DisplayName("Title")]
    [DisplayAsOptional]
    [MaxLength(DataValueConstants.NamePartMaxLength)]
    public string? Title { get; set; }

    /// <summary>
    /// Gets and Sets the first name of the external user.
    /// </summary>
    [Required(ErrorMessage = "Your first name must be provided")]
    [DisplayName("First name")]
    [MaxLength(DataValueConstants.NamePartMaxLength)]
    public string? FirstName { get; set; }

    /// <summary>
    /// Gets and Sets the last name of the external user.
    /// </summary>
    [Required(ErrorMessage = "Your last name must be provided")]
    [DisplayName("Last name")]
    [MaxLength(DataValueConstants.NamePartMaxLength)]
    public string? LastName { get; set; }

    [Required(ErrorMessage = "A job title is required")]
    [DisplayName("Job title")]
    public AccountTypeInternal RequestedAccountType { get; set; }

    [DisplayName("Other job title")]
    public AccountTypeInternalOther? RequestedAccountTypeOther { get; set; }

    public List<AccountTypeInternal> DisallowedRoles { get;} = new()
    {
        AccountTypeInternal.FcStaffMember
    };

    public bool AllowRoleChange { get; set; }

    public bool AllowSetCanApproveApplications { get; protected set; }

    /// <summary>
    /// Gets and sets whether or not the user is allowed to approve/refuse applications.
    /// </summary>
    [DisplayName("Can approve/refuse applications?")]
    public bool CanApproveApplications { get; set; }
}