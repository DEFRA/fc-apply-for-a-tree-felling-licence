using Forestry.Flo.Services.FellingLicenceApplications.Models;

namespace Forestry.Flo.External.Web.Models.FellingLicenceApplication;

public class ViewCaseNotesViewModel : IApplicationWithBreadcrumbsViewModel
{
    public ActivityFeedViewModel ActivityFeedViewModel { get; set; }

    public Guid ApplicationId { get; set; }

    public string? ApplicationReference { get; set; }

    public BreadcrumbsModel? Breadcrumbs { get; set; }

    public string TaskName => "View case notes";
}