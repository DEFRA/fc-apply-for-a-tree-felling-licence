using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.Services.InternalUsers.Entities.UserAccount
{
    public enum Status
    {
        [Display(Name = "Confirmed")]
        Confirmed,
        [Display(Name = "Requested")]
        Requested,
        [Display(Name = "Denied")]
        Denied,
        [Display(Name = "Closed")]
        Closed
    }
}
