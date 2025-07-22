using Forestry.Flo.External.Web.Models.PropertyProfile;
using Forestry.Flo.Services.Applicants.Models;

namespace Forestry.Flo.External.Web.Models.FellingLicenceApplication;

/// <summary>
/// View model for the application summary page.
/// </summary>
public class FellingLicenceApplicationSummaryViewModel
{
    public FellingLicenceApplicationModel Application { get; set; }

    public AgencyModel? Agency { get; set; }

    public WoodlandOwnerModel WoodlandOwner { get; set; }

    public Flo.Services.PropertyProfiles.Entities.PropertyProfile PropertyProfile { get; set; }

    public FellingAndRestockingPlaybackViewModel FellingAndRestocking { get; set; }
}