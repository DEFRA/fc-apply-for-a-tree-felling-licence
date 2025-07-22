using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.External.Web.Models.UserAccount;

public class UserTypeModel
{
    [Required(ErrorMessage = "Select an account type")]
    public AccountType? AccountType { get; set; }

    public bool IsOrganisation { get; set; }
}