using System.ComponentModel.DataAnnotations;
using Forestry.Flo.Services.FellingLicenceApplications.Models;

namespace Forestry.Flo.External.Web.Models.FellingLicenceApplication.HabitatRestoration;

public class WoodlandSpeciesTypeModel : ApplicationStepBase, IApplicationWithBreadcrumbsViewModel
{
    public override Guid ApplicationId { get; set; }
    public string? ApplicationReference { get; set; }
    public BreadcrumbsModel? Breadcrumbs { get; set; }

    public Guid CompartmentId { get; set; }
    public string CompartmentName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Select a woodland species type")]
    public WoodlandSpeciesType? SelectedSpeciesType { get; set; }

    public string TaskName => "Woodland species type";

    public FellingLicenceApplicationSummary? ApplicationSummary { get; set; }
}
