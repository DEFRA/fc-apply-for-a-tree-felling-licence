using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.External.Web.Models.FellingLicenceApplication.HabitatRestoration;

public class HabitatCompartmentsModel : ApplicationStepBase, IApplicationWithBreadcrumbsViewModel
{
    public override Guid ApplicationId { get; set; }
    public string? ApplicationReference { get; set; }
    public BreadcrumbsModel? Breadcrumbs { get; set; }

    [MinLength(1, ErrorMessage = "Select which compartments are being converted into priority open habitats")]
    public List<Guid> SelectedCompartmentIds { get; set; } = new();

    public string TaskName => "Habitat restoration compartments";
}
