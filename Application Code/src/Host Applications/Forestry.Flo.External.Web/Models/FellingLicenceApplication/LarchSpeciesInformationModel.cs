using Forestry.Flo.Services.FellingLicenceApplications.Entities;

namespace Forestry.Flo.External.Web.Models.FellingLicenceApplication;

public class LarchSpeciesInformationModel : ApplicationStepBase, IApplicationWithBreadcrumbsViewModel
{
    public FellingOperationType OperationType { get; set; }

    public Guid FellingCompartmentId { get; set; }
    public Guid ProposedFellingDetailsId { get; set; }
    public string? ApplicationReference { get; set; }
    public BreadcrumbsModel? Breadcrumbs { get; set; }

    public string TaskName => "Felling details";

}