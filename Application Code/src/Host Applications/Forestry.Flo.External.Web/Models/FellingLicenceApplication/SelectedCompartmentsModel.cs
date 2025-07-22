using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.External.Web.Models.FellingLicenceApplication;

public class SelectedCompartmentsModel: ApplicationStepBase, IApplicationWithBreadcrumbsViewModel
{
    public override Guid ApplicationId { get; set; }

    public List<Guid>? SelectedCompartmentIds { get; set; }

    public Guid PropertyProfileId { get; set; }

    public string? ApplicationReference { get; set; }

    public string? GIS { get; set; }

    public BreadcrumbsModel? Breadcrumbs { get; set; }

    public string TaskName => "Select compartments";

    public override bool? StepComplete { get; set; }

    public bool? ConstraintCheckStepComplete { get; set; }

    public List<FellingAndRestockingDetail> DetailsList { get; set; } = new();

    public bool IsForRestockingCompartmentSelection { get;set; }
    public FellingOperationType? FellingOperationType { get; set; }
    public string? FellingCompartmentName { get; set; }
    public Guid? FellingCompartmentId { get; set; }
    public Guid? ProposedFellingDetailsId { get; set; }
}