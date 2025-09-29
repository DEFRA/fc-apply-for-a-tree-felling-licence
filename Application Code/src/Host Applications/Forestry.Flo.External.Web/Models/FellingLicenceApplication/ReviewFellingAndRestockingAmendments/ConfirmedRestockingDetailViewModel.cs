using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Microsoft.AspNetCore.Mvc;

namespace Forestry.Flo.External.Web.Models.FellingLicenceApplication.ReviewFellingAndRestockingAmendments;

public class NewConfirmedRestockingDetailViewModel
{
    [HiddenInput]
    public Guid ConfirmedFellingDetailsId { get; set; }
    public double? RestockArea { get; set; }

    public int? PercentOpenSpace { get; set; }

    public TypeOfProposal? RestockingProposal { get; set; }

    public ConfirmedRestockingSpeciesModel[] ConfirmedRestockingSpecies { get; set; } = [];

    public double? RestockingDensity { get; set; }

    public int? NumberOfTrees { get; set; }

    public int? PercentNaturalRegeneration { get; set; }

    public Guid RestockingCompartmentId { get; set; }

    public string? RestockingCompartmentNumber { get; set; }

    public double? RestockingCompartmentTotalHectares { get; set; }

    public FellingOperationType OperationType { get; set; }

    /// <summary>
    /// Gets and sets the unique identifier for the proposed restocking associated with this confirmed restocking detail.
    /// </summary>
    /// <remarks>
    /// If this is null, it indicates that the confirmed restocking detail was created without a corresponding proposed restocking detail.
    /// </remarks>
    public Guid? ProposedRestockingDetailsId { get; set; }

    public Dictionary<string, string> AmendedProperties { get; set; } = new Dictionary<string, string>();
    public object? OldValue(string propName, object? currentValue = null)
    {
        if (AmendedProperties.TryGetValue(propName, out var oldValue))
        {
            return oldValue;
        }
        return currentValue;
    }

    /// <summary>
    /// Returns a comma-separated list of actual species names and their percentages from ConfirmedRestockingSpecies,
    /// using TreeSpeciesFactory.SpeciesDictionary for name lookup.
    /// Ignores null, empty, or deleted species.
    /// Example: "Ash: 80%, Alder: 20%"
    /// </summary>
    public string ListRestockingSpecies()
    {
        return string.Join(
            ", ",
            ConfirmedRestockingSpecies
                .Where(s => !s.Deleted && !string.IsNullOrWhiteSpace(s.Species))
                .Select(s =>
                {
                    var name = TreeSpeciesFactory.SpeciesDictionary.TryGetValue(s.Species!, out var speciesModel)
                        ? speciesModel.Name
                        : s.Species;
                    var percent = s.Percentage.HasValue ? $"{s.Percentage.Value}%" : "";
                    return string.IsNullOrEmpty(percent) ? name : $"{name}: {percent}";
                })
        );
    }
}

/// <summary>
/// Represents the view model for confirmed restocking details, including a unique identifier.
/// </summary>
/// <remarks>This class extends <see cref="NewConfirmedRestockingDetailViewModel"/> to include additional
/// information specific to confirmed restocking details.</remarks>
public class ConfirmedRestockingDetailViewModel : NewConfirmedRestockingDetailViewModel
{
    /// <summary>
    /// Gets or sets the unique identifier for the confirmed restocking details.
    /// </summary>
    [HiddenInput]
    public Guid ConfirmedRestockingDetailsId { get; set; }

    /// <summary>
    /// Determines the type of confirmed restocking operation by comparing the current details
    /// to the proposed restocking details and any amendments made.
    /// </summary>
    /// <returns>
    /// A <see cref="ConfirmedRestockingType"/> value indicating whether the restocking detail is new,
    /// amended, or unmodified compared to the proposed details.
    /// </returns>
    public ConfirmedRestockingType GetConfirmedRestockingType()
    {
        if (ProposedRestockingDetailsId is null)
        {
            return ConfirmedRestockingType.NewRestocking;
        }

        return AmendedProperties.Count > 0
            ? ConfirmedRestockingType.Amended
            : ConfirmedRestockingType.Unmodified;
    }
}

/// <summary>
/// An enumeration of possible confirmed restocking operation types based on comparison to proposed details.
/// </summary>
public enum ConfirmedRestockingType
{
    /// <summary>
    /// The restocking details have not been modified from the proposed details.
    /// </summary>
    Unmodified,
    /// <summary>
    /// The restocking detail has been modified.
    /// </summary>
    Amended,
    /// <summary>
    /// The restocking detail is new.
    /// </summary>
    NewRestocking,
}