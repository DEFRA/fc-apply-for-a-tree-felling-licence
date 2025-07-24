using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.External.Web.Models.Home;

public class AccountHolderConfirmationPageModel
{
    [Required(ErrorMessage = "An option must be selected")]
    public bool? IsAccountHolder { get; set; } = null;
}