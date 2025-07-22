using Microsoft.AspNetCore.Mvc;

namespace Forestry.Flo.Internal.Web.Models.FellingLicenceApplication
{
    /// <summary>
    /// Model class representing the view for reopening a withdrawn felling licence application.
    /// </summary>
    public class ReopenWithdrawnApplicationModel : FellingLicenceApplicationPageViewModel
    {
        /// <summary>
        /// Gets and sets the ID of the felling licence application to be reopened.
        /// </summary>
        [HiddenInput]
        public required Guid ApplicationId { get; set; }

        /// <summary>
        /// Gets and sets the activity feed model.
        /// </summary>
        public required ActivityFeedModel ActivityFeed { get; set; }
    }
}
