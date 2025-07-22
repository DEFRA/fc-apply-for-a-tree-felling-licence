using Forestry.Flo.Services.FellingLicenceApplications.Entities;

namespace Forestry.Flo.Services.FellingLicenceApplications.Models;

public class ApplicationCompartmentDetail
{
    public Guid ApplicationId { get; set; }
    public string ApplicationReference { get; set; } = null!;
    public Guid PropertyProfileId { get; set; }
    public IList<ProposedFellingDetail> ProposedFellingDetails { get; set; } = null!;
    public IList<StatusHistory> StatusHistories { get; set; } = new List<StatusHistory>();
}