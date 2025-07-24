using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.External.Web.Models.UserAccount;

/// <summary>
/// Represents options available for account type during registration on the external application.
/// </summary>
public enum AccountType
{
    [Display(Name = "Agent")]
    [Description("You are making this application on behalf of someone else.")]
    Agent,
    [Display(Name = "Owner")]
    [Description("You or your organisation own the land.")]
    WoodlandOwner,
    [Display(Name = "Tenant")]
    [Description("You or your organisation are tenants on the land.")]
    Tenant,
    [Display(Name = "Trust")]
    [Description("A non-governmental organisation working for a charitable purpose.")]
    Trust,

    [Display(Name = "FC User")]
    [Description("A user from the Forestry Commission.")]
    FcUser
}