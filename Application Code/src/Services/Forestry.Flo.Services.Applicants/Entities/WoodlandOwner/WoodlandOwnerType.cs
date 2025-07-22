namespace Forestry.Flo.Services.Applicants.Entities.WoodlandOwner;

/// <summary>
/// An enumeration of possible <see cref="Entities.WoodlandOwner.WoodlandOwner"/> types.
/// </summary>
public enum WoodlandOwnerType
{
    /// <summary>
    /// Represents an owner of a woodland.
    /// </summary>
    WoodlandOwner,
    /// <summary>
    /// Represents a tenant on crown land.
    /// </summary>
    Tenant,
    /// <summary>
    /// Represents a trust.
    /// </summary>
    Trust
}