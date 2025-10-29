using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Microsoft.AspNetCore.Mvc.Routing;

namespace Forestry.Flo.External.Web.Models.FellingLicenceApplication
{
    public class FellingDetailPlaybackViewModel : ApplicationStepBase
    {
        public ProposedFellingDetail FellingDetail { get; set; }
        public string FellingCompartmentName { get; set; }
        public List<RestockingCompartmentPlaybackViewModel> RestockingCompartmentDetails { get; set; }
        public UrlActionContext AreaChangeLink { get; set; }
        public UrlActionContext NoofTreesChangeLink { get; set; }
        public UrlActionContext TreeMarkingChangeLink { get; set; }
        public UrlActionContext SpeciesChangeLink { get; set; }
        public UrlActionContext TPOChangeLink { get; set; }
        public UrlActionContext ConservationAreaChangeLink { get; set; }
        public UrlActionContext WillYouRestockChangeLink { get; set; }
        public UrlActionContext RestockingCompartmentsChangeLink { get; set; }
        public UrlActionContext EstimateVolumeChangeLink { get; set; }

        public string SpeciesString
        {
            get
            {
                if (FellingDetail.FellingSpecies != null && FellingDetail.FellingSpecies.Any())
                {
                    return string.Join(", ", FellingDetail.FellingSpecies.Select(f => TreeSpeciesFactory.SpeciesDictionary[f.Species].Name));
                }

                return string.Empty;
            }
        }

        public string RestockingCompartmentsString
        {
            get
            {
                if (RestockingCompartmentDetails != null && RestockingCompartmentDetails.Any())
                {
                    return string.Join(", ", RestockingCompartmentDetails.Select(r => r.CompartmentName));
                }
                return string.Empty;
            }
        }
    }
}
