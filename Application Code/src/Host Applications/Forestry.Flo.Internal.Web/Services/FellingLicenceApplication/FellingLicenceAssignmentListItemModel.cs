using Forestry.Flo.Services.FellingLicenceApplications.Entities;

namespace Forestry.Flo.Internal.Web.Services.FellingLicenceApplication
{
    public class FellingLicenceAssignmentListItemModel
    {
        public IList<Guid>? AssignedUserIds { get; set; }

        public Guid FellingLicenceApplicationId { get; set; }

        public string? Reference { get; set; }

        public string? Property { get; set; }

        public DateTime? Deadline { get; set; }

        public FellingLicenceStatus FellingLicenceStatus { get; set; }

        public List<string>? UserFirstLastNames { get; set; }

        public DateTime? SubmittedDate { get; set; }

        public DateTime? CitizensCharterDate { get; set; }
    }
}
