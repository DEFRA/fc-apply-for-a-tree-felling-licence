using Microsoft.AspNetCore.Routing.Constraints;

namespace Forestry.Flo.Services.PropertyProfiles.DataImports;

/// <summary>
/// Model class representing properties within FLOv2 that are used in data imports.
/// </summary>
public class PropertyIds
{
    /// <summary>
    /// Gets and sets the name of a property within FLOv2.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets and sets the unique identifier for the property within FLOv2.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets and sets a collection of compartment identifiers associated with this property.
    /// </summary>
    public IEnumerable<CompartmentIds> CompartmentIds { get; set; }
}

/// <summary>
/// Model class representing compartments within FLOv2 that are used in data imports.
/// </summary>
public class CompartmentIds
{
    /// <summary>
    /// Gets and sets the name of the compartment within FLOv2.
    /// </summary>
    public string CompartmentName { get; set; }

    /// <summary>
    /// Gets and sets the unique identifier for the compartment within FLOv2.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets and sets the area of the compartment in hectares.
    /// </summary>
    public double? Area { get; set; }
}