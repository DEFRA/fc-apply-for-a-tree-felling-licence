using Forestry.Flo.Services.FellingLicenceApplications.Models;
using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.External.Web.Models.FellingLicenceApplication.HabitatRestoration;

public class PriorityOpenHabitatsViewModel : ApplicationStepBase, IApplicationWithBreadcrumbsViewModel
{
    public string? ApplicationReference { get; set; }
    public BreadcrumbsModel? Breadcrumbs { get; set; }
    public FellingLicenceApplicationSummary? ApplicationSummary { get; set; }

    [Required(ErrorMessage = "Select Yes or No")]
    public bool? IsPriorityOpenHabitat { get; set; }

    public string TaskName => "Priority open habitats";
}