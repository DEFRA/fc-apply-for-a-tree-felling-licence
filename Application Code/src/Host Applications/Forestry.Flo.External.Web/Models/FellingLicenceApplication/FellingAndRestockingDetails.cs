namespace Forestry.Flo.External.Web.Models.FellingLicenceApplication;

public class FellingAndRestockingDetails: ApplicationStepBase,IApplicationWithBreadcrumbsViewModel
{
    public override Guid ApplicationId { get; set; }
    public string? ApplicationReference { get; set; }
    public BreadcrumbsModel? Breadcrumbs { get; set; }
    public string TaskName => "Felling and restocking";
    //public ApplicationStepStatus Status =>
    //    DetailsList.All(c => c.Status == ApplicationStepStatus.Completed) ? ApplicationStepStatus.Completed
    //    : DetailsList.Any(c => c.Status == ApplicationStepStatus.InProgress || c.Status == ApplicationStepStatus.Completed) ? ApplicationStepStatus.InProgress
    //    : ApplicationStepStatus.NotStarted;

    public List<FellingAndRestockingDetail> DetailsList { get; set; } = new();

    public int CompletedCount => DetailsList.Count(c => c.Status == ApplicationStepStatus.Completed);
}