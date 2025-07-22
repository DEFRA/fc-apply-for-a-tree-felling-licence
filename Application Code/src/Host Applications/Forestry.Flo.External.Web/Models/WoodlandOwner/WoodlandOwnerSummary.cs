namespace Forestry.Flo.External.Web.Models.WoodlandOwner;

public record WoodlandOwnerSummary(
    Guid Id,
    Guid AgencyId,
    string? ContactName,
    string ContactEmail,
    string? OrgName)
{
    public string DisplayName => string.IsNullOrWhiteSpace(OrgName)
        ? ContactName ?? "Unknown"
        : OrgName;

    public string OrgDisplayName => string.IsNullOrWhiteSpace(OrgName) 
        ? "N/A" 
        : OrgName;
}
