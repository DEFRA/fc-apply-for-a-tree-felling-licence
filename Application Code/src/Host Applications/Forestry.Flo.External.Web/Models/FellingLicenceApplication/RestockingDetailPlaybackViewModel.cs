using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Microsoft.AspNetCore.Mvc.Routing;

namespace Forestry.Flo.External.Web.Models.FellingLicenceApplication
{
    public class RestockingDetailPlaybackViewModel : ApplicationStepBase
    {
        public UrlActionContext AreaChangeLink { get; set; }
        public UrlActionContext PercentageChangeLink { get; set; }
        public UrlActionContext DensityChangeLink { get; set; }
        public UrlActionContext NumberOfTreesChangeLink { get; set; }
        public UrlActionContext SpeciesChangeLink { get; set; }
        public ProposedRestockingDetail RestockingDetail { get; set; }
        public string RestockingCompartmentName { get; set; }

        public string SpeciesAndRestockingPercentageDisplay
        {
            get
            {
                var display = string.Empty;

                if (RestockingDetail.RestockingSpecies != null && RestockingDetail.RestockingSpecies.Any())
                {
                    display = string.Join(", ", RestockingDetail.RestockingSpecies.Select(rs => $"{TreeSpeciesFactory.SpeciesDictionary[rs.Species].Name}: {rs.Percentage}%"));
                }

                return display;
            }
        }
    }
}
