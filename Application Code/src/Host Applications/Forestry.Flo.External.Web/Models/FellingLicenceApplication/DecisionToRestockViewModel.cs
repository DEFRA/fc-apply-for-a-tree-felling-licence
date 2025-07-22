using Forestry.Flo.Services.FellingLicenceApplications.Entities;

namespace Forestry.Flo.External.Web.Models.FellingLicenceApplication
{
    public class DecisionToRestockViewModel : ApplicationStepBase, IApplicationWithBreadcrumbsViewModel
    {
        public Guid FellingCompartmentId { get; set; }
        public Guid ProposedFellingDetailsId { get; set; }
        public FellingOperationType OperationType { get; set; }
        public string FellingCompartmentName { get; set; } = string.Empty;
        public bool IsRestockSelected { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string? ApplicationReference { get; set; }
        public BreadcrumbsModel? Breadcrumbs { get; set; }

        public string TaskName => "Restock for felling type?";
    }
}
