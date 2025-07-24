using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.Internal.Web.Models.Compartment;

public class CompartmentModel:PageWithBreadcrumbsViewModel
{
    /// <summary>
    /// Gets and Sets the compartment id.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets and Sets the compartment number.
    /// </summary>
    [Required(ErrorMessage = "Compartment name or number must be provided")]
    [RegularExpression("^[-a-zA-Z0-9'\\s]*$", ErrorMessage = "Compartment name or number must only include letters a to z, numbers, and special characters such as hyphens, spaces and apostrophes")]
    [MaxLength(DataValueConstants.PropertyNameMaxLength, ErrorMessage = "Compartment name or number must be 50 characters or less")]
    public string CompartmentNumber { get; set; } = null!;

    /// <summary>
    /// Gets and Sets the sub compartment name
    /// </summary>
    [RegularExpression("^[-a-zA-Z0-9'\\s]*$", ErrorMessage = "Sub-compartment name or number must only include letters a to z, numbers, and special characters such as hyphens, spaces and apostrophes")]
    [MaxLength(DataValueConstants.PropertyNameMaxLength, ErrorMessage = "Sub-compartment name or number must be 50 characters or less")]
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
    public string DisplayName => CompartmentNumber;

    public Guid? ApplicationId { get; set; }
}