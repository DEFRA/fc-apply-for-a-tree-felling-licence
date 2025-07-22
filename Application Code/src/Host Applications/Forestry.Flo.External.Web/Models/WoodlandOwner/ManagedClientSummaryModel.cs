using Forestry.Flo.External.Web.Models.AgentAuthorityForm;

namespace Forestry.Flo.External.Web.Models.WoodlandOwner
{
    public class ManagedClientSummaryModel
    {
        public ManageWoodlandOwnerDetailsModel ManageWoodlandOwnerDetails { get; set; }

        public IEnumerable<ManagedPropertySummary> Properties { get; set; }

        public Guid? AgencyId { get; set; }

        public Guid AgentAuthorityId { get; set; }

        public AgentAuthorityFormDocumentItemModel CurrentAgentAuthorityForm { get; set; }
    }

    public class ManagedPropertySummary
    {
        public string Name { get; set; }
        public int NoOfCompartments { get; set; }
        public Guid PropertyId { get; set; }
    }
}
