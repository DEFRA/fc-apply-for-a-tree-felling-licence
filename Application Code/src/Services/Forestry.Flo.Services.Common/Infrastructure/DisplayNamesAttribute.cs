namespace Forestry.Flo.Services.Common.Infrastructure;

[AttributeUsage(AttributeTargets.Field)]
public class DisplayNamesAttribute(
    string internalName,
    string externalName) : Attribute
{
    /// <summary>
    /// The internal description of the enum value.
    /// </summary>
    public string InternalName { get; } = internalName;

    /// <summary>
    /// The external description of the enum value.
    /// </summary>
    public string ExternalName { get; } = externalName;
}