namespace Forestry.Flo.Services.FellingLicenceApplications.Models.Reports;


/// <summary>
/// Class holding the list of Entries which will be populated with matching results from the report query.
/// </summary>
public class FellingLicenceApplicationsReportQueryResultModel
{
    public List<FellingLicenceApplicationReportEntry> FellingLicenceApplicationReportEntries { get; set; } = new();

    public List<ConsultationPublicRegisterExemptionEntry> ConsultationPublicRegisterExemptCases { get; set; } = new();

    public List<SubmittedPropertyProfileReportEntry> SubmittedPropertyProfileReportEntries { get; set; } = new();

    public List<ConfirmedCompartmentDetailReportEntry> ConfirmedCompartmentDetailReportEntries { get; set; } = new();

    public List<ProposedCompartmentDetailReportEntry> ProposedCompartmentDetailReportEntries { get; set; } = new();

    public bool HasData => FellingLicenceApplicationReportEntries.Any();
}
