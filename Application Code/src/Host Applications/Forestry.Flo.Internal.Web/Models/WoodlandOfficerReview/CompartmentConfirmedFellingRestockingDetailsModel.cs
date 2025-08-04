using Microsoft.AspNetCore.Mvc;

namespace Forestry.Flo.Internal.Web.Models.WoodlandOfficerReview;

public class CompartmentConfirmedFellingRestockingDetailsModelBase
{
    [HiddenInput]
    public Guid? CompartmentId { get; set; }

    public string? CompartmentNumber { get; set; }

    public string? CompartmentName => CompartmentNumber;

    public string? SubCompartmentName { get; set; }

    public double? TotalHectares { get; set; }

    public string? Designation { get; set; }
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