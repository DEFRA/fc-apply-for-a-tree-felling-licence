namespace Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;

public class FellingAndRestockingDetails: IApplicationWithBreadcrumbsViewModel
{
    public Guid ApplicationId { get; set; }
    public string? ApplicationReference { get; set; }
    public BreadcrumbsModel? Breadcrumbs { get; set; }
    public string TaskName => " Compartments felling details";

    public bool? ConfirmedFellingAndRestockingCompleted { get; set; }

    public List<FellingAndRestockingDetail> DetailsList { get; set; } = new();
}