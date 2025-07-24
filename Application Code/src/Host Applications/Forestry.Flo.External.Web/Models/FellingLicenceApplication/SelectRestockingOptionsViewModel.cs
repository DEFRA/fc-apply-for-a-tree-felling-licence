using Forestry.Flo.Services.FellingLicenceApplications.Entities;

namespace Forestry.Flo.External.Web.Models.FellingLicenceApplication
{
    public class SelectRestockingOptionsViewModel : ApplicationStepBase, IApplicationWithBreadcrumbsViewModel
    {
        public Guid FellingCompartmentId { get; set; }
        public Guid RestockingCompartmentId { get; set; }
        public Guid ProposedFellingDetailsId { get; set; }
        public FellingOperationType FellingOperationType { get; set; }
        public string FellingCompartmentName { get; set; } = string.Empty;
        public string RestockingCompartmentName { get; set; } = string.Empty;
        public string? GIS { get; set; }
        public bool RestockAlternativeArea { get; set; }

        public bool IsCoppiceRegrowthAllowed { get; set; }
        public bool IsCoppiceRegrowthSelected { get; set; }
        public bool IsCreateOpenSpaceAllowed { get; set; }
        public bool IsCreateOpenSpaceSelected { get; set; }
        public bool IsIndividualTreesAllowed { get; set; }
        public bool IsIndividualTreesSelected { get; set; }
        public bool IsNaturalRegenerationAllowed { get; set; }
        public bool IsNaturalRegenerationSelected { get; set; }
        public bool IsReplantFelledAreaAllowed { get; set; }
        public bool IsReplantFelledAreaSelected { get; set; }

        public bool IsIndividualTreesInAlternativeAreaAllowed { get; set; }
        public bool IsIndividualTreesInAlternativeAreaSelected { get; set; }
        public bool IsPlantingInAlternativeAreaAllowed { get; set; }
        public bool IsPlantingInAlternativeAreaSelected { get; set; }
        public bool IsNaturalColonisationAllowed { get; set; }
        public bool IsNaturalColonisationSelected { get; set; }
        public List<TypeOfProposal> RestockingOptions { get; set; } = new List<TypeOfProposal>();

        public string? ApplicationReference { get; set; }
        public BreadcrumbsModel? Breadcrumbs { get; set; }

        public string TaskName => "Select restocking options";
    }
}
