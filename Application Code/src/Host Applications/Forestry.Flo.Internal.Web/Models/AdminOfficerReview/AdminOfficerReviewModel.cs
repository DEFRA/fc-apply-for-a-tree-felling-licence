using System.ComponentModel.DataAnnotations;
using Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models.AdminOfficerReview;
using Microsoft.AspNetCore.Mvc;

namespace Forestry.Flo.Internal.Web.Models.AdminOfficerReview;

public class AdminOfficerReviewModel : FellingLicenceApplicationPageViewModel
{
    /// <summary>
    /// Gets and sets the application ID.
    /// </summary>
    [HiddenInput]
    public Guid ApplicationId { get; set; }

    public ActivityFeedModel AdminOfficerReviewCommentsFeed { get; set; }

    [Required(ErrorMessage = "The date at which the application was received must be specified")]
    public DatePart? DateReceived { get; set; } = null!;

    public FellingLicenceApplicationSource ApplicationSource { get; set; }

    public string? AssignedWoodlandOfficer { get; set; }

    /// <summary>
    /// Gets and sets a populated <see cref="Flo.Services.FellingLicenceApplications.Models.AdminOfficerReview.AdminOfficerReviewTaskListStates"/> indicating the status of each
    /// step of the admin officer review process.
    /// </summary>
    [Required]
    public AdminOfficerReviewTaskListStates AdminOfficerReviewTaskListStates { get; set; } = null!;

    public bool RequireWOReview { get; init; }


    /// <summary>
    /// Gets a flag indicating whether the application state and current user should be able
    /// to edit the admin officer review details for the application.
    /// </summary>
    public bool Editable { get; set; }

    /// <summary>
    /// Gets and sets a flag indicating whether the application is an agent application.
    /// </summary>
    public bool AgentApplication { get; set; }
}