using Forestry.Flo.Services.Common.Extensions;
using Microsoft.AspNetCore.Mvc.Routing;

namespace Forestry.Flo.External.Web.Models.FellingLicenceApplication
{
    public class RestockingCompartmentPlaybackViewModel : ApplicationStepBase
    {
        public Guid CompartmentId { get; set; }
        public string CompartmentName { get; set; }
        public UrlActionContext RestockingOptionsChangeLink { get; set; }
        public List<RestockingDetailPlaybackViewModel> RestockingDetails { get; set; }

        public string RestockingOperationsString
        {
            get
            {
                if (RestockingDetails != null && RestockingDetails.Any())
                {
                    return string.Join(",", RestockingDetails.Select(r => r.RestockingDetail.RestockingProposal.GetDisplayName()));
                }
                return string.Empty;
            }
        }
    }
}
