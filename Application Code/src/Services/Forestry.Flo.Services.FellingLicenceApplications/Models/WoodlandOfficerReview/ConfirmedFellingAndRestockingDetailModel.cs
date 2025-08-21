using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview;

/// <summary>
/// Base model class for confirmed felling and restocking details of a compartment.
/// </summary>
public class FellingAndRestockingDetailModelBase
{
    /// <summary>
    /// Gets or sets the unique identifier for the compartment.
    /// </summary>
    public Guid CompartmentId { get; set; }

    /// <summary>
    /// Gets and sets the unique identifier for the submitted FLA property compartment.
    /// </summary>
    public Guid SubmittedFlaPropertyCompartmentId { get; set; }

    /// <summary>
    /// Gets or sets the total hectares for the compartment.
    /// </summary>
    public double? TotalHectares { get; set; }

    /// <summary>
    /// Gets or sets the designation of the compartment.
    /// </summary>
    public string? Designation { get; set; }

    /// <summary>
    /// Gets or sets the compartment number.
    /// </summary>
    public string? CompartmentNumber { get; set; }

    /// <summary>
    /// Gets or sets the sub-compartment name.
    /// </summary>
    public string? SubCompartmentName { get; set; }

    /// <summary>
    /// Gets or sets the name of the nearest town to the compartment.
    /// </summary>
    public string? NearestTown { get; set; }
}

/// <summary>
/// Model class for confirmed felling and restocking details of a compartment,
/// including a collection of confirmed felling details and the total confirmed hectares.
/// </summary>
public class FellingAndRestockingDetailModel : FellingAndRestockingDetailModelBase
{
    /// <summary>
    /// Gets or sets the collection of confirmed felling detail models for the compartment.
    /// </summary>
    public IEnumerable<ConfirmedFellingDetailModel> ConfirmedFellingDetailModels { get; set; } = [];

    /// <summary>
    /// Gets or sets a collection of proposed felling detail models for the compartment.
    /// </summary>
    /// <remarks>
    /// The presence of a proposed felling operation without a corresponding confirmed felling detail
    /// indicates that the confirmed felling detail has been deleted.
    /// </remarks>
    public IEnumerable<ProposedFellingDetailModel> ProposedFellingDetailModels { get; set; } = [];

    /// <summary>
    /// Gets or sets the total confirmed hectares for the compartment.
    /// </summary>
    public double? ConfirmedTotalHectares { get; set; }
}

/// <summary>
/// Model class for an individual confirmed felling and restocking detail, 
/// containing a single confirmed felling detail for a compartment.
/// </summary>
public class IndividualFellingRestockingDetailModel : FellingAndRestockingDetailModelBase
{
    /// <summary>
    /// Gets or sets the confirmed felling detail model for the compartment.
    /// </summary>
    public required ConfirmedFellingDetailModel ConfirmedFellingDetailModel { get; set; }
}

/// <summary>
/// Model class for an individual confirmed felling and restocking detail, 
/// containing a single confirmed felling detail for a compartment.
/// </summary>
public class IndividualRestockingDetailModel : FellingAndRestockingDetailModelBase
{
    /// <summary>
    /// Gets or sets the confirmed felling detail model for the compartment.
    /// </summary>
    public required ConfirmedRestockingDetailModel ConfirmedRestockingDetailModel { get; set; }
}