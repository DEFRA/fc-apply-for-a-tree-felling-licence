namespace Forestry.Flo.Services.Common.Auditing;

public static class AuditEvents
{
    public const string AddAdminOfficerToAdminHub = "Add Operations Admin Officer to Admin Hub";
    public const string AddAdminOfficerToAdminHubFailure = "Add Operations Admin Officer to Admin Hub Failure";
    public const string RemoveAdminOfficerFromAdminHub = "Remove Operations Admin Officer From Admin Hub";
    public const string RemoveAdminOfficerFromAdminHubFailure = "Remove Operations Admin Officer From Admin Hub Failure";
    public const string UpdateAdminHubDetails = "Update Admin Hub Details";
    public const string UpdateAdminHubDetailsFailure = "Update Admin Hub Details Failure";
    
    public const string RegisterAuditEvent = "Register New Account";
    public const string RegisterAccountDueToNewInternalFcUserAccountApprovalAuditEvent = "Register New Account Due To New Internal Fc User Account Approval";
    public const string RegisterAccountDueToNewInternalFcUserAccountApprovalAuditEventFailure = "Register New Account Due To New Internal Fc User Account Approval Failure";

    public const string UpdateAccountEvent = "Update Existing Account";
    public const string RegisterFailureEvent = "Register New Account Failure";
    public const string UpdateAccountFailureEvent = "Update Existing Account Failure";

    public const string UpdateWoodlandOwnerEvent = "Update Woodland Owner Details";
    public const string UpdateWoodlandOwnerFailureEvent = "Update Woodland Owner Details Failure";

    public const string CreatePropertyProfileEvent = "Create Property Profile";
    public const string CreatePropertyProfileFailureEvent = "Create Property Profile Failure";
    public const string UpdatePropertyProfileEvent = "Update Property Profile";
    public const string UpdatePropertyProfileFailureEvent = "Update Property Profile Failure";

    public const string CreateCompartmentEvent = "Create Compartment";
    public const string CreateCompartmentFailureEvent = "Create Compartment Failure";
    public const string UpdateCompartmentEvent = "Update Compartment";
    public const string UpdateCompartmentFailureEvent = "Update Compartment Failure";

    public const string WoodlandOwnerUserInvitationSent = "Woodland Owner User Invitation Sent";
    public const string WoodlandOwnerUserInvitationFailure = "Woodland Owner User Invitation Failure";
    public const string AgencyUserInvitationSent = "Agency User Invitation Sent";
    public const string AgencyUserInvitationFailure = "Agency User Invitation Failure";
    public const string AcceptInvitationSuccess = "Accept Invitation Success";
    public const string AcceptInvitationFailure = "Accept Invitation Failure";

    public const string CreateFellingLicenceApplication = "Create Felling Licence Application";
    public const string CreateFellingLicenceApplicationFailure = "Create Felling Licence Application Failure";
    public const string UpdateFellingLicenceApplication = "Update Felling Licence Application";
    public const string UpdateFellingLicenceApplicationFailure = "Update Felling Licence Application Failure";
    public const string SubmitFellingLicenceApplication = "Submit Felling Licence Application";
    public const string SubmitFellingLicenceApplicationFailure = "Submit Felling Licence Application Failure";
    public const string ResubmitFellingLicenceApplication = "Resubmit Felling Licence Application";
    public const string ResubmitFellingLicenceApplicationFailure = "Resubmit Felling Licence Application Failure";
    public const string WithdrawFellingLicenceApplication = "Withdraw Felling Licence Application";
    public const string WithdrawFellingLicenceApplicationFailure = "Withdraw Felling Licence Application Failure";
    public const string DeleteDraftFellingLicenceApplication = "Delete  draft Felling Licence Application";
    public const string DeleteDraftFellingLicenceApplicationFailure = "Delete draft Felling Licence Application Failure";

    public const string AddFellingLicenceAttachmentEvent = "Add Felling Licence Attachment";
    public const string AddFellingLicenceAttachmentFailureEvent = "Add Felling Licence Attachment Failure";
    public const string RemoveFellingLicenceAttachmentEvent = "Remove Felling Licence Attachment";
    public const string RemoveFellingLicenceAttachmentFailureEvent = "Remove Felling Licence Attachment Failure";
    public const string GetFellingLicenceAttachmentEvent = "Get Felling Licence Attachment";
    public const string GetFellingLicenceAttachmentFailureEvent = "Get Felling Licence Attachment Failure";


    //Submit FLA audit events
    public const string PropertyProfileSnapshotCreated = "Property Profile Snapshot Created";
    public const string FellingLicenceApplicationAssignedToAdminHub = "Felling Licence Application Assigned To Admin Hub";
    public const string FellingLicenceApplicationFinalActionDateUpdated = "Felling Licence Application Final Action Date Updated";
    public const string FellingLicenceApplicationSubmissionNotificationSent = "Felling Licence Application Submission Notification Sent";
    public const string FellingLicenceApplicationSubmissionComplete = "Felling Licence Application Submission Complete";
    public const string FellingLicenceApplicationSubmissionFailure = "Felling Licence Application Submission Failure";

    public const string AssignFellingLicenceApplicationToStaffMember = "Assign Felling Licence Application To Staff Member";
    public const string AssignFellingLicenceApplicationToStaffMemberFailure = "Assign Felling Licence Application To Staff Member Failure";
    public const string AssignFellingLicenceApplicationToApplicant = "Assign Felling Licence Application To Applicant";
    public const string AssignFellingLicenceApplicationToApplicantFailure = "Assign Felling Licence Application To Applicant Failure";

    public const string CalculateCentrePointForApplication = "Calculate Centre Point For Application";
    public const string CalculateCentrePointForApplicationFailure = "Calculate Centre Point For Application Failure";

    public const string UpdateDateReceived = "Update Date Received";
    public const string UpdateDateReceivedFailure = "Update Date Received Failure";

    //Withdraw FLA audit events
    public const string FellingLicenceApplicationWithdrawNotificationSent = "Felling Licence Application Withdraw Notification Sent";
    public const string FellingLicenceApplicationWithdrawNotificationSentFailed = "Felling Licence Application Withdraw Notification Failed to Send";
    public const string FellingLicenceApplicationWithdrawComplete = "Felling Licence Application Withdraw Complete";
    public const string FellingLicenceApplicationWithdrawFailure = "Felling Licence Application Withdraw Failure";

    public const string RevertApplicationFromWithdrawnSuccess = "Revert Application From Withdrawn Success";
    public const string RevertApplicationFromWithdrawnFailure = "Revert Application From Withdrawn Failure";

    //LIS Constraint Report audit events
    public const string LISConstraintReportConsumedOk = "LIS Constraint Report Consumed Ok";
    public const string LISConstraintReportConsumedFailure = "LIS Constraint Report Consumed Failure";

    //Audit events for outcome of end-user requesting the constraint check feature (outcome of sending geom to forester and the url built for lis deep-link)
    public const string ConstraintCheckerExecutionSuccess = "Constraint Checker Execution Success";
    public const string ConstraintCheckerExecutionFailure = "Constraint Checker Execution Failure";

    //woodland officer review audit events
    public const string UpdateWoodlandOfficerReview = "Update Woodland Officer Review";
    public const string UpdateWoodlandOfficerReviewFailure = "Update Woodland Officer Review Failure";
    public const string AddToConsultationPublicRegisterSuccess = "Add to Consultation Public Register Success";
    public const string AddToConsultationPublicRegisterFailure = "Add to Consultation Public Register Failure";
    public const string UpdatePw14Checks = "Update PW14 Checks";
    public const string UpdatePw14ChecksFailure = "Update PW14 Checks Failure";
    public const string UpdateSiteVisit = "Update Site Visit";
    public const string UpdateSiteVisitFailure = "Update Site Visit Failure";
    public const string ConfirmWoodlandOfficerReview = "Confirm Woodland Officer Review";
    public const string ConfirmWoodlandOfficerReviewFailure = "Confirm Woodland Officer Review Failure";
    public const string ConfirmWoodlandOfficerReviewNotificationSent = "Confirm Woodland Officer Review Notification Sent";
    public const string ConfirmWoodlandOfficerReviewNotificationFailure = "Confirm Woodland Officer Review Notification Failure";
    public const string UpdateConfirmedFellingDetails = "Update Confirmed Felling Details";
    public const string UpdateConfirmedFellingDetailsFailure = "Update Confirmed Felling Details Failure";
    public const string UpdateConfirmedRestockingDetails = "Update Confirmed Restocking Details";
    public const string UpdateConfirmedRestockingDetailsFailure = "Update Confirmed Restocking Details Failure";
    public const string RevertApproveToWoodlandOfficerReview = "Revert Approve to Woodland Officer Review";
    public const string RevertConfirmedFellingDetails = "Revert Confirmed Felling Details";
    public const string RevertConfirmedFellingDetailsFailure = "Revert Confirmed Felling Details Failure";

    //Admin officer review audit events
    public const string ConfirmAdminOfficerReview = "Confirm Operations Admin Officer Review";
    public const string ConfirmAdminOfficerReviewFailure = "Confirm Operations Admin Officer Review Failure";
    public const string ConfirmAdminOfficerReviewNotificationSent = "Confirm Operations Admin Officer Review Notification Sent";
    public const string ConfirmAdminOfficerReviewNotificationFailure = "Confirm Operations Admin Officer Review Notification Failure";
    public const string UpdateAdminOfficerReview = "Update Admin Officer Review";
    public const string UpdateAdminOfficerReviewFailure = "Update Admin Officer Review Failure";
    public const string RevertApproveToAdminOfficerReview = "Revert Approve to Admin Officer Review";

    //ActivityFeedAuditEvents
    public const string RetrieveActivityFeedItems = "Retrieve Activity Feed Items";
    public const string RetrieveActivityFeedItemsFailure = "Retrieve Activity Feed Items Failure";

    //Consultee comments audit events
    public const string ExternalConsulteeInvitationSent = "External Consultee Invitation Sent";
    public const string ExternalConsulteeInvitationFailure = "External Consultee Invitation Failure";
    public const string AddConsulteeComment = "Add Consultee Comment";
    public const string AddConsulteeCommentFailure = "Add Consultee Comment Failure";

    //Felling restocking internal audit events
    public const string ImportFellingRestockingDetails = "Import Proposed Felling/Restocking Details";
    public const string ImportFellingRestockingDetailsFailure = "Import Proposed Felling/Restocking Details Failure";
    public const string SaveChangesFellingRestockingDetails = "Save Changes Felling/Restocking Details";
    public const string SaveChangesFellingRestockingDetailsFailure = "Save Changes Felling/Restocking Details Failure";

    //Conditions Builder
    public const string ConditionsBuiltForApplication = "Conditions Built For Application";
    public const string ConditionsBuiltForApplicationFailure = "Conditions Built For Application Failure";
    public const string ConditionsSavedForApplication = "Conditions Saved For Application";
    public const string ConditionsSavedForApplicationFailure = "Conditions Saved For Application Failure";

    //Agent authority request
    public const string CreateAgentAuthorityForm = "Agent Authority Requested";
    public const string CreateAgentAuthorityFormFailure = "Agent Authority Requested Failure";
    public const string SetAgentAuthorityStatus = "Set Agent Authority Status";
    public const string SetAgentAuthorityStatusFailure = "Set Agent Authority Status Failure";

    public const string RemoveAgentAuthorityForm = "Remove Agent Authority Form";
    public const string RemoveAgentAuthorityFormFailure = "Remove Agent Authority Form Failure";

    public const string DownloadAgentAuthorityFormFiles = "Download Agent Authority Form Files";
    public const string DownloadAgentAuthorityFormFilesFailure = "Download Agent Authority Form Files Failure";

    public const string AddAgentAuthorityFormFiles = "Add Agent Authority Form Files";
    public const string AddAgentAuthorityFormFilesValidationFailure = "Add Agent Authority Form Files Validation Failure";
    public const string AddAgentAuthorityFormFilesFailure = "Add Agent Authority Form Files Failure";


    //FC User creating a new Woodland Owner
    public const string FcAgentUserCreateWoodlandOwnerEvent = "FC Agent Created Woodland Owner Details";
    public const string FcAgentUserCreateWoodlandOwnerFailureEvent = "FC Agent Created Woodland Owner Details Failure";

    //FC User creating a new Agency Owner
    public const string FcUserCreateAgencyEvent = "FC User Created Agency";
    public const string FcUserCreateAgencyFailureEvent = "FC User Created Agency Failure";

    //CSV Data Import
    public const string ImportDataFromCsv = "Import Data From CSV";
    public const string ImportDataFromCsvFailure = "Import Data From CSV Failure";

    //Approve, refuse and refer applications
    public const string SaveApproverReview = "Save Approver Review";
    public const string SaveApproverReviewFailure = "Save Approver Review Failure";
    public const string ApplicationApproved = "Application Approved";
    public const string ApplicationRefused = "Application Refused";
    public const string ApplicationReferredToLocalAuthority = "Application Referred To Local Authority";

    public const string ApplicationExtensionNotification = "Application Extension Notification";
    public const string ApplicationVoluntaryWithdrawalNotification = "Application Volutary Withdrawal Notification";

    //Account administrator
    public const string AccountAdministratorUpdateExistingAccount = "Account Administrator Update Existing Account";
    public const string AccountAdministratorUpdateExistingAccountFailure = "Account Administrator Update Existing Account Failure";
    public const string AccountAdministratorCloseAccount = "Account Administrator Close Account";
    public const string AccountAdministratorCloseAccountFailure = "Account Administrator Close Account Failure";
    public const string AdministratorUpdateExternalAccount = "Administrator Update External Account";
    public const string AdministratorUpdateExternalAccountFailure = "Administrator Update External Account Failure";
    public const string AdministratorCloseExternalAccount = "Administrator Close External Account";
    public const string AdministratorCloseExternalAccountFailure = "Administrator Close External Account Failure";

    //Pdf Generator
    public const string GeneratingPdfFellingLicence = "Generating the PDF for the Application";
    public const string GeneratingPdfFellingLicenceFailure = "Generating the PDF for the Application Failure";

    public const string ReportExecution = "Report Execution";
    public const string ReportExecutionNoDataFound = "Report Execution No Data Found";
    public const string ReportExecutionFailure = "Report Execution Failure";

    // legacy documents
    public const string AccessLegacyDocumentEvent = "Access Legacy Document";
    public const string AccessLegacyDocumentFailureEvent = "Access Legacy Document Failure";


    public const string ConsultationPublicRegisterAutomatedExpirationRemovalSuccess =
        "Consultation Public Register Automated Expiration Removal Success";

    public const string ConsultationPublicRegisterAutomatedExpirationRemovalFailure =
        "Consultation Public Register Automated Expiration Removal Failure";

    public const string ConsultationPublicRegisterApplicationRemovalNotification = 
        "Consultation Public Register Application Removal Notification";


    public const string DecisionPublicRegisterAutomatedExpirationRemovalSuccess =
        "Decision Public Register Automated Expiration Removal Success";

    public const string DecisionPublicRegisterAutomatedExpirationRemovalFailure =
        "Decision Public Register Automated Expiration Removal Failure";

    public const string DecisionPublicRegisterApplicationRemovalNotification = "Decision Public Register Application Removal Notification";

    // EIA
    public const string ApplicantUploadEiaDocumentsSuccess = "Applicant Upload EIA Documents Success";
    public const string ApplicantUploadEiaDocumentsFailure = "Applicant Upload EIA Documents Failure";
    public const string ApplicantCompleteEiaSection = "Applicant Complete EIA Section";
    public const string ApplicantCompleteEiaSectionFailure = "Applicant Complete EIA Section Failure";
    public const string AdminOfficerCompleteEiaCheck = "Admin Officer Complete EIA Check";
    public const string AdminOfficerCompleteEiaCheckFailure = "Admin Officer Complete EIA Check Failure";
    public const string WoodlandOfficerReviewEiaScreening = "Woodland Officer Review EIA Screening";
    public const string WoodlandOfficerReviewEiaScreeningFailure = "Woodland Officer Review EIA Screening Failure";

    // amendment review
    public const string ApplicantReviewedAmendments = "Applicant Reviewed Amendments";
    public const string ApplicantReviewedAmendmentsFailure = "Applicant Reviewed Amendments Failure";


    /// <summary>
    /// Returns the source entity type by a given audit event name.
    /// </summary>
    /// <param name="eventName">A string representing the audit event name</param>
    /// <returns>An enum <see cref="SourceEntityType"/> value representing the source entity type.</returns>
    /// <exception cref="NotSupportedException">Throws NotSupportedException when a source entity type is not mapped to the audit event.</exception>
    public static SourceEntityType GetEventSourceEntityType(string eventName) =>
        eventName switch
        {
            RegisterAuditEvent => SourceEntityType.UserAccount,
            UpdateAccountEvent => SourceEntityType.UserAccount,
            RegisterFailureEvent => SourceEntityType.UserAccount,
            UpdateAccountFailureEvent => SourceEntityType.UserAccount,
            RegisterAccountDueToNewInternalFcUserAccountApprovalAuditEvent => SourceEntityType.UserAccount,
            RegisterAccountDueToNewInternalFcUserAccountApprovalAuditEventFailure => SourceEntityType.UserAccount,

            WoodlandOwnerUserInvitationSent =>  SourceEntityType.UserAccount,
            WoodlandOwnerUserInvitationFailure =>  SourceEntityType.UserAccount,
            AgencyUserInvitationSent =>  SourceEntityType.UserAccount,
            AgencyUserInvitationFailure =>  SourceEntityType.UserAccount,
            AcceptInvitationSuccess =>  SourceEntityType.UserAccount,
            AcceptInvitationFailure =>  SourceEntityType.UserAccount,

            UpdateWoodlandOwnerEvent => SourceEntityType.WoodlandOwner,
            UpdateWoodlandOwnerFailureEvent => SourceEntityType.WoodlandOwner,

            CreatePropertyProfileEvent =>  SourceEntityType.PropertyProfile,
            CreatePropertyProfileFailureEvent =>  SourceEntityType.PropertyProfile,
            UpdatePropertyProfileEvent =>  SourceEntityType.PropertyProfile,
            UpdatePropertyProfileFailureEvent =>  SourceEntityType.PropertyProfile,

            CreateCompartmentEvent =>  SourceEntityType.Compartment,
            CreateCompartmentFailureEvent =>  SourceEntityType.Compartment,
            UpdateCompartmentEvent =>  SourceEntityType.Compartment,
            UpdateCompartmentFailureEvent =>  SourceEntityType.Compartment,

            CreateFellingLicenceApplication => SourceEntityType.FellingLicenceApplication,
            CreateFellingLicenceApplicationFailure => SourceEntityType.FellingLicenceApplication,
            UpdateFellingLicenceApplication => SourceEntityType.FellingLicenceApplication,
            UpdateFellingLicenceApplicationFailure => SourceEntityType.FellingLicenceApplication,
            AddFellingLicenceAttachmentEvent => SourceEntityType.FellingLicenceApplication,
            AddFellingLicenceAttachmentFailureEvent => SourceEntityType.FellingLicenceApplication,
            RemoveFellingLicenceAttachmentEvent => SourceEntityType.FellingLicenceApplication,
            RemoveFellingLicenceAttachmentFailureEvent => SourceEntityType.FellingLicenceApplication,
            GetFellingLicenceAttachmentEvent => SourceEntityType.FellingLicenceApplication,
            GetFellingLicenceAttachmentFailureEvent => SourceEntityType.FellingLicenceApplication,

            SubmitFellingLicenceApplication => SourceEntityType.FellingLicenceApplication,
            SubmitFellingLicenceApplicationFailure => SourceEntityType.FellingLicenceApplication,
            ResubmitFellingLicenceApplication => SourceEntityType.FellingLicenceApplication,
            ResubmitFellingLicenceApplicationFailure => SourceEntityType.FellingLicenceApplication,
            WithdrawFellingLicenceApplication => SourceEntityType.FellingLicenceApplication,
            WithdrawFellingLicenceApplicationFailure => SourceEntityType.FellingLicenceApplication,
            DeleteDraftFellingLicenceApplication => SourceEntityType.FellingLicenceApplication,
            DeleteDraftFellingLicenceApplicationFailure => SourceEntityType.FellingLicenceApplication,

            PropertyProfileSnapshotCreated => SourceEntityType.FellingLicenceApplication,
            FellingLicenceApplicationAssignedToAdminHub => SourceEntityType.FellingLicenceApplication,
            FellingLicenceApplicationFinalActionDateUpdated => SourceEntityType.FellingLicenceApplication,
            FellingLicenceApplicationSubmissionNotificationSent => SourceEntityType.FellingLicenceApplication,
            FellingLicenceApplicationSubmissionComplete => SourceEntityType.FellingLicenceApplication,
            FellingLicenceApplicationSubmissionFailure => SourceEntityType.FellingLicenceApplication,

            AssignFellingLicenceApplicationToStaffMember => SourceEntityType.FellingLicenceApplication,
            AssignFellingLicenceApplicationToStaffMemberFailure => SourceEntityType.FellingLicenceApplication,
            AssignFellingLicenceApplicationToApplicant => SourceEntityType.FellingLicenceApplication,
            AssignFellingLicenceApplicationToApplicantFailure => SourceEntityType.FellingLicenceApplication,

            CalculateCentrePointForApplication => SourceEntityType.FellingLicenceApplication,
            CalculateCentrePointForApplicationFailure => SourceEntityType.FellingLicenceApplication,

            UpdateDateReceived => SourceEntityType.FellingLicenceApplication,
            UpdateDateReceivedFailure => SourceEntityType.FellingLicenceApplication,

            FellingLicenceApplicationWithdrawNotificationSent => SourceEntityType.FellingLicenceApplication,
            FellingLicenceApplicationWithdrawNotificationSentFailed => SourceEntityType.FellingLicenceApplication,
            FellingLicenceApplicationWithdrawComplete => SourceEntityType.FellingLicenceApplication,
            FellingLicenceApplicationWithdrawFailure => SourceEntityType.FellingLicenceApplication,

            LISConstraintReportConsumedOk => SourceEntityType.FellingLicenceApplication,
            LISConstraintReportConsumedFailure => SourceEntityType.FellingLicenceApplication,
            ConstraintCheckerExecutionSuccess => SourceEntityType.FellingLicenceApplication,
            ConstraintCheckerExecutionFailure => SourceEntityType.FellingLicenceApplication,

            AddAdminOfficerToAdminHub => SourceEntityType.AdminHub,
            AddAdminOfficerToAdminHubFailure => SourceEntityType.AdminHub,
            RemoveAdminOfficerFromAdminHub => SourceEntityType.AdminHub,
            RemoveAdminOfficerFromAdminHubFailure =>SourceEntityType.AdminHub,
            UpdateAdminHubDetails => SourceEntityType.AdminHub,
            UpdateAdminHubDetailsFailure => SourceEntityType.AdminHub,

            ExternalConsulteeInvitationSent => SourceEntityType.FellingLicenceApplication,
            ExternalConsulteeInvitationFailure => SourceEntityType.FellingLicenceApplication,
            AddConsulteeComment => SourceEntityType.FellingLicenceApplication,
            AddConsulteeCommentFailure => SourceEntityType.FellingLicenceApplication,

            UpdateWoodlandOfficerReview => SourceEntityType.FellingLicenceApplication,
            UpdateWoodlandOfficerReviewFailure => SourceEntityType.FellingLicenceApplication,
            AddToConsultationPublicRegisterSuccess => SourceEntityType.FellingLicenceApplication,
            AddToConsultationPublicRegisterFailure => SourceEntityType.FellingLicenceApplication,
            UpdatePw14Checks => SourceEntityType.FellingLicenceApplication,
            UpdatePw14ChecksFailure => SourceEntityType.FellingLicenceApplication,
            UpdateSiteVisit => SourceEntityType.FellingLicenceApplication,
            UpdateSiteVisitFailure => SourceEntityType.FellingLicenceApplication,
            ConfirmWoodlandOfficerReview => SourceEntityType.FellingLicenceApplication,
            ConfirmWoodlandOfficerReviewFailure => SourceEntityType.FellingLicenceApplication,
            ConfirmWoodlandOfficerReviewNotificationSent => SourceEntityType.FellingLicenceApplication,
            ConfirmWoodlandOfficerReviewNotificationFailure => SourceEntityType.FellingLicenceApplication,
            RevertApproveToWoodlandOfficerReview => SourceEntityType.FellingLicenceApplication,
            UpdateConfirmedFellingDetails => SourceEntityType.FellingLicenceApplication,
            UpdateConfirmedFellingDetailsFailure => SourceEntityType.FellingLicenceApplication,
            UpdateConfirmedRestockingDetails => SourceEntityType.FellingLicenceApplication,
            UpdateConfirmedRestockingDetailsFailure => SourceEntityType.FellingLicenceApplication,
            RevertConfirmedFellingDetails => SourceEntityType.ProposedFellingDetails,
            RevertConfirmedFellingDetailsFailure => SourceEntityType.ProposedFellingDetails,

            ConfirmAdminOfficerReview => SourceEntityType.FellingLicenceApplication,
            ConfirmAdminOfficerReviewFailure => SourceEntityType.FellingLicenceApplication,
            ConfirmAdminOfficerReviewNotificationSent => SourceEntityType.FellingLicenceApplication,
            ConfirmAdminOfficerReviewNotificationFailure => SourceEntityType.FellingLicenceApplication,
            UpdateAdminOfficerReview => SourceEntityType.FellingLicenceApplication,
            UpdateAdminOfficerReviewFailure => SourceEntityType.FellingLicenceApplication,
            RevertApproveToAdminOfficerReview => SourceEntityType.FellingLicenceApplication,

            RetrieveActivityFeedItems => SourceEntityType.FellingLicenceApplication,
            RetrieveActivityFeedItemsFailure => SourceEntityType.FellingLicenceApplication,

            ImportFellingRestockingDetails => SourceEntityType.FellingLicenceApplication,
            ImportFellingRestockingDetailsFailure => SourceEntityType.FellingLicenceApplication,
            SaveChangesFellingRestockingDetails => SourceEntityType.FellingLicenceApplication,
            SaveChangesFellingRestockingDetailsFailure => SourceEntityType.FellingLicenceApplication,

            ConditionsBuiltForApplication => SourceEntityType.FellingLicenceApplication,
            ConditionsBuiltForApplicationFailure => SourceEntityType.FellingLicenceApplication,
            ConditionsSavedForApplication => SourceEntityType.FellingLicenceApplication,
            ConditionsSavedForApplicationFailure => SourceEntityType.FellingLicenceApplication,

            CreateAgentAuthorityForm => SourceEntityType.Agency,
            CreateAgentAuthorityFormFailure => SourceEntityType.Agency,
            RemoveAgentAuthorityForm => SourceEntityType.Agency,
            RemoveAgentAuthorityFormFailure => SourceEntityType.Agency,
            DownloadAgentAuthorityFormFiles => SourceEntityType.Agency,
            DownloadAgentAuthorityFormFilesFailure => SourceEntityType.Agency,
            AddAgentAuthorityFormFiles => SourceEntityType.Agency,
            AddAgentAuthorityFormFilesFailure => SourceEntityType.Agency,
            AddAgentAuthorityFormFilesValidationFailure => SourceEntityType.Agency,

            SetAgentAuthorityStatus => SourceEntityType.AgentAuthority,
            SetAgentAuthorityStatusFailure => SourceEntityType.AgentAuthority,

            FcAgentUserCreateWoodlandOwnerEvent => SourceEntityType.FcUser,
            FcAgentUserCreateWoodlandOwnerFailureEvent => SourceEntityType.FcUser,
            FcUserCreateAgencyEvent => SourceEntityType.FcUser,
            FcUserCreateAgencyFailureEvent => SourceEntityType.FcUser,

            ImportDataFromCsv => SourceEntityType.WoodlandOwner,
            ImportDataFromCsvFailure => SourceEntityType.WoodlandOwner,

            SaveApproverReview => SourceEntityType.FellingLicenceApplication,
            SaveApproverReviewFailure => SourceEntityType.FellingLicenceApplication,
            ApplicationApproved => SourceEntityType.FellingLicenceApplication,
            ApplicationRefused => SourceEntityType.FellingLicenceApplication,
            ApplicationReferredToLocalAuthority => SourceEntityType.FellingLicenceApplication,

            ApplicationExtensionNotification => SourceEntityType.FellingLicenceApplication,
            ApplicationVoluntaryWithdrawalNotification => SourceEntityType.FellingLicenceApplication,

            AccountAdministratorUpdateExistingAccount => SourceEntityType.UserAccount,
            AccountAdministratorUpdateExistingAccountFailure => SourceEntityType.UserAccount,
            AccountAdministratorCloseAccount => SourceEntityType.UserAccount,
            AccountAdministratorCloseAccountFailure => SourceEntityType.UserAccount,
            AdministratorUpdateExternalAccount => SourceEntityType.UserAccount,
            AdministratorUpdateExternalAccountFailure => SourceEntityType.UserAccount,
            AdministratorCloseExternalAccount => SourceEntityType.UserAccount,
            AdministratorCloseExternalAccountFailure => SourceEntityType.UserAccount,

            GeneratingPdfFellingLicence => SourceEntityType.FellingLicenceApplication,
            GeneratingPdfFellingLicenceFailure => SourceEntityType.FellingLicenceApplication,

            ReportExecution =>SourceEntityType.FellingLicenceApplication,
            ReportExecutionNoDataFound => SourceEntityType.FellingLicenceApplication,
            ReportExecutionFailure => SourceEntityType.FellingLicenceApplication,

            AccessLegacyDocumentEvent => SourceEntityType.WoodlandOwner,
            AccessLegacyDocumentFailureEvent => SourceEntityType.WoodlandOwner,

            ConsultationPublicRegisterAutomatedExpirationRemovalSuccess => SourceEntityType.FellingLicenceApplication,
            ConsultationPublicRegisterAutomatedExpirationRemovalFailure => SourceEntityType.FellingLicenceApplication,
            ConsultationPublicRegisterApplicationRemovalNotification => SourceEntityType.FellingLicenceApplication,

            DecisionPublicRegisterAutomatedExpirationRemovalSuccess => SourceEntityType.FellingLicenceApplication,
            DecisionPublicRegisterAutomatedExpirationRemovalFailure => SourceEntityType.FellingLicenceApplication,
            DecisionPublicRegisterApplicationRemovalNotification => SourceEntityType.FellingLicenceApplication,

            RevertApplicationFromWithdrawnSuccess => SourceEntityType.FellingLicenceApplication,
            RevertApplicationFromWithdrawnFailure => SourceEntityType.FellingLicenceApplication,

            ApplicantUploadEiaDocumentsSuccess => SourceEntityType.FellingLicenceApplication,
            ApplicantUploadEiaDocumentsFailure => SourceEntityType.FellingLicenceApplication,
            ApplicantCompleteEiaSection => SourceEntityType.FellingLicenceApplication,
            ApplicantCompleteEiaSectionFailure => SourceEntityType.FellingLicenceApplication,

            AdminOfficerCompleteEiaCheck => SourceEntityType.FellingLicenceApplication,
            AdminOfficerCompleteEiaCheckFailure => SourceEntityType.FellingLicenceApplication,

            WoodlandOfficerReviewEiaScreening => SourceEntityType.FellingLicenceApplication,
            WoodlandOfficerReviewEiaScreeningFailure => SourceEntityType.FellingLicenceApplication,

            ApplicantReviewedAmendments => SourceEntityType.FellingLicenceApplication,
            ApplicantReviewedAmendmentsFailure => SourceEntityType.FellingLicenceApplication,

            _ => throw new NotSupportedException($"{eventName} event is not supported")
        };
}