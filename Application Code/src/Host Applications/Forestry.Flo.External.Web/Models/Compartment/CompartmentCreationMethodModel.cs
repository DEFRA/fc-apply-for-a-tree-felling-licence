using Forestry.Flo.Services.FellingLicenceApplications.Entities;

namespace Forestry.Flo.External.Web.Models.Compartment
{
    public class CompartmentCreationMethodModel
    {
        public CreationMethod CreationMethod { get; set; }

        public Guid? CompartmentId { get; set; }
        public Guid PropertyProfileId { get; set; }
        public Guid? ApplicationId { get; set; }
        public Guid WoodlandOwnerId { get; set; }
        public Guid? AgencyId { get; set; }
        public bool IsForRestockingCompartmentSelection { get; set; }
        public string? FellingCompartmentName { get; set; }
        public Guid? FellingCompartmentId { get; set; }
        public Guid? ProposedFellingDetailsId { get; set; }
        public FellingOperationType? FellingOperationType { get; set; }

    }
}
