using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview;

namespace Forestry.Flo.Internal.Web.Models.WoodlandOfficerReview;

/// <summary>
/// ViewModel representing the details of a felling operation to be displayed in the confirmed felling and restocking summary screen.
/// </summary>
public class FellingDetailViewModel : CompartmentConfirmedFellingRestockingDetailsModelBase
{
    /// <summary>
    /// Gets or sets the area to be felled, in hectares.
    /// </summary>
    public double? AreaToBeFelled { get; set; }

    /// <summary>
    /// Gets or sets the type of felling operation.
    /// </summary>
    public FellingOperationType? OperationType { get; set; }

    /// <summary>
    /// Gets or sets the collection of species involved in the felling.
    /// </summary>
    public string FellingSpecies { get; set; }

    /// <summary>
    /// Gets or sets the number of trees to be felled.
    /// </summary>
    public int? NumberOfTrees { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether tree marking is used.
    /// </summary>
    public bool? IsTreeMarkingUsed { get; set; }

    /// <summary>
    /// Gets or sets the tree marking reference or description.
    /// </summary>
    public string? TreeMarking { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the area is part of a Tree Preservation Order.
    /// </summary>
    public bool? IsPartOfTreePreservationOrder { get; set; }

    /// <summary>
    /// Gets or sets the reference for the Tree Preservation Order, if applicable.
    /// </summary>
    public string? TreePreservationOrderReference { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the area is within a conservation area.
    /// </summary>
    public bool? IsWithinConservationArea { get; set; }

    /// <summary>
    /// Gets or sets the reference for the conservation area, if applicable.
    /// </summary>
    public string? ConservationAreaReference { get; set; }

    /// <summary>
    /// Gets or sets the estimated total felling volume, in cubic meters.
    /// </summary>
    public double? EstimatedTotalFellingVolume { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether there is restocking for this felling.
    /// </summary>
    /// <remarks>
    /// This can be null if restocking is Thinning.
    /// </remarks>
    public bool? IsRestocking { get; set; }

    /// <summary>
    /// Gets or sets the reason for not having restocking.
    /// </summary>
    public string? NoRestockingReason { get; set; }

    /// <summary>
    /// Gets or sets a dictionary containing amended property values, keyed by property name.
    /// The value represents the previous value of the property before amendment, or null if not amended.
    /// </summary>
    public Dictionary<string, string?> AmendedProperties { get; set; }

    /// <summary>
    /// Returns the old value for a given property name, or the current value if no amendment exists.
    /// </summary>
    /// <param name="propName">The property name to look up.</param>
    /// <param name="currentValue">The current value to return if no amendment exists.</param>
    /// <returns>The old value if found; otherwise, the current value.</returns>
    public object? OldValue(string propName, object? currentValue = null) =>
        AmendedProperties.TryGetValue(propName, out var oldValue) 
            ? oldValue 
            : currentValue;

    /// <summary>
    /// Initializes a new instance of the <see cref="FellingDetailViewModel"/> class using a <see cref="ConfirmedFellingDetailViewModel"/>
    /// and a <see cref="CompartmentConfirmedFellingRestockingDetailsModelBase"/> compartment.
    /// </summary>
    /// <param name="model">The confirmed felling detail model containing felling details.</param>
    /// <param name="compartment">The compartment details associated with the felling operation.</param>
    public FellingDetailViewModel(ConfirmedFellingDetailViewModel model, CompartmentConfirmedFellingRestockingDetailsModelBase compartment)
    {
        AreaToBeFelled = model.AreaToBeFelled;
        OperationType = model.OperationType;
        FellingSpecies = model.ListFellingSpecies();
        NumberOfTrees = model.NumberOfTrees;
        IsTreeMarkingUsed = model.IsTreeMarkingUsed;
        TreeMarking = model.TreeMarking;
        IsPartOfTreePreservationOrder = model.IsPartOfTreePreservationOrder;
        TreePreservationOrderReference = model.TreePreservationOrderReference;
        IsWithinConservationArea = model.IsWithinConservationArea;
        ConservationAreaReference = model.ConservationAreaReference;
        EstimatedTotalFellingVolume = model.EstimatedTotalFellingVolume;
        IsRestocking = model.IsRestocking;
        NoRestockingReason = model.NoRestockingReason;
        CompartmentId = compartment.CompartmentId;
        CompartmentNumber = compartment.CompartmentNumber;
        SubCompartmentName = compartment.SubCompartmentName;
        TotalHectares = compartment.TotalHectares;
        Designation = compartment.Designation;
        AmendedProperties = model.AmendedProperties;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FellingDetailViewModel"/> class using a <see cref="ProposedFellingDetailModel"/>
    /// and a <see cref="CompartmentConfirmedFellingRestockingDetailsModelBase"/> compartment.
    /// </summary>
    /// <param name="model">The proposed felling detail model containing felling details.</param>
    /// <param name="compartment">The compartment details associated with the felling operation.</param>
    public FellingDetailViewModel(ProposedFellingDetailModel model, CompartmentConfirmedFellingRestockingDetailsModelBase compartment)
    {
        AreaToBeFelled = model.AreaToBeFelled;
        OperationType = model.OperationType;
        FellingSpecies = model.ListFellingSpecies();
        NumberOfTrees = model.NumberOfTrees;
        IsTreeMarkingUsed = model.IsTreeMarkingUsed;
        TreeMarking = model.TreeMarking;
        IsPartOfTreePreservationOrder = model.IsPartOfTreePreservationOrder;
        TreePreservationOrderReference = model.TreePreservationOrderReference;
        IsWithinConservationArea = model.IsWithinConservationArea;
        ConservationAreaReference = model.ConservationAreaReference;
        EstimatedTotalFellingVolume = model.EstimatedTotalFellingVolume;
        IsRestocking = model.IsRestocking;
        NoRestockingReason = model.NoRestockingReason;
        CompartmentId = compartment.CompartmentId;
        CompartmentNumber = compartment.CompartmentNumber;
        SubCompartmentName = compartment.SubCompartmentName;
        TotalHectares = compartment.TotalHectares;
        Designation = compartment.Designation;
        AmendedProperties = [];
    }
}