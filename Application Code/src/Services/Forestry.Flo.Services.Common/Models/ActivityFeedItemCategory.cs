using Forestry.Flo.Services.Common.Infrastructure;
using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.Services.Common.Models;

public enum ActivityFeedItemCategory
{
    CaseNote,
    Notification,
    [Display(Name = "Outgoing Notification")]
    OutgoingNotification,
    [Display(Name = "Amendment reviews")]
    AmendmentReviews
}