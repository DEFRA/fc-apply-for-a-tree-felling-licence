using System.ComponentModel.DataAnnotations;
using Forestry.Flo.Services.Common.Extensions;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Microsoft.AspNetCore.Mvc;
using Forestry.Flo.Services.FellingLicenceApplications.Models; // Add this for TreeSpeciesFactory

namespace Forestry.Flo.Internal.Web.Models.WoodlandOfficerReview;

/// <summary>
/// ViewModel representing the details of a confirmed felling operation, including area, operation type, species, and related restocking details.
/// </summary>
public class NewConfirmedFellingDetailViewModel
{
    /// <summary>
    /// Gets or sets the area to be felled, in hectares.
    /// </summary>
    [Required]
    public double? AreaToBeFelled { get; set; }

    /// <summary>
    /// Gets or sets the type of felling operation.
    /// </summary>
    [Required]
    public FellingOperationType? OperationType { get; set; }

    /// <summary>
    /// Gets or sets the collection of species involved in the confirmed felling.
    /// </summary>
    public ConfirmedFellingSpeciesModel[] ConfirmedFellingSpecies { get; set; } = [];

    /// <summary>
    /// Gets or sets the number of trees to be felled.
    /// </summary>
    [Required]
    public int? NumberOfTrees { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether tree marking is used.
    /// </summary>
    [Required]
    public bool? IsTreeMarkingUsed { get; set; }

    /// <summary>
    /// Gets or sets the tree marking reference or description.
    /// </summary>
    public string? TreeMarking { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the area is part of a Tree Preservation Order.
    /// </summary>
    [Required]
    public bool? IsPartOfTreePreservationOrder { get; set; }

    /// <summary>
    /// Gets or sets the reference for the Tree Preservation Order, if applicable.
    /// </summary>
    public string? TreePreservationOrderReference { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the area is within a conservation area.
    /// </summary>
    [Required]
    public bool? IsWithinConservationArea { get; set; }

    /// <summary>
    /// Gets or sets the reference for the conservation area, if applicable.
    /// </summary>
    public string? ConservationAreaReference { get; set; }

    /// <summary>
    /// Gets or sets the collection of restocking details associated with the confirmed felling.
    /// </summary>
    public ConfirmedRestockingDetailViewModel[] ConfirmedRestockingDetails { get; set; } = [];

    /// <summary>
    /// Gets or sets the estimated total felling volume, in cubic meters.
    /// </summary>
    [Required]
    public double? EstimatedTotalFellingVolume { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether there is restocking for this felling.
    /// </summary>
    [Required]
    public bool? IsRestocking { get; set; }

    /// <summary>
    /// Gets or sets the reason for not having restocking.
    /// </summary>
    public string? NoRestockingReason { get; set; }

    public Dictionary<string, string?> AmendedProperties { get; set; } = new Dictionary<string, string?>();

    public object? OldValue(string propName, object? currentValue = null)
    {
        if (AmendedProperties.TryGetValue(propName, out var oldValue))
        {
            return oldValue;
        }
        return currentValue;
    }

    /// <summary>
    /// Returns a comma-separated list of actual species names from ConfirmedFellingSpecies,
    /// using TreeSpeciesFactory.SpeciesDictionary for name lookup.
    /// Ignores null, empty, or deleted species.
    /// </summary>
    public string ListFellingSpecies()
    {
        return string.Join(
            ", ",
            ConfirmedFellingSpecies
                .Where(s => !s.Deleted && !string.IsNullOrWhiteSpace(s.Species))
                .Select(s =>
                    TreeSpeciesFactory.SpeciesDictionary.TryGetValue(s.Species!, out var speciesModel)
                        ? speciesModel.Name
                        : s.Species
                )
        );
    }
}

public class ConfirmedFellingDetailViewModel : NewConfirmedFellingDetailViewModel
{
    /// <summary>
    /// Gets or sets the unique identifier for the confirmed felling details.
    /// </summary>
    [HiddenInput]
    public Guid ConfirmedFellingDetailsId { get; set; }

    /// <summary>
    /// Gets or sets the identifier for the proposed felling details, if applicable.
    /// </summary>
    [HiddenInput]
    public Guid? ProposedFellingDetailsId { get; set; }

    /// <summary>
    /// Determines the type of confirmed felling operation by comparing the current details to their proposed counterparts.
    /// </summary>
    /// <param name="containsDeletedRestocking">
    /// Indicates whether any restocking details have been deleted.
    /// </param>
    /// <returns>
    /// A <see cref="ConfirmedFellingType"/> value representing the type of confirmed felling operation:
    /// <list type="bullet">
    /// <item><see cref="ConfirmedFellingType.DeletedRestocking"/> if any restocking details have been deleted.</item>
    /// <item><see cref="ConfirmedFellingType.NewFelling"/> if there is no corresponding proposed felling detail.</item>
    /// <item><see cref="ConfirmedFellingType.NewRestocking"/> if any new restocking details have been added.</item>
    /// <item><see cref="ConfirmedFellingType.Amended"/> if any felling details or associated restocking details have been amended.</item>
    /// <item><see cref="ConfirmedFellingType.Unmodified"/> if none of the above conditions are met.</item>
    /// </list>
    /// </returns>
    public ConfirmedFellingType GetConfirmedFellingType(bool containsDeletedRestocking)
    {
        if (containsDeletedRestocking)
        {
            return ConfirmedFellingType.DeletedRestocking;
        }

        if (ProposedFellingDetailsId.HasNoValue())
        {
            return ConfirmedFellingType.NewFelling;
        }

        if (ConfirmedRestockingDetails.Any(x => x.ProposedRestockingDetailsId.HasNoValue()))
        {
            return ConfirmedFellingType.NewRestocking;
        }

        if (AmendedProperties.Count > 0 ||
            ConfirmedRestockingDetails.Any(x => x.AmendedProperties.Count > 0))
        {
            return ConfirmedFellingType.Amended;
        }

        return ConfirmedFellingType.Unmodified;
    }
}

/// <summary>
/// Represents the type of confirmed felling operation based on comparison to its respective proposed details.
/// </summary>
/// <remarks>
/// Note this does not include the case where the felling operation is deleted, as there is no confirmed felling in that case.
/// </remarks>
public enum ConfirmedFellingType
{
    /// <summary>
    /// The felling details have not been modified from the proposed details.
    /// </summary>
    Unmodified,
    /// <summary>
    /// At least one restocking detail has been deleted from the felling operation.
    /// </summary>
    DeletedRestocking,
    /// <summary>
    /// Either the felling operation or at least one restocking detail has been modified.
    /// </summary>
    Amended,
    /// <summary>
    /// The felling operation is new and does not have a corresponding proposed felling detail.
    /// </summary>
    NewFelling,
    /// <summary>
    /// At least one new restocking detail has been added to the felling operation.
    /// </summary>
    NewRestocking,
}