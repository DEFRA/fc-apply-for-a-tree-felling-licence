using Forestry.Flo.Services.FellingLicenceApplications.Entities;

namespace Forestry.Flo.External.Web.Models.FellingLicenceApplication.ReviewFellingAndRestockingAmendments;

/// <summary>
/// ViewModel representing the details of a felling operation to be displayed in the confirmed felling and restocking summary screen.
/// </summary>
public class RestockingDetailViewModel : CompartmentConfirmedFellingRestockingDetailsModelBase
{
    /// <summary>
    /// Gets or sets the area to be restocked, in hectares.
    /// </summary>
    public double? RestockArea { get; set; }

    /// <summary>
    /// Gets or sets the percentage of open space within the restocked area.
    /// </summary>
    public int? PercentOpenSpace { get; set; }

    /// <summary>
    /// Gets or sets the type of restocking proposal.
    /// </summary>
    public TypeOfProposal? RestockingProposal { get; set; }

    /// <summary>
    /// Gets or sets a comma-separated list of confirmed restocking species and their percentages.
    /// </summary>
    public string ConfirmedRestockingSpecies { get; set; }

    /// <summary>
    /// Gets or sets the density of restocking (e.g., trees per hectare).
    /// </summary>
    public double? RestockingDensity { get; set; }

    /// <summary>
    /// Gets or sets the total number of trees to be restocked.
    /// </summary>
    public int? NumberOfTrees { get; set; }

    /// <summary>
    /// Gets or sets the percentage of restocking achieved through natural regeneration.
    /// </summary>
    public int? PercentNaturalRegeneration { get; set; }

    /// <summary>
    /// Gets or sets a dictionary of amended property names and their previous values.
    /// </summary>
    public Dictionary<string, string> AmendedProperties { get; set; }

    /// <summary>
    /// Returns the old value for a given property name, or the current value if no amendment exists.
    /// </summary>
    /// <param name="propName">The property name to look up.</param>
    /// <param name="currentValue">The current value to return if no amendment exists.</param>
    /// <returns>The old value if found; otherwise, the current value.</returns>
    public object? OldValue(
        string propName,
        object? currentValue = null) =>
        AmendedProperties.TryGetValue(propName, out var oldValue)
            ? oldValue
            : currentValue;

    /// <summary>
    /// Initializes a new instance of the <see cref="RestockingDetailViewModel"/> class using a confirmed restocking detail and compartment details.
    /// </summary>
    /// <param name="model">The confirmed restocking detail model.</param>
    public RestockingDetailViewModel(ConfirmedRestockingDetailViewModel model)
    {
        CompartmentId = model.RestockingCompartmentId;
        CompartmentNumber = model.RestockingCompartmentNumber;
        TotalHectares = model.RestockingCompartmentTotalHectares;
        RestockArea = model.RestockArea;
        PercentOpenSpace = model.PercentOpenSpace;
        RestockingProposal = model.RestockingProposal;
        ConfirmedRestockingSpecies = model.ListRestockingSpecies();
        RestockingDensity = model.RestockingDensity;
        NumberOfTrees = model.NumberOfTrees;
        PercentNaturalRegeneration = model.PercentNaturalRegeneration;
        AmendedProperties = model.AmendedProperties;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RestockingDetailViewModel"/> class using a proposed restocking detail and compartment details.
    /// </summary>
    /// <param name="model">The proposed restocking detail model.</param>
    public RestockingDetailViewModel(Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview.ProposedRestockingDetailModel model)
    {
        CompartmentId = model.CompartmentId;
        CompartmentNumber = model.CompartmentNumber;
        SubCompartmentName = model.SubCompartmentName;
        TotalHectares = model.CompartmentTotalHectares;
        RestockArea = model.Area;
        RestockingProposal = model.RestockingProposal;
        ConfirmedRestockingSpecies = model.ListRestockingSpecies();
        RestockingDensity = model.RestockingDensity;
        NumberOfTrees = model.NumberOfTrees;
        AmendedProperties = [];
    }
}