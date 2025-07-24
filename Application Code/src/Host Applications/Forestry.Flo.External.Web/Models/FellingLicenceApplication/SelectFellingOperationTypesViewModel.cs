using Forestry.Flo.External.Web.Models.Compartment;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;

namespace Forestry.Flo.External.Web.Models.FellingLicenceApplication
{
    public class SelectFellingOperationTypesViewModel: ApplicationStepBase, IApplicationWithBreadcrumbsViewModel
    {
        public Guid ApplicationId { get; set; }
        public Guid FellingCompartmentId { get; set; }
        public List<FellingOperationType> OperationTypes { get; set; } = new List<FellingOperationType>();
        public bool IsClearFellingSelected { get; set; }
        public bool IsFellingOfCoppiceSelected { get; set; }
        public bool IsFellingIndividualTreesSelected { get; set; }
        public bool IsRegenerationFellingSelected { get; set; }
        public bool IsThinningSelected { get; set; }
        public IReadOnlyCollection<CompartmentModel> Compartments { get; set; } = null!;
        public string? CompartmentName { get; set; }
        public string? ApplicationReference { get; set; }
        public string? GIS { get; set; }
        public BreadcrumbsModel? Breadcrumbs { get; set; }
        public FellingLicenceApplicationModel Application { get; set; } = null!;
        public string TaskName => "Select felling options";
    }
}
