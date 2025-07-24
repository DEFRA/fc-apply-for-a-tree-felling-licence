using System.ComponentModel;

namespace Forestry.Flo.External.Web.Models.UserAccount;

public enum WoodlandOwnerUserRole
{
    [Description("Administrator (can make changes and can add or remove administrators and editors)")]
    WoodlandOwnerAdministrator,
    [Description("Editor (can make minor changes)")]
    WoodlandOwnerUser,
}