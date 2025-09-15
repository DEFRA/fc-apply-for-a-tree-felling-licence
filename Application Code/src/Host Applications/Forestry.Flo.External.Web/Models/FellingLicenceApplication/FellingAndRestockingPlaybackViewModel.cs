using Microsoft.AspNetCore.Mvc.Routing;

namespace Forestry.Flo.External.Web.Models.FellingLicenceApplication
{
    public class FellingAndRestockingPlaybackViewModel : ApplicationStepBase
    {
        public string? ApplicationReference { get; set; }
        public UrlActionContext FellingCompartmentsChangeLink { get; set; }
        public string? GIS { get; set; }
        public List<FellingCompartmentPlaybackViewModel> FellingCompartmentDetails { get; set; }

        /// <summary>
        /// Gets or sets the link context for the "Save and continue" action.
        /// </summary>
        public required UrlActionContext SaveAndContinueContext { get; set; }
    }
}