namespace Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;

public class FellingAndRestockingDetail : IApplicationWithBreadcrumbsViewModel
{
    public Guid WoodlandId { get; set; }
    public Guid CompartmentId { get; set; }
    public ProposedFellingDetailModel FellingDetail { get; set; } = null!;
    public ProposedRestockingDetailModel RestockingDetail { get; set; } = null!;
    public string CompartmentName { get; set; } = null!;
    // ReSharper disable once InconsistentNaming
    public string? GISData { get; set; }
    public double? CompartmentTotalHectares { get; set; }
    public Guid ApplicationId { get; set; }
    public string? ApplicationReference { get; set; }
    public BreadcrumbsModel? Breadcrumbs { get; set; }
    public string TaskName => "Felling and restocking details";
    public string ActionName { get; set; } = null!;
    public string Tab { get; set; } = null!;
    public string Felling { get; set; } = string.Empty;
    public string RestockingCompartment { get; set; } = string.Empty;

    public string? RestockingGISData { get; set; }
    public bool Zone1 { get; set; }
    public bool Zone2 { get; set; }
    public bool Zone3 { get; set; }
}