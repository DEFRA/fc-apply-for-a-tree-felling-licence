using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Forestry.Flo.Services.PropertyProfiles.Entities;

public class Compartment
{
    /// <summary>
    /// Gets and Sets the compartment id.
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Gets and Sets the compartment number.
    /// </summary>
    [Required]
    public string CompartmentNumber { get; protected set; }

    /// <summary>
    /// Gets and Sets the sub compartment name
    /// </summary>
    public string? SubCompartmentName { get; protected set; }

    /// <summary>
    /// Gets and Sets the total hectares number
    /// </summary>
    public double? TotalHectares { get; protected set; }

    /// <summary>
    /// Gets and Sets the designation
    /// </summary>
    public string? Designation { get; protected set; }

    /// <summary>
    /// Gets and Sets the GIS data
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public string? GISData { get; set; }

    /// <summary>
    /// Gets and Sets the id of the Property Profile
    /// </summary>
    public Guid PropertyProfileId { get; set; }

    public virtual PropertyProfile PropertyProfile { get; set; }

    protected Compartment()
    {
    }

    public Compartment(string compartmentNumber,
        string? subCompartmentName,
        double? totalHectares,
        string? designation,
        string? gisData,
        Guid propertyProfileId)
    {
        CompartmentNumber = compartmentNumber;
        SubCompartmentName = subCompartmentName;
        TotalHectares = totalHectares;
        Designation = designation;
        GISData = gisData;
        PropertyProfileId = propertyProfileId;
    }
}