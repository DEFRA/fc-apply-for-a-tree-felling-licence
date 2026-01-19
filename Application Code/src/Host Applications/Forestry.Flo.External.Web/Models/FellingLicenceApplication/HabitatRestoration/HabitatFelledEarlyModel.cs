using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.External.Web.Models.FellingLicenceApplication.HabitatRestoration;

public class HabitatFelledEarlyModel : ApplicationStepBase, IApplicationWithBreadcrumbsViewModel
{
    public override Guid ApplicationId { get; set; }
    public string? ApplicationReference { get; set; }
    public BreadcrumbsModel? Breadcrumbs { get; set; }

    public Guid CompartmentId { get; set; }
    public string CompartmentName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Select if the trees have been felled early")]
    public bool? IsFelledEarly { get; set; }

    public string TaskName => "Felled early";

    public FellingLicenceApplicationSummary? ApplicationSummary { get; set; }
}
