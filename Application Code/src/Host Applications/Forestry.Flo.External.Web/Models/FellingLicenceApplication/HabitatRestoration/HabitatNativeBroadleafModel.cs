using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.External.Web.Models.FellingLicenceApplication.HabitatRestoration;

public class HabitatNativeBroadleafModel : ApplicationStepBase, IApplicationWithBreadcrumbsViewModel
{
    public override Guid ApplicationId { get; set; }
    public string? ApplicationReference { get; set; }
    public BreadcrumbsModel? Breadcrumbs { get; set; }

    public Guid CompartmentId { get; set; }
    public string CompartmentName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Select if the compartment is native broadleaf")]
    public bool? IsNativeBroadleaf { get; set; }

    public string TaskName => "Native broadleaf";

    public FellingLicenceApplicationSummary? ApplicationSummary { get; set; }
}
