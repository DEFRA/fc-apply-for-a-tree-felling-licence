using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.Services.Common.User;

public enum AccountTypeExternal
{
    [Display(Name = "Woodland Owner")]
    WoodlandOwner = 0,
    [Display(Name = "Woodland Owner Administrator")]
    WoodlandOwnerAdministrator = 1,
    [Display(Name = "Agent")]
    Agent = 2,
    [Display(Name = "Agent Administrator")]
    AgentAdministrator = 3,
    [Display(Name = "FC User")]
    FcUser = 4
}