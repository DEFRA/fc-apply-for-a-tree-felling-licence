namespace Forestry.Flo.Services.FellingLicenceApplications.Models.Reports;

public class SubmittedPropertyProfileReportEntry
{
    public string FellingLicenceReference { get; set; }
    public string Name { get; set; }
    public string? NearestTown { get; set; }
    public bool? HasWoodlandManagementPlan { get; set; }
    public string? WoodlandManagementPlanReference { get; set; }
    public bool? IsWoodlandCertificationScheme { get; set; }
    public string? WoodlandCertificationSchemeReference { get; set; }
}