namespace Forestry.Flo.Services.FellingLicenceApplications.Entities
{
    public class RestockingCompartmentStatus
    {
        public Guid CompartmentId { get; set; }
        public bool? Status { get; set; }
        public List<RestockingStatus> RestockingStatuses { get; set; } = new List<RestockingStatus>();
    }
}
