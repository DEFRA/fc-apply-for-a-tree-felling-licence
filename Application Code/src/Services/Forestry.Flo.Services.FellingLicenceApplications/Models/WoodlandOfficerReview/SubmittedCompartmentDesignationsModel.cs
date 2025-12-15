using Forestry.Flo.Services.FellingLicenceApplications.Entities;

namespace Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview;

/// <summary>
/// Model class representing the designations for a submitted compartment on an FLA.
/// </summary>
public class SubmittedCompartmentDesignationsModel
{
    /// <summary>
    /// Gets and sets the ID of the underlying entity for this designations model.
    /// </summary>
    public Guid? Id { get; set; }

    /// <summary>
    /// Gets and sets the property profile compartment id that these designations belong to.
    /// </summary>
    public Guid CompartmentId { get; set; }

    /// <summary>
    /// Gets and sets the ID of the submitted compartment that these designations belong to.
    /// </summary>
    public Guid SubmittedFlaCompartmentId { get; set; }

    /// <summary>
    /// Gets and sets the name of the compartment that these designations belong to.
    /// </summary>
    public string CompartmentName { get; set; }

    /// <summary>
    /// Gets and sets a value indicating whether the compartment is within an SSSI.
    /// </summary>
    public bool Sssi { get; set; }

    /// <summary>
    /// Gets and sets a value indicating whether the compartment is within an SAC.
    /// </summary>
    public bool Sacs { get; set; }

    /// <summary>
    /// Gets and sets a value indicating whether the compartment is within an SPA.
    /// </summary>
    public bool Spa { get; set; }

    /// <summary>
    /// Gets and sets a value indicating whether the compartment is within a RAMSAR.
    /// </summary>
    public bool Ramsar { get; set; }

    /// <summary>
    /// Gets and sets a value indicating whether the compartment is within an SBI.
    /// </summary>
    public bool Sbi { get; set; }

    /// <summary>
    /// Gets and sets a value indicating whether the compartment has other designations.
    /// </summary>
    public bool Other { get; set; }

    /// <summary>
    /// Gets and sets details about other designations if applicable.
    /// </summary>
    public string? OtherDesignationDetails { get; set; }

    /// <summary>
    /// Gets and sets a value indicating whether the compartment has no designations.
    /// </summary>
    public bool None { get; set; }

    /// <summary>
    /// Gets and sets a value indicating that the compartment crosses a PAWS zone.
    /// </summary>
    public bool Paws { get; set; }

    /// <summary>
    /// Gets and sets the <see cref="NativeTreeSpeciesProportion"/> for the compartment before the
    /// felling operation is carried out.
    /// </summary>
    public NativeTreeSpeciesProportion? ProportionBeforeFelling { get; set; }

    /// <summary>
    /// Gets and sets the <see cref="NativeTreeSpeciesProportion"/> for the compartment after the
    /// felling operation is carried out.
    /// </summary>
    public NativeTreeSpeciesProportion? ProportionAfterFelling { get; set; }

    /// <summary>
    /// Gets and sets a value indicating whether the compartment designations have been reviewed.
    /// </summary>
    public bool HasBeenReviewed { get; set; }

    public string GetDesignationSummary()
    {
        if (!HasBeenReviewed)
        {
            return Paws ? "PAWS (unreviewed)" : "None (unreviewed)";
        }

        var designations = new List<string>();
        if (Sssi) designations.Add("SSSI");
        if (Sacs) designations.Add("SAC");
        if (Spa) designations.Add("SPA");
        if (Ramsar) designations.Add("RAMSAR");
        if (Sbi) designations.Add("SBI");
        if (Paws) designations.Add("PAWS");
        if (Other) designations.Add("Other");
        if (None) designations.Add("None (reviewed)");

        return designations.Count == 0 ? "None" : string.Join(", ", designations);
    }
}