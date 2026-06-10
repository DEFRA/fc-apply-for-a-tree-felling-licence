using Forestry.Flo.Services.FellingLicenceApplications.Entities;

namespace Forestry.Flo.External.Web.Models.FellingLicenceApplication
{
    /// <summary>
    /// View model class for confirming the withdrawal of a felling licence application.
    /// </summary>
    public class ConfirmWithdrawFellingLicenceApplicationViewModel : IApplicationWithBreadcrumbsViewModel
    {
        /// <summary>
        /// Gets and sets the application ID.
        /// </summary>
        public Guid ApplicationId { get; set; }
        
        /// <summary>
        /// Gets and sets the application reference.
        /// </summary>
        public string? ApplicationReference { get; set; }

        /// <summary>
        /// Gets and sets the summary details of the felling licence application to be displayed on the confirm
        /// withdrawal page, including details such as the woodland owner name, agent and property name.
        /// </summary>
        public FellingLicenceApplicationSummary ApplicationSummary { get; set; }

        /// <summary>
        /// Gets and sets the page navigation breadcrumbs.
        /// </summary>
        public BreadcrumbsModel? Breadcrumbs { get; set; }
        
        /// <summary>
        /// Gets and sets the page task name.
        /// </summary>
        public string TaskName { get; set; }

        /// <summary>
        /// Gets and sets the withdrawal reason options available for the applicant to select from when confirming
        /// the withdrawal of their felling licence application.
        /// </summary>
        public Dictionary<WithdrawalReason, bool> WithdrawalReasonOptions { get; set; } = new();

        /// <summary>
        /// Gets and sets the details of the withdrawal reason if the applicant selects "Other" as their reason
        /// for withdrawing their felling licence application.
        /// </summary>
        public string? WithdrawalReasonsOtherDetails { get; set; }
    }
}
