using Forestry.Flo.Services.Common.Models;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;


public class LarchFlyoverModel : FellingLicenceApplicationPageViewModel
{

    public DatePart? FlyoverDate { get; set; } = null!;

    [Required]
    public string? FlightObservations { get; set; }

    /// <summary>
    /// Gets and sets the application ID.
    /// </summary>
    [HiddenInput]
    public Guid ApplicationId { get; set; }

    public IList<ActivityFeedItemModel>? ActivityFeedItems { get; set; }

    /// <summary>
    /// Gets a flag indicating whether the application state and current user should be able
    /// to edit the larch details for the application.
    /// </summary>
    public bool Disabled { get; set; }

    public string? CaseNote { get; set; }
    public bool VisibleToApplicant { get; set; }
    public bool VisibleToConsultee { get; set; }

    public DateTime? SubmissionDate { get; set; }
}