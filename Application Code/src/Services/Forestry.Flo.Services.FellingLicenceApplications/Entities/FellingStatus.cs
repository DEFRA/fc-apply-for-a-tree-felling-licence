namespace Forestry.Flo.Services.FellingLicenceApplications.Entities
{
    public class FellingStatus
    {
        public Guid Id { get; set; }
        public bool? Status { get; set; }
        public List<RestockingCompartmentStatus> RestockingCompartmentStatuses { get; set; } = new List<RestockingCompartmentStatus>();
    }
}
