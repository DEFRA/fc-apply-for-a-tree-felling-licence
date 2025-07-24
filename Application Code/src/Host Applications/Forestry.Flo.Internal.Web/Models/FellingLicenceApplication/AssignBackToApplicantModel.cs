using System.ComponentModel.DataAnnotations;
using Forestry.Flo.Services.Applicants.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Microsoft.AspNetCore.Mvc;

namespace Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;

public class AssignBackToApplicantModel : FellingLicenceApplicationPageViewModel
{
    [HiddenInput] public Guid FellingLicenceApplicationId { get; set; }

    [Required(ErrorMessage = "You must specify an applicant.")]
    public Guid? ExternalApplicantId { get; set; }

    [Required(ErrorMessage = "Please add a comment to explain why this application is being returned.")]
    public string? ReturnToApplicantComment { get; set; }

    public List<UserAccountModel>? ExternalApplicants { get; set; }

    [Required]
    public Dictionary<FellingLicenceApplicationSection, bool> SectionsToReview { get; set; } = new();

    public Dictionary<Guid, bool> CompartmentIdentifiersToReview { get; set; } = new();

    [HiddenInput]
    public string ReturnUrl { get; set; }

    public bool ShowListOfUsers { get; set; }

    public bool LarchCheckSplit { get; set; } = true;

    public RecommendSplitApplicationEnum? LarchCheckSplitRecommendation { get; set; }
}