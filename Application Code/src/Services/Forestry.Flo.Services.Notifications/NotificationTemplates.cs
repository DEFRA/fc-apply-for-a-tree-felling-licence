using Forestry.Flo.Services.Notifications.Entities;

namespace Forestry.Flo.Services.Notifications;

public static class NotificationTemplates
{
    public static string NotificationBodyTemplatePath(NotificationType notificationType) =>
        notificationType switch
        {
            NotificationType.InviteWoodlandOwnerUserToOrganisation => "Resources.InviteWoodlandOwnerToOrganisation",
            NotificationType.InviteAgentUserToOrganisation => "Resources.InviteAgentToOrganisation",
            NotificationType.ApplicationSubmissionConfirmation => "Resources.ApplicationSubmissionConfirmation",
            NotificationType.ApplicationWithdrawnConfirmation => "Resources.ApplicationWithdrawnConfirmation",
            NotificationType.UserAssignedToApplication => "Resources.UserAssignedToApplication",
            NotificationType.InformWoodlandOfficerOfAdminOfficerReviewCompletion => "Resources.InformWoodlandOfficerOfAdminOfficerReviewCompletion",
            NotificationType.InformFieldManagerOfWoodlandOfficerReviewCompletion => "Resources.InformFieldManagerOfWoodlandOfficerReviewCompletion",
            NotificationType.InformApplicantOfApplicationVoluntaryWithdrawOption => "Resources.InformApplicantOfApplicationVoluntaryWithdrawOption",
            NotificationType.ApplicationResubmitted => "Resources.ApplicationResubmitted",
            NotificationType.ApplicationWithdrawn => "Resources.ApplicationWithdrawn",
            NotificationType.ExternalConsulteeInvite => "Resources.ExternalConsulteeInvite",
            NotificationType.ExternalConsulteeInviteWithPublicRegisterInfo => "Resources.ExternalConsulteeInvite",
            NotificationType.InformApplicantOfReturnedApplication => "Resources.InformApplicantOfReturnedApplication",
            NotificationType.InformFCStaffOfReturnedApplication => "Resources.InformFCStaffOfReturnedApplication",
            NotificationType.ConditionsToApplicant => "Resources.ConditionsToApplicant",
            NotificationType.InformApplicantOfApplicationExtension => "Resources.InformApplicantOfApplicationExtension",
            NotificationType.InformFCStaffOfFinalActionDateReached => "Resources.InformFCStaffOfFinalActionDateReached",
            NotificationType.InformApplicantOfApplicationApproval => "Resources.InformApplicantOfApplicationApproval",
            NotificationType.InformApplicantOfApplicationRefusal => "Resources.InformApplicantOfApplicationRefusal",
            NotificationType.InformAdminOfNewAccountSignup => "Resources.InformAdminOfNewAccountSignup.cshtml",
            NotificationType.InformInternalUserOfAccountApproval => "Resources.InformInternalUserOfAccountApproval.cshtml",
            NotificationType.InformApplicantOfApplicationReferredToLocalAuthority => "Resources.InformApplicantOfApplicationReferredToLocalAuthority.cshtml",
            NotificationType.InformFcStaffOfApplicationRemovedFromDecisionPublicRegisterFailure => "Resources.InformFcStaffOfApplicationRemovedFromDecisionPublicRegisterFailure.cshtml",
            NotificationType.InformFcStaffOfApplicationAddedToConsultationPublicRegister => "Resources.InformFcStaffOfApplicationAddedToConsultationPublicRegister.cshtml",
            NotificationType.InformFcStaffOfApplicationAddedToDecisionPublicRegister => "Resources.InformFcStaffOfApplicationAddedToDecisionPublicRegister.cshtml",
            _ => throw new NotSupportedException($"{notificationType} is not supported")
        };

    public static string NotificationSubjectTemplate<T>(NotificationType notificationType, T model)
    {
        return notificationType switch
        {
            NotificationType.InviteWoodlandOwnerUserToOrganisation =>
                "You have been invited to sign up to Felling Licence Online",
            NotificationType.InviteAgentUserToOrganisation =>
                "You have been invited to sign up to Felling Licence Online",
            NotificationType.ApplicationSubmissionConfirmation =>
                "Your Felling Licence Application has been successfully submitted",
            NotificationType.ApplicationWithdrawnConfirmation =>
                "Your Felling Licence Application has been successfully withdrawn",
            NotificationType.UserAssignedToApplication =>
                "You have been assigned to work on Felling Licence Application " + model.GetType().GetProperty("ApplicationReference")?.GetValue(model),
            NotificationType.InformWoodlandOfficerOfAdminOfficerReviewCompletion =>
                "The operations admin officer review for Felling Licence Application " + model.GetType().GetProperty("ApplicationReference")?.GetValue(model) + " has been completed",
            NotificationType.InformFieldManagerOfWoodlandOfficerReviewCompletion =>
                "The woodland officer review for Felling Licence Application " + model.GetType().GetProperty("ApplicationReference")?.GetValue(model) + " has been completed",
            NotificationType.InformApplicantOfApplicationVoluntaryWithdrawOption =>
                "Your Felling Licence Application " + model.GetType().GetProperty("ApplicationReference")?.GetValue(model) + " requires attention",
            NotificationType.ApplicationResubmitted =>
                "An application to which you are assigned has been resubmitted " + model.GetType().GetProperty("ApplicationReference")?.GetValue(model),
            NotificationType.ExternalConsulteeInvite =>
                "You have been assigned as external consultee for Felling Licence Application " + model.GetType().GetProperty("ApplicationReference")?.GetValue(model),
            NotificationType.ExternalConsulteeInviteWithPublicRegisterInfo =>
                "You have been assigned as external consultee for Felling Licence Application " + model.GetType().GetProperty("ApplicationReference")?.GetValue(model),
            NotificationType.InformApplicantOfReturnedApplication =>
                "Your Felling Licence Application " + model.GetType().GetProperty("ApplicationReference")?.GetValue(model) + " has been returned to you",
            NotificationType.InformFCStaffOfReturnedApplication =>
                 "An application to which you are assigned has been returned to applicant " + model.GetType().GetProperty("ApplicationReference")?.GetValue(model),
            NotificationType.ConditionsToApplicant =>
                "Your Felling Licence Application " + model.GetType().GetProperty("ApplicationReference")?.GetValue(model) + " has had conditions applied",
            NotificationType.InformApplicantOfApplicationExtension =>
                "The final action date for your Felling Licence Application " + model.GetType().GetProperty("ApplicationReference")?.GetValue(model) + " has been extended",
            NotificationType.InformFCStaffOfFinalActionDateReached =>
                "Felling Licence Application " + model.GetType().GetProperty("ApplicationReference")?.GetValue(model) + " has reached its final action date without being processed",
            NotificationType.InformApplicantOfApplicationApproval =>
                "Your Felling Licence Application " + model.GetType().GetProperty("ApplicationReference")?.GetValue(model) + " has been approved",
            NotificationType.InformApplicantOfApplicationRefusal =>
                "Your Felling Licence Application " + model.GetType().GetProperty("ApplicationReference")?.GetValue(model) + " has been refused",
            NotificationType.InformApplicantOfApplicationReferredToLocalAuthority =>
                "Your Felling Licence Application " + model.GetType().GetProperty("ApplicationReference")?.GetValue(model) + " has been referred to the local authority",
            NotificationType.InformAdminOfNewAccountSignup =>
                "A new account is awaiting approval in FLOv2",
            NotificationType.InformInternalUserOfAccountApproval => "Your account has been approved",
            NotificationType.InformFcStaffOfApplicationRemovedFromDecisionPublicRegisterFailure =>
                "Felling Licence Application " + model.GetType().GetProperty("ApplicationReference")?.GetValue(model) + " could not be automatically removed from the decision " +
                "public register",
            NotificationType.InformFcStaffOfApplicationAddedToConsultationPublicRegister => "Felling Licence Application " + model.GetType().GetProperty("ApplicationReference")?.GetValue(model) + " has been added to the consultation public register",
            NotificationType.InformFcStaffOfApplicationAddedToDecisionPublicRegister => "Felling Licence Application " + model.GetType().GetProperty("ApplicationReference")?.GetValue(model) + " has been added to the decision public register",
            _ => throw new NotSupportedException($"{notificationType} is not supported")
        };
    }
}