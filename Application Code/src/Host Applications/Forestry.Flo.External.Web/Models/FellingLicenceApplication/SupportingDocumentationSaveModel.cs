using Microsoft.AspNetCore.Mvc;

namespace Forestry.Flo.External.Web.Models.FellingLicenceApplication
{
    public class SupportingDocumentationSaveModel : ApplicationStepBase
    {
        /// <summary>
        /// Gets or sets the Application id
        /// </summary>
        [HiddenInput]
        public override Guid ApplicationId { get; set; }
    }
}
