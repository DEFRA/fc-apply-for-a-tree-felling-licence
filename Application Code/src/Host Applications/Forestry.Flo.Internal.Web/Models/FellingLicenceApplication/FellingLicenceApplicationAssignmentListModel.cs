using Forestry.Flo.Internal.Web.Services.FellingLicenceApplication;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;

namespace Forestry.Flo.Internal.Web.Models.FellingLicenceApplication
{
    public class FellingLicenceApplicationAssignmentListModel
    {
        public IList<FellingLicenceAssignmentListItemModel> AssignedFellingLicenceApplicationModels { get; set; } = new List<FellingLicenceAssignmentListItemModel>(0);

        public IList<FellingLicenceStatusCount> FellingLicenceStatusCount { get; set; } = new List<FellingLicenceStatusCount>(0);

        public int AssignedToUserCount { get; set; }

        // Pagination properties
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 12;
        public int TotalCount { get; set; }
        public int TotalPages => (int)System.Math.Ceiling((double)TotalCount / PageSize);
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
    }

    public class FellingLicenceStatusCount
    {
        public FellingLicenceStatus FellingLicenceStatus { get; set; }

        public int Count { get; set; }
    }
}
