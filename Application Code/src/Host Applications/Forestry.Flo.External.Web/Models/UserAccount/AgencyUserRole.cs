using System.ComponentModel;

namespace Forestry.Flo.External.Web.Models.UserAccount;

public enum AgencyUserRole
{
    [Description("Agency User")]
    AgencyUser,
    [Description("Agency Administrator")]
    AgencyAdministrator,
}