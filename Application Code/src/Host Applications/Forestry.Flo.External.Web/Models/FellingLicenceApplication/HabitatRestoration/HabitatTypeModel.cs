using System.ComponentModel.DataAnnotations;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;

namespace Forestry.Flo.External.Web.Models.FellingLicenceApplication.HabitatRestoration;

public class HabitatTypeModel : ApplicationStepBase, IApplicationWithBreadcrumbsViewModel, IValidatableObject
{
    public override Guid ApplicationId { get; set; }
    public string? ApplicationReference { get; set; }
    public BreadcrumbsModel? Breadcrumbs { get; set; }

    public Guid CompartmentId { get; set; }
    public string CompartmentName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Select a habitat type")]
    public HabitatType? SelectedHabitatType { get; set; }
    public string? OtherHabitatDescription { get; set; }

    public string TaskName => "Habitat type";

    public FellingLicenceApplicationSummary? ApplicationSummary { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (SelectedHabitatType == HabitatType.Other && string.IsNullOrWhiteSpace(OtherHabitatDescription))
        {
            yield return new ValidationResult("Enter a description for the other habitat type", new[] { nameof(OtherHabitatDescription) });
        }
    }
}
