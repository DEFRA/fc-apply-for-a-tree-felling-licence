using System.ComponentModel.DataAnnotations;
using CsvHelper.Configuration.Attributes;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;

namespace Forestry.Flo.Services.FellingLicenceApplications.DataImports.Models;

/// <summary>
/// Model class representing a proposed felling operation as defined in a data import source file.
/// </summary>
public class ProposedFellingSource
{
    /// <summary>
    /// Gets and sets the unique identifier for the proposed felling operation within the import file set.
    /// </summary>
    [Name("proposedfellingid")]
    public int ProposedFellingId { get; set; }

    /// <summary>
    /// Gets and sets the unique identifier for the application that this proposed felling operation relates to.
    /// </summary>
    [Name("applicationid")]
    public int ApplicationId { get; set; }

    /// <summary>
    /// Gets and sets the name of the compartment within FLOv2 that this proposed felling operation relates to.
    /// </summary>
    [Name("flov2compartmentname")]
    public string Flov2CompartmentName { get; set; }

    /// <summary>
    /// Gets and sets the felling operation type for this proposed felling operation.
    /// </summary>
    [Name("operationtype")]
    public FellingOperationType OperationType { get; set; }

    /// <summary>
    /// Gets and sets the area to be felled for this felling operation.
    /// </summary>
    [Name("areatobefelled")]
    public double AreaToBeFelled { get; set; }

    /// <summary>
    /// Gets and sets the optional number of trees to be felled for this felling operation.
    /// </summary>
    /// <remarks>
    /// This field is mandatory for a <see cref="FellingOperationType.FellingIndividualTrees"/> operation.
    /// </remarks>
    [Name("numberoftrees")]
    public int? NumberOfTrees { get; set; }

    /// <summary>
    /// Gets and sets the estimated total felling volume for this proposed felling operation.
    /// </summary>
    [Name("estimatedtotalfellingvolume")]
    public double EstimatedTotalFellingVolume { get; set; }

    /// <summary>
    /// Gets and sets an optional tree marking description for this proposed felling operation.
    /// </summary>
    [Name("treemarking")]
    public string? TreeMarking { get; set; }

    /// <summary>
    /// Gets and sets whether the trees being felled in this operation are part of a tree preservation order.
    /// </summary>
    [Name("ispartoftreepreservationorder")]
    public bool IsPartOfTreePreservationOrder { get; set; }

    /// <summary>
    /// Gets and sets the reference for the tree preservation order, if applicable.
    /// </summary>
    [Name("treepreservationorderreference")]
    public string? TreePreservationOrderReference { get; set; }

    /// <summary>
    /// Gets and sets whether the proposed felling operation is within a conservation area.
    /// </summary>
    [Name("iswithinconservationarea")]
    public bool IsWithinConservationArea { get; set; }

    /// <summary>
    /// Gets and sets the reference for the conservation area, if applicable.
    /// </summary>
    [Name("conservationareareference")]
    public string? ConservationAreaReference { get; set; }

    /// <summary>
    /// Gets and sets whether there is any proposed restocking associated with this felling operation.
    /// </summary>
    [Name("isrestocking")]
    public bool IsRestocking { get; set; }

    /// <summary>
    /// Gets and sets the reason for no restocking, if applicable.
    /// </summary>
    [Name("norestockingreason")]
    public string? NoRestockingReason { get; set; }

    /// <summary>
    /// Gets and sets a comma-separated list of species codes for the species to be felled as part of this felling operation.
    /// </summary>
    [Name("species")]
    public string Species { get; set; }
}