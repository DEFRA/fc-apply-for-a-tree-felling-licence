using Forestry.Flo.Internal.Web.Services.FellingLicenceApplication;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.InternalUsers.Entities.UserAccount;

namespace Forestry.Flo.Internal.Web.Models.FellingLicenceApplication
{
    public class FellingLicenceApplicationAssignmentListModel
    {
        public IList<FellingLicenceAssignmentListItemModel> AssignedFellingLicenceApplicationModels { get; set; } = new List<FellingLicenceAssignmentListItemModel>(0);

        public IList<FellingLicenceStatusCount> FellingLicenceStatusCount { get; set; } = new List<FellingLicenceStatusCount>(0);

        public int AssignedToUserCount { get; set; }
    }

    public class FellingLicenceStatusCount
    {
        public FellingLicenceStatus FellingLicenceStatus { get; set; }

        public int Count { get; set; }
    }
}
