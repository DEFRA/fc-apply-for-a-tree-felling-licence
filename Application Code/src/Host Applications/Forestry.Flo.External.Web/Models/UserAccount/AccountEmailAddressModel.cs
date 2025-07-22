using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Forestry.Flo.Services.Common.Infrastructure;

namespace Forestry.Flo.External.Web.Models.UserAccount;

public class AccountEmailAddressModel
{
    /// <summary>
    /// Gets and Sets the email address of the external user.
    /// </summary>
    [Required(ErrorMessage = "Enter an email address in the correct format, like name@example.com")]
    [DisplayName("Email address")]
    [FloEmailAddress(ErrorMessage = "Enter an email address in the correct format, like name@example.com")]
    public string? Email { get; set; }
}