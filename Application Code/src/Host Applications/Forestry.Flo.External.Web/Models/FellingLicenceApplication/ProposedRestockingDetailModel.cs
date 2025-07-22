using System.ComponentModel.DataAnnotations;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;

namespace Forestry.Flo.External.Web.Models.FellingLicenceApplication;

public class ProposedRestockingDetailModel : ApplicationStepBase, IApplicationWithBreadcrumbsViewModel
{

    public ProposedRestockingDetailModel()
    {
        Species = new Dictionary<string, SpeciesModel>();
    }
    
    /// <summary>
    /// Gets and Sets the property document ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the restocking proposal.
    /// </summary>
    public TypeOfProposal RestockingProposal { get; set; }

    /// <summary>
    /// Gets or sets the type of the operation.
    /// </summary>
    public FellingOperationType OperationType { get; set; }

    /// <summary>
    /// Gets or sets the area.
    /// </summary>
    public double? Area { get; set; }
    
    /// <summary>
    /// Gets or sets compartment total hectares.
    /// </summary>
    public double? CompartmentTotalHectares { get; set; }

    /// <summary>
    /// Gets or sets the percentage of restock area.
    /// </summary>
    public double? PercentageOfRestockArea { get; set; }

    /// <summary>
    /// Gets or sets the restocking density.
    /// </summary>
    public double? RestockingDensity { get; set; }

    /// <summary>
    /// Gets or sets the no of trees.
    /// </summary>
    public int? NumberOfTrees { get; set; }


    /// <summary>
    /// Gets or sets the restocking species.
    /// </summary>
    public Dictionary<string,SpeciesModel> Species { get; set; } 

    public ApplicationStepStatus Status => RestockingDensity != 0 && RestockingProposal != TypeOfProposal.None
        ?
        ApplicationStepStatus.Completed
        : RestockingDensity != 0 || RestockingProposal != TypeOfProposal.None
            ? ApplicationStepStatus.InProgress
            : ApplicationStepStatus.NotStarted;
    
    [Required(ErrorMessage = "Select whether or not you have finished entering the restocking details for this compartment")]
    public bool? StepComplete { get; set; }
    public Guid ApplicationId { get; set; }
    public Guid RestockingCompartmentId { get; set; }
    public Guid FellingCompartmentId { get; set; }
    public Guid ProposedFellingDetailsId { get; set; }
    public string? ApplicationReference { get; set; }
    public BreadcrumbsModel? Breadcrumbs { get; set; }

    public string TaskName => "Restocking details";

    public string CompartmentName { get; set; }
}