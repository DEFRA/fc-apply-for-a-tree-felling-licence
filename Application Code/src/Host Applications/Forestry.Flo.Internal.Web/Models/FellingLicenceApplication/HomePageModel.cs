using Forestry.Flo.Services.InternalUsers.Entities.UserAccount;

namespace Forestry.Flo.Internal.Web.Models.FellingLicenceApplication
{
    public class HomePageModel : PageWithBreadcrumbsViewModel
    {
        public FellingLicenceApplicationAssignmentListModel FellingLicenceApplicationAssignmentListModel { get; set; } = new();

        public IList<Roles> SignedInUserRoles { get; set; } = new List<Roles>(0);
    }
}
