using Forestry.Flo.Internal.Web.Models.UserAccount;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;

namespace Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;

public class AssigneeHistoryModel
{
    public UserAccountModel? UserAccount { get; set; }

    public ExternalApplicantModel? ExternalApplicant { get; set; }

    public AssignedUserRole Role { get; set; }

    public DateTime TimestampAssigned { get; set; }

    public DateTime? TimestampUnassigned { get; set; }
}