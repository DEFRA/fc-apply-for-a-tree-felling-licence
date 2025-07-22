using System.ComponentModel.DataAnnotations;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;

namespace Forestry.Flo.External.Web.Models.FellingLicenceApplication;

public class ProposedFellingDetailModel : ApplicationStepBase, IApplicationStep, IApplicationWithBreadcrumbsViewModel
{
    public ProposedFellingDetailModel()
    {
        Species = new Dictionary<string, SpeciesModel>();
    }
    
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the type of the operation.
    /// </summary>
    public FellingOperationType OperationType { get; set; }

    /// <summary>
    /// Gets or sets the restocking details.
    /// </summary>
    public List<ProposedRestockingDetailModel> ProposedRestockingDetails { get; set; } = null!;

    /// <summary>
    /// Gets or sets compartment total hectares.
    /// </summary>
    public double? CompartmentTotalHectares { get; set; }

    /// <summary>
    /// Gets or sets the area to be felled.
    /// </summary>
    public double AreaToBeFelled { get; set; }

    /// <summary>
    /// Gets or sets the no of trees.
    /// </summary>
    public int? NumberOfTrees { get; set; }

    /// <summary>
    /// Gets or sets a flag indicating whether tree marking is used.
    /// </summary>
    public bool? IsTreeMarkingUsed { get; set; }

    /// <summary>
    /// Gets or sets the tree marking.
    /// </summary>
    public string? TreeMarking { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this instance is part of tree preservation order.
    /// </summary>
    public bool? IsPartOfTreePreservationOrder { get; set; }

    /// <summary>
    /// Gets and Sets the Tree Preservation Order reference.
    /// </summary>
    public string? TreePreservationOrderReference { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this instance is within conservation area.
    /// </summary>
    public bool? IsWithinConservationArea { get; set; }

    /// <summary>
    /// Gets and Sets the conservation area reference.
    /// </summary>
    public string? ConservationAreaReference { get; set; }

    public Dictionary<string,SpeciesModel> Species { get; set; } 

    public ApplicationStepStatus Status => OperationType != FellingOperationType.None && AreaToBeFelled != 0
        ?
        ApplicationStepStatus.Completed
        : OperationType != FellingOperationType.None || AreaToBeFelled != 0
            ? ApplicationStepStatus.InProgress
            : ApplicationStepStatus.NotStarted;
    
    [Required(ErrorMessage = "Select whether or not you have finished entering the felling details for this compartment")]
    public bool? StepComplete { get; set; }
    public Guid ApplicationId { get; set; }
    public Guid FellingCompartmentId { get; set; }
    public string? ApplicationReference { get; set; }
    public BreadcrumbsModel? Breadcrumbs { get; set; }

    public string TaskName => "Felling details";

    public string FellingCompartmentName { get; set; }
    public bool? IsRestocking { get; set; }
    public string? NoRestockingReason { get; set; }
    public double EstimatedTotalFellingVolume { get; set; }
}