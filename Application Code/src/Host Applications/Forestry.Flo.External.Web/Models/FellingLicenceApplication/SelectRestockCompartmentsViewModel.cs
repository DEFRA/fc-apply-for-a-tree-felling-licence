using Forestry.Flo.Services.FellingLicenceApplications.Entities;

namespace Forestry.Flo.External.Web.Models.FellingLicenceApplication
{
    public class SelectRestockCompartmentsViewModel : ApplicationStepBase, IApplicationWithBreadcrumbsViewModel
    {
        public FellingOperationType OperationType { get; set; }
        public Guid CompartmentId { get; set; }
        public string CompartmentName { get; set; } = string.Empty;
        public string? ApplicationReference { get; set; }
        public BreadcrumbsModel? Breadcrumbs { get; set; }

        public string TaskName => "Select restocking compartments";
    }
}
