using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.Internal.Web.Models.WoodlandOfficerReview;

/// <summary>
/// View model for selecting a felling compartment during the woodland officer review process.
/// </summary>
public class SelectFellingCompartmentModel : WoodlandOfficerReviewModelBase
{
    /// <summary>
    /// Gets or sets the unique identifier of the application.
    /// </summary>
    public Guid ApplicationId { get; set; }

    /// <summary>
    /// Gets or sets the list of compartments available for selection.
    /// </summary>
    public List<SelectableCompartment> SelectableCompartments { get; set; } = [];

    /// <summary>
    /// Gets or sets the identifier of the selected compartment.
    /// </summary>
    [Required(ErrorMessage = "Select a compartment")]
    public Guid? SelectedCompartmentId { get; set; }

    /// <summary>
    /// Gets or sets the GIS data associated with the selection.
    /// </summary>
    public string? GisData { get; set; }
}

/// <summary>
/// Represents a selectable compartment with an identifier and display name.
/// </summary>
public record SelectableCompartment(Guid Id, string DisplayName);