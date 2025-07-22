using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.External.Web.Models.Compartment;

public class CompartmentModel: PageWithBreadcrumbsViewModel, IValidatableObject
{
    private const string CompartmentNumberValidationMessage =
        "Enter a compartment name or number up to 35 characters long, which must only include letters a to z, numbers, and special characters such as hyphens, spaces and apostrophes";

    private const string SubCompartmentNameValidationMessage =
        "Enter a sub-compartment name or number up to 10 characters long, which must only include letters a to z, numbers, and special characters such as hyphens, spaces and apostrophes";

    /// <summary>
    /// Gets and Sets the compartment id.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets and Sets the compartment number.
    /// </summary>
    [Required(ErrorMessage = CompartmentNumberValidationMessage)]
    [RegularExpression("^[-a-zA-Z0-9'\\s]*$", ErrorMessage = CompartmentNumberValidationMessage)]
    [MaxLength(DataValueConstants.PropertyNameMaxLength, ErrorMessage = CompartmentNumberValidationMessage)]
    public string? CompartmentNumber { get; set; }

    /// <summary>
    /// Gets and Sets the sub compartment name
    /// </summary>
    [RegularExpression("^[-a-zA-Z0-9'\\s]*$", ErrorMessage = SubCompartmentNameValidationMessage)]
    [MaxLength(DataValueConstants.SubPropertyNameMaxLength, ErrorMessage = SubCompartmentNameValidationMessage)]
    public string? SubCompartmentName { get; set; }

    /// <summary>
    /// Gets and Sets the total hectares number
    /// </summary>
    public double? TotalHectares { get; set; }

    /// <summary>
    /// Gets and Sets the designation
    /// </summary>
    [MaxLength(DataValueConstants.PropertyNameMaxLength)]
    public string? Designation { get; set; }

    /// <summary>
    /// Gets and Sets the GIS data
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public string? GISData { get; set; }

    /// <summary>
    /// Gets and sets the id of the property profile which includes the compartment
    /// </summary>
    public Guid PropertyProfileId { get; set; }

    public string PropertyProfileName { get; set; } = null!;

    /// <summary>
    /// Get the display name for the current compartment, which is the <see cref="CompartmentNumber"/>.
    /// </summary>
    public string? DisplayName => CompartmentNumber;

    public Guid? ApplicationId { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (double.TryParse(CompartmentNumber, out double cptNumber))
        {
            if (cptNumber < 0 || cptNumber > 9999)
            {
                yield return new ValidationResult(
                    "Enter a compartment number between 0 and 9999",
                    new[] { nameof(CompartmentNumber) });
            }
        }
    }

    public Guid WoodlandOwnerId { get; set; }
    public bool IsForRestockingCompartmentSelection { get; set; }
    public Guid? FellingCompartmentId { get; set; }
    public string? FellingCompartmentName { get; set; }
    public Guid? ProposedFellingDetailsId { get; set; }
    public FellingOperationType? FellingOperationType { get; set; }
    public Guid? AgencyId { get; set; }
}