namespace Forestry.Flo.Internal.Web.Models.WoodlandOfficerReview;

/// <summary>
/// Model class representing errors related to area totals across felling operations in a particular compartment, or restocking
/// operations for a specific felling operation. To be presented as warnings, as opposed to full-on validation failures that
/// prevent completion of the confirmed felling and restocking process.
/// </summary>
public class ConfirmedFellingAndRestockingWarning
{
    /// <summary>
    /// Gets and sets the id of the compartment that the warning relates to.
    /// </summary>
    public Guid CompartmentId { get; set; }

    /// <summary>
    /// Gets and sets the id of the confirmed felling details that the warning relates to,
    /// if relevant.
    /// </summary>
    public Guid? ConfirmedFellingDetailsId { get; set; }

    /// <summary>
    /// Gets and sets the warning type of this warning instance.
    /// </summary>
    public ConfirmedFellingAndRestockingWarningType WarningType { get; set; }
}

/// <summary>
/// Enumeration of possible types of <see cref="ConfirmedFellingAndRestockingWarning"/>.
/// </summary>
public enum ConfirmedFellingAndRestockingWarningType
{
    /// <summary>
    /// Warning type for when the total of felling areas across all felling operations in
    /// a compartment exceeds the total area of the compartment.
    /// </summary>
    FellingAreasExceedCompartmentArea,

    /// <summary>
    /// Warning type for when the total of restocking areas across all restocking operations linked to a
    /// felling operation does not match the felling area.
    /// </summary>
    RestockingAreasDoNotMatchFellingArea
}