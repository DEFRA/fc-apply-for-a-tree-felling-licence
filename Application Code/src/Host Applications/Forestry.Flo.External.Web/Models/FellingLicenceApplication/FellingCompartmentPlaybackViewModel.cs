using Forestry.Flo.Services.Common.Extensions;
using Microsoft.AspNetCore.Mvc.Routing;

namespace Forestry.Flo.External.Web.Models.FellingLicenceApplication
{
    public class FellingCompartmentPlaybackViewModel : ApplicationStepBase
    {
        public Guid CompartmentId { get; set; }
        public string CompartmentName { get; set; }
        public UrlActionContext FellingOperationsChangeLink { get; set; }
        public List<FellingDetailPlaybackViewModel> FellingDetails { get; set; }

        public string OperationsString
        {
            get
            {
                if (FellingDetails != null && FellingDetails.Any())
                {
                    return string.Join(",", FellingDetails.Select(fd => fd.FellingDetail.OperationType.GetDisplayName()));
                }

                return string.Empty;
            }
        }
    }
}
