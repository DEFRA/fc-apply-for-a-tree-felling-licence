using System.ComponentModel.DataAnnotations;
using Forestry.Flo.Services.Common.Infrastructure;

namespace Forestry.Flo.Services.Common.Models;

public enum ActivityFeedItemType
{
    // Case Notes

    [Display(Name = "Case note"), ActivityFeedItemType(ActivityFeedItemCategory.CaseNote)]
    CaseNote,

    [Display(Name = "Operations admin officer review comment"), ActivityFeedItemType(ActivityFeedItemCategory.CaseNote)]
    AdminOfficerReviewComment,

    [Display(Name = "Woodland officer review comment"), ActivityFeedItemType(ActivityFeedItemCategory.CaseNote)]
    WoodlandOfficerReviewComment,

    [Display(Name = "Site visit comment"), ActivityFeedItemType(ActivityFeedItemCategory.CaseNote)]
    SiteVisitComment,

    [Display(Name = "Return to applicant comment"), ActivityFeedItemType(ActivityFeedItemCategory.CaseNote)]
    ReturnToApplicantComment,

    [Display(Name = "Larch check comment"), ActivityFeedItemType(ActivityFeedItemCategory.CaseNote)]
    LarchCheckComment,

    [Display(Name = "Cricket bat willow check comment"), ActivityFeedItemType(ActivityFeedItemCategory.CaseNote)]
    CBWCheckComment,

    [Display(Name = "Approver review comment"), ActivityFeedItemType(ActivityFeedItemCategory.CaseNote)]
    ApproverReviewComment,

    // Notifications

    [Display(Name = "Assigned user changed"), ActivityFeedItemType(ActivityFeedItemCategory.Notification)]
    AssigneeHistoryNotification,

    [Display(Name = "Application status changed"), ActivityFeedItemType(ActivityFeedItemCategory.Notification)]    
    StatusHistoryNotification,

    [Display(Name = "Public register comment"), ActivityFeedItemType(ActivityFeedItemCategory.Notification)]
    PublicRegisterComment,

    [Display(Name = "Consultation comment"), ActivityFeedItemType(ActivityFeedItemCategory.Notification)]
    ConsulteeComment,

    // Outgoing notifications

    [Display(Name = "Application submission confirmation"), ActivityFeedItemType(ActivityFeedItemCategory.OutgoingNotification)]
    ApplicationSubmissionConfirmation,
    
    [Display(Name = "Application withdrawn confirmation"), ActivityFeedItemType(ActivityFeedItemCategory.OutgoingNotification)]
    ApplicationWithdrawnConfirmation,

    [Display(Name = "User assigned to application"), ActivityFeedItemType(ActivityFeedItemCategory.OutgoingNotification)]
    UserAssignedToApplication,

    [Display(Name = "Inform WO of operations admin officer review completion"), ActivityFeedItemType(ActivityFeedItemCategory.OutgoingNotification)]
    InformWoodlandOfficerOfAdminOfficerReviewCompletion,

    [Display(Name = "Inform Approver of woodland officer review completion"), ActivityFeedItemType(ActivityFeedItemCategory.OutgoingNotification)]
    InformFieldManagerOfWoodlandOfficerReviewCompletion,

    [Display(Name = "Application resubmitted notification"), ActivityFeedItemType(ActivityFeedItemCategory.OutgoingNotification)]
    ApplicationResubmitted,

    [Display(Name = "Inform applicant of returned application"), ActivityFeedItemType(ActivityFeedItemCategory.OutgoingNotification)]
    InformApplicantOfReturnedApplication,

    [Display(Name = "Inform FC staff of returned application"), ActivityFeedItemType(ActivityFeedItemCategory.OutgoingNotification)]
    InformFCStaffOfReturnedApplication,

    [Display(Name = "External consultee invite"), ActivityFeedItemType(ActivityFeedItemCategory.OutgoingNotification)]
    ExternalConsulteeInvite,

    [Display(Name = "External consultee invite"), ActivityFeedItemType(ActivityFeedItemCategory.OutgoingNotification)]
    ExternalConsulteeInviteWithPublicRegisterInfo,

    [Display(Name = "Application conditions sent to applicant"), ActivityFeedItemType(ActivityFeedItemCategory.OutgoingNotification)]
    ConditionsToApplicant,

    [Display(Name = "Application approved"), ActivityFeedItemType(ActivityFeedItemCategory.OutgoingNotification)]
    InformApplicantOfApplicationApproval,

    [Display(Name = "Application refused"), ActivityFeedItemType(ActivityFeedItemCategory.OutgoingNotification)]
    InformApplicantOfApplicationRefusal,

    [Display(Name = "Application referred to local authority"), ActivityFeedItemType(ActivityFeedItemCategory.OutgoingNotification)]
    InformApplicantOfApplicationReferredToLocalAuthority,
    
    [Display(Name = "Application extended"), ActivityFeedItemType(ActivityFeedItemCategory.OutgoingNotification)]
    InformApplicantOfApplicationExtension,

    [Display(Name = "Applicant responded to amendments"), ActivityFeedItemType(ActivityFeedItemCategory.AmendmentReviews)]
    AmendmentApplicantReason,

    [Display(Name = "Amendments sent to applicant"), ActivityFeedItemType(ActivityFeedItemCategory.AmendmentReviews)]
    AmendmentOfficerReason,
}