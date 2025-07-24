using Microsoft.AspNetCore.Mvc;

namespace Forestry.Flo.External.Web.Models.FellingLicenceApplication
{
    public class SupportingDocumentationModel : ApplicationStepBase, IApplicationWithBreadcrumbsViewModel
    {
        /// <summary>
        /// Gets or sets the Application id
        /// </summary>
        [HiddenInput]
        public override Guid ApplicationId { get; set; }

        /// <summary>
        /// Gets or sets the application reference
        /// </summary>
        public string? ApplicationReference { get; set; }

        /// <summary>
        /// Gets or sets the model for displaying breadcrumbs
        /// </summary>
        public BreadcrumbsModel? Breadcrumbs { get; set; }

        /// <summary>
        /// Gets or sets the the task name for displaying in breadcrumbs and step descriptions in audit records
        /// </summary>
        public string TaskName => " Supporting documentation";

        /// <summary>
        /// Get or sets the new list of documents for the application
        /// </summary>
        public IEnumerable<DocumentModel> Documents { get; set; } = Enumerable.Empty<DocumentModel>();
    }
}
