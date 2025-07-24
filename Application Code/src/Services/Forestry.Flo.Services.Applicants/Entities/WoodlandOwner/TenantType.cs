namespace Forestry.Flo.Services.Applicants.Entities.WoodlandOwner;

/// <summary>
/// An enumeration of possible types of tenants.
/// </summary>
public enum TenantType
{
    /// <summary>
    /// Represents a non-tenant woodland owner.
    /// </summary>
    None,
    /// <summary>
    /// Represents a tenant on Crown Land.
    /// </summary>
    CrownLand,
    /// <summary>
    /// Represents a tenant on non-Crown Land.
    /// </summary>
    NonCrownLand
}