namespace Forestry.Flo.Services.Gis.Models.Esri.Configuration;

/// <summary>
/// Enumeration of valid target environment values for the LIS system to determine
/// where it should send the generated constraints report back to.
/// </summary>
public enum LisResponseTargetEnvironment
{
    InternalStaging,
    InternalPreprod,
    InternalProd,
    ExternalStaging,
    ExternalPreprod,
    ExternalProd
}