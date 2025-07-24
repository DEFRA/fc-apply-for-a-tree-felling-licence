using Forestry.Flo.External.Web.Models.FellingLicenceApplication;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Forestry.Flo.External.Web.Views.Shared.Components.GdsApplicationStatusTag
{
    /// <summary>
    /// https://design-system.service.gov.uk/components/tag/
    /// </summary>
    [ViewComponent(Name = "GdsApplicationStatusTag")]
    public class GdsApplicationStatusTagViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(FellingLicenceStatus applicationState)
        {
            return View("Default", new GdsApplicationStatusTagViewComponentModel{ApplicationState = applicationState});
        }
    }
}