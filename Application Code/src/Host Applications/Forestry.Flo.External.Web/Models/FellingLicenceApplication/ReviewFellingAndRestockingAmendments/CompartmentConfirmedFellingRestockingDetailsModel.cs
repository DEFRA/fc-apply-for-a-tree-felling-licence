using Microsoft.AspNetCore.Mvc;

namespace Forestry.Flo.External.Web.Models.FellingLicenceApplication.ReviewFellingAndRestockingAmendments;

/// <summary>
/// Base model containing common compartment details for confirmed felling and restocking operations.
/// </summary>
public class CompartmentConfirmedFellingRestockingDetailsModelBase
{
    /// <summary>
    /// Gets or sets the unique identifier for the compartment.
    /// </summary>
    [HiddenInput]
    public Guid? CompartmentId { get; set; }

    [HiddenInput]
    public Guid? SubmittedFlaPropertyCompartmentId { get; set; }

    /// <summary>
    /// Gets or sets the compartment number.
    /// </summary>
    public string? CompartmentNumber { get; set; }

    /// <summary>
    /// Gets the compartment name, which is the same as the compartment number.
    /// </summary>
    public string? CompartmentName => CompartmentNumber;

    /// <summary>
    /// Gets or sets the sub-compartment name.
    /// </summary>
    public string? SubCompartmentName { get; set; }

    /// <summary>
    /// Gets or sets the total area of the compartment in hectares.
    /// </summary>
    public double? TotalHectares { get; set; }

    /// <summary>
    /// Gets or sets the designation of the compartment.
    /// </summary>
    public string? Designation { get; set; }

    /// <summary>
    /// Gets or sets the name of the nearest town to the compartment.
    /// </summary>
    public string? NearestTown { get; set; }

    /// <summary>
    /// Gets and sets the GIS data associated with the compartment, which may include spatial information or maps.
    /// </summary>
    public string? GISData { get; set; }
}

public class CompartmentConfirmedFellingRestockingDetailsModel : CompartmentConfirmedFellingRestockingDetailsModelBase
{
    /// <summary>
    /// A collection of confirmed felling details for the compartment.
    /// </summary>
    public ConfirmedFellingDetailViewModel[] ConfirmedFellingDetails { get; set; } = [];

    /// <summary>
    /// Gets or sets the confirmed total hectares for the submitted compartment.
    /// </summary>
    public double? ConfirmedTotalHectares { get; set; }
}

public class IndividualConfirmedFellingRestockingDetailModel : CompartmentConfirmedFellingRestockingDetailsModelBase
{
    /// <summary>
    /// The individual confirmed felling details for the compartment.
    /// </summary>
    public required ConfirmedFellingDetailViewModel ConfirmedFellingDetails { get; set; }
}

public class NewConfirmedFellingDetailModel : CompartmentConfirmedFellingRestockingDetailsModelBase
{
    /// <summary>
    /// The individual confirmed felling details for the compartment.
    /// </summary>
    public required NewConfirmedFellingDetailViewModel ConfirmedFellingDetails { get; set; }
}

public class IndividualConfirmedRestockingDetailModel : CompartmentConfirmedFellingRestockingDetailsModelBase
{
    public ConfirmedRestockingDetailViewModel ConfirmedRestockingDetails { get; set; }
}