using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Forestry.Flo.Internal.Web.Infrastructure;

namespace Forestry.Flo.Internal.Web.Models.UserAccount;

public class AccountPersonNameModel
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
    [Required(ErrorMessage = "A first name must be provided")]
    [DisplayName("First name")]
    [MaxLength(DataValueConstants.NamePartMaxLength)]
    public string? FirstName { get; set; }

    /// <summary>
    /// Gets and Sets the last name of the external user.
    /// </summary>
    [Required(ErrorMessage = "A last name must be provided")]
    [DisplayName("Last name")]
    [MaxLength(DataValueConstants.NamePartMaxLength)]
    public string? LastName { get; set; }
}