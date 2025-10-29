using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.Services.FellingLicenceApplications.Entities;

/// <summary>
/// FellingLicenceApplication entity class
/// </summary>
public class FellingLicenceApplication
{
    /// <summary>
    /// Gets and Sets the felling licence application ID.
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the application reference.
    /// </summary>
    [Required]
    public string ApplicationReference { get; set; }

    /// <summary>
    /// Gets or sets the created date / time.
    /// </summary>
    [Required]
    public DateTime CreatedTimestamp { get; set; }


    /// <summary>
    /// Gets or sets the users id creating the felling licence application
    /// </summary>
    [Required]
    public Guid CreatedById { get; set; }

    /// <summary>
    /// Gets or sets the proposed felling start date / time.
    /// </summary>
    public DateTime? ProposedFellingStart { get; set; }

    /// <summary>
    /// Gets or sets the proposed felling end date / time.
    /// </summary>
    public DateTime? ProposedFellingEnd { get; set; }

    /// <summary>
    /// Gets or sets the actual felling start date / time.
    /// </summary>
    public DateTime? ActualFellingStart { get; set; }

    /// <summary>
    /// Gets or sets the actual felling end date / time.
    /// </summary>
    public DateTime? ActualFellingEnd { get; set; }

    public string? ProposedTiming { get; set; }

    public string? Measures { get; set; }

    /// <summary>
    /// Gets or sets the source.
    /// </summary>
    public FellingLicenceApplicationSource Source { get; set; } = FellingLicenceApplicationSource.ApplicantUser;

    /// <summary>
    /// Gets or sets the approver
    /// </summary>
    public Guid? ApproverId { get; set; }

    /// <summary>
    /// Gets or sets the woodland owner identifier.
    /// </summary>
    [Required]
    public Guid WoodlandOwnerId { get; set; }

    /// <summary>
    /// Gets or sets the documents.
    /// </summary>
    public IList<Document>? Documents { get; set; } = new List<Document>();

    /// <summary>
    /// Gets or sets the status histories.
    /// </summary>
    [Required]
    public IList<StatusHistory> StatusHistories { get; set; } = new List<StatusHistory>();

    /// <summary>
    /// Gets or sets the assignee histories.
    /// </summary>
    public IList<AssigneeHistory> AssigneeHistories { get; set; } = new List<AssigneeHistory>();

    /// <summary>
    /// Gets or sets the Case Notes.
    /// </summary>
    public IList<CaseNote> CaseNotes { get; set; } = new List<CaseNote>();
    
    /// <summary>
    /// Gets or sets the External Access links.
    /// </summary>
    public IList<ExternalAccessLink> ExternalAccessLinks { get; set; } = new List<ExternalAccessLink>();

    /// <summary>
    /// Gets and sets the list of consultee comments for this felling licence application.
    /// </summary>
    public IList<ConsulteeComment> ConsulteeComments { get; set; } = new List<ConsulteeComment>();

    /// <summary>
    /// Gets or sets the linked property profile.
    /// </summary>
    public LinkedPropertyProfile? LinkedPropertyProfile { get; set; }

    public bool TermsAndConditionsAccepted { get; set; }

    /// <summary>
    /// Gets or sets the final action date for this application.
    /// </summary>
    public DateTime? FinalActionDate { get; set; }

    /// <summary>
    /// Gets or sets a flag indicating whether the final action date has been previously extended.
    /// </summary>
    public bool FinalActionDateExtended { get; set; } = false;

    /// <summary>
    /// Gets and sets the citizens charter date for the application.
    /// </summary>
    public DateTime? CitizensCharterDate { get; set; }

    /// <summary>
    /// Gets and sets the date time at which the application was received.
    /// </summary>
    public DateTime? DateReceived { get; set; }

    /// <summary>
    /// Gets and sets the date time at which the external Land Information Search was accessed.
    /// </summary>
    public DateTime? ExternalLisAccessedTimestamp { get; set; }

    /// <summary>
    /// Gets or sets the date and time of the most recent Voluntary Withdrawal Notification.
    /// </summary>
    public DateTime? VoluntaryWithdrawalNotificationTimeStamp { get; set; }

    /// <summary>
    /// Gets or sets a flag indicating whether the user is running the external Lis report
    /// </summary>
    public bool NotRunningExternalLisReport { get; set; }

    public SubmittedFlaPropertyDetail? SubmittedFlaPropertyDetail { get; set; }

    /// <summary>
    /// Gets and sets the Woodland Officer review details for this FLA.
    /// </summary>
    public WoodlandOfficerReview? WoodlandOfficerReview { get; set; }

    /// <summary>
    /// Gets and sets the Woodland Officer review details for this FLA.
    /// </summary>
    public ApproverReview? ApproverReview { get; set; }

    /// <summary>
    /// Gets and sets the Admin Officer review details for this FLA.
    /// </summary>
    public AdminOfficerReview? AdminOfficerReview { get; set; }

    /// <summary>
    /// Gets and sets the public register status details for this FLA.
    /// </summary>
    public PublicRegister? PublicRegister { get; set; }
   
    public FellingLicenceApplicationStepStatus FellingLicenceApplicationStepStatus { get; set; }

    /// <summary>
    /// Gets and Sets the Area Code.
    /// </summary>
    public string? AreaCode { get; set; }

    /// <summary>
    /// Gets and Sets the OS Grid reference.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public string? OSGridReference { get; set; }

    /// <summary>
    /// Gets and Sets the serialised centre point of the property profile.
    /// </summary>
    public string? CentrePoint { get; set; }

    /// <summary>
    /// Gets and Sets the administrative region of the property profile.
    /// </summary>
    public string? AdministrativeRegion { get; set; }

    /// <summary>
    /// Gets or sets the larch check details for this application.
    /// </summary>
    public LarchCheckDetails? LarchCheckDetails { get; set; }

    /// <summary>
    /// Gets or sets the Environmental Impact Assessment details for this application.
    /// </summary>
    public EnvironmentalImpactAssessment? EnvironmentalImpactAssessment { get; set; }

    /// <summary>
    /// Gets and sets a flag indicating whether this application is for a ten year licence.
    /// </summary>
    public bool? IsForTenYearLicence { get; set; }

    /// <summary>
    /// Gets and sets the woodland management plan reference.
    /// </summary>
    public string? WoodlandManagementPlanReference { get; set; }
}