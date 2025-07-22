namespace Forestry.Flo.External.Web.Models.FellingLicenceApplication;

public class FellingAndRestockingDetailViewModel : IApplicationWithBreadcrumbsViewModel
{
    public FellingAndRestockingDetail FellingAndRestockingDetail { get; set; } = null!;
    public Guid ApplicationId { get; set; }
    public string? ApplicationReference { get; set; }
    public BreadcrumbsModel? Breadcrumbs { get; set; }
    public string TaskName => "Felling and restocking detail";

    public FellingLicenceApplicationSummary ApplicationSummary { get; set; }
}