namespace Forestry.Flo.Services.FellingLicenceApplications.Models.Reports;

public class FellingLicenceApplicationReportEntry
{
    public string FellingLicenceReference { get; set; }
    public string CurrentStatus { get; set; }
    public DateTime? DateOfSubmission { get; set; }
    public DateTime? DateOfApproval { get; set; }
    public string? Source { get; set; }
    public DateTime? FinalActionDate { get; set; }
    public bool? FinalActionDateExtended { get; set; }
    public DateTime? CitizensCharterDate { get; set; }
    public DateTime? ActualFellingStartDate { get; set; }
    public DateTime? ActualFellingEndDate { get; set; }
    public DateTime? ProposedFellingStart { get; set; }
    public DateTime? ProposedFellingEnd { get; set; }
    public DateTime? PublicRegisterOn { get; set; }
    public DateTime? PublicRegisterRemoved { get; set; }
    public DateTime? PublicRegisterExpires { get; set; }
    public bool? PublicRegisterExempt { get; set; }
    public string AssignedAO { get; set; }
    public string AssignedWO { get; set; }
    public string CurrentAssignee { get; set; }
    public double DaysAtCurrentStatus {get; set; }
    public int? DaysAtDraft { get; set; }
    public int? DaysAtSubmitted { get; set; }
    public int? DaysAtReceived { get; set; }
    public int? DaysAtWithApplicant { get; set; }
    public int? DaysAtReturnedToApplicant { get; set; }
    public int? DaysAtAdminOfficerReview { get; set; }
    public int? DaysAtWoodlandOfficerReview { get; set; }
    public int? DaysAtSentForApproval { get; set; }
    public int? DaysAtApproved { get; set; }
    public int? DaysAtRefused { get; set; }
    public int? DaysAtWithdrawn { get; set; }
    public int? DaysAtReferredToLocalAuthority { get; set; }
}
