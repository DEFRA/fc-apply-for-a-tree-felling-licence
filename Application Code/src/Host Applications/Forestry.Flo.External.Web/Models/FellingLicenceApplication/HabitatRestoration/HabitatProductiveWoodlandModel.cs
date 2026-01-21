using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.External.Web.Models.FellingLicenceApplication.HabitatRestoration;

public class HabitatProductiveWoodlandModel : ApplicationStepBase, IApplicationWithBreadcrumbsViewModel
{
    public override Guid ApplicationId { get; set; }
    public string? ApplicationReference { get; set; }
    public BreadcrumbsModel? Breadcrumbs { get; set; }

    public Guid CompartmentId { get; set; }
    public string CompartmentName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Select if the woodland will be productive")]
    public bool? IsProductiveWoodland { get; set; }

    public string TaskName => "Productive woodland";

    public FellingLicenceApplicationSummary? ApplicationSummary { get; set; }
}
