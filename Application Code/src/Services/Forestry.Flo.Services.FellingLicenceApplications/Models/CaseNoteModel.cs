using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.InternalUsers.Entities.UserAccount;

namespace Forestry.Flo.Services.FellingLicenceApplications.Models;

public class CaseNoteModel
{
    public Guid? Id { get; set; }

    public Guid FellingLicenceApplicationId { get; set; }

    public CaseNoteType Type { get; set; }

    public string Text { get; set; }

    public bool VisibleToApplicant { get; set; }

    public bool VisibleToConsultee { get; set; }

    public DateTime CreatedTimestamp { get; set; }

    public Guid CreatedByUserId { get; set; }

    public string CreatedByUserName { get; set; }

    public AccountTypeInternal CreatedByUserAccountType { get; set; }
}