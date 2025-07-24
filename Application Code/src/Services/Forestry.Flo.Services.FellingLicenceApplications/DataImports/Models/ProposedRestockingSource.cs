using System.ComponentModel.DataAnnotations;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;

namespace Forestry.Flo.Services.FellingLicenceApplications.DataImports.Models;

/// <summary>
/// Model class representing a proposed restocking operation as defined in a data import source file.
/// </summary>
public class ProposedRestockingSource
{
    /// <summary>
    /// Gets and sets the identifier of the proposed felling operation that this restocking proposal relates to.
    /// </summary>
    public int ProposedFellingId { get; set; }

    /// <summary>
    /// Gets and sets the type of restocking for this proposed restocking operation.
    /// </summary>
    public TypeOfProposal RestockingProposal { get; set; }

    /// <summary>
    /// Gets and sets the name of the compartment within FLOv2 that this proposed restocking operation is to take place in.
    /// </summary>
    /// <remarks>
    /// This field is mandatory if the restocking operation is <see cref="TypeOfProposal.PlantAnAlternativeArea"/> or
    /// <see cref="TypeOfProposal.PlantAnAlternativeAreaWithIndividualTrees"/>; otherwise, it will be ignored.
    /// </remarks>
    public string? Flov2CompartmentName { get; set; }

    /// <summary>
    /// Gets and sets the area to be restocked for this proposed restocking operation.
    /// </summary>
    public double AreaToBeRestocked { get; set; }

    /// <summary>
    /// Gets and sets the restocking density in stems/ha for this proposed restocking operation.
    /// </summary>
    /// <remarks>
    /// This field is mandatory, unless the restocking operation is <see cref="TypeOfProposal.RestockWithIndividualTrees"/>
    /// or <see cref="TypeOfProposal.PlantAnAlternativeAreaWithIndividualTrees"/> in which case it will be ignored.
    /// </remarks>
    public double? RestockingDensity { get; set; }

    /// <summary>
    /// Gets and sets the number of trees to be restocked for this proposed restocking operation.
    /// </summary>
    /// <remarks>
    /// This field is mandatory if the restocking operation is <see cref="TypeOfProposal.RestockWithIndividualTrees"/>
    /// or <see cref="TypeOfProposal.PlantAnAlternativeAreaWithIndividualTrees"/>; otherwise, it will be ignored.
    /// </remarks>
    public int? NumberOfTrees { get; set; }

    /// <summary>
    /// Gets and sets a comma-separated list of species and their respective percentages for this proposed restocking operation.
    /// </summary>
    public string? SpeciesAndPercentages { get; set; }
}