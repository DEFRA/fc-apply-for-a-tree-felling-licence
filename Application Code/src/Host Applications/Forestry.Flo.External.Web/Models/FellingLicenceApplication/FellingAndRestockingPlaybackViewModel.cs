using Microsoft.AspNetCore.Mvc.Routing;

namespace Forestry.Flo.External.Web.Models.FellingLicenceApplication
{
    public class FellingAndRestockingPlaybackViewModel : ApplicationStepBase
    {
        public string? ApplicationReference { get; set; }
        public UrlActionContext FellingCompartmentsChangeLink { get; set; }
        public string? GIS { get; set; }
        public List<FellingCompartmentPlaybackViewModel> FellingCompartmentDetails { get; set; }
    }
}