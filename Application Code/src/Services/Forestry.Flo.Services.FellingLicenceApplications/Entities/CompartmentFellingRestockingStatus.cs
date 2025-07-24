namespace Forestry.Flo.Services.FellingLicenceApplications.Entities
{
    public class CompartmentFellingRestockingStatus
    {
        public Guid CompartmentId { get; set; }
        public bool? Status { get; set; }

        public List<FellingStatus> FellingStatuses { get; set; } = new List<FellingStatus>();
    }
}
