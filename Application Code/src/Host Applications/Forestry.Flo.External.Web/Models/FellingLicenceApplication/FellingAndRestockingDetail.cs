using Forestry.Flo.External.Web.Models.Compartment;

namespace Forestry.Flo.External.Web.Models.FellingLicenceApplication;

public class FellingAndRestockingDetail : ApplicationStepBase, IApplicationWithBreadcrumbsViewModel
{
    public override Guid ApplicationId { get; set; }
    public Guid WoodlandId { get; set; }
    public Guid CompartmentId { get; set; }
    public List<ProposedFellingDetailModel> FellingDetails { get; set; } = null!;
    public string CompartmentName { get; set; } = null!;
    public string? ApplicationReference { get; set; }
    public BreadcrumbsModel? Breadcrumbs { get; set; }
    public string TaskName => "Felling and restocking details";
    public string Tab { get; set; } = null!;
    public List<CompartmentModel> Compartments { get; set; } = new List<CompartmentModel>();
    /// <summary>
    /// Get or sets the new StepComplete with the unique error message for this model
    /// </summary>
    public override bool? StepComplete { get; set; }
    public bool MapIsNotSet => FellingDetails.Exists(f => !f.CompartmentTotalHectares.HasValue);
    public double TotalCompartmentHectares => FellingDetails?.Sum(f => f.CompartmentTotalHectares ?? 0D) ?? 0D;
    public Guid WoodlandOwnerId { get; set; }
}