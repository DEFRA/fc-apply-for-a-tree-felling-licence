using Forestry.Flo.External.Web.Models.FellingLicenceApplication.EnvironmentalImpactAssessment;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Microsoft.AspNetCore.Mvc;

namespace Forestry.Flo.External.Web.Models.FellingLicenceApplication;

public class FellingLicenceApplicationModel 
{
    private readonly Dictionary<FellingLicenceApplicationSection, IApplicationStep> _steps = new();

    public FellingLicenceApplicationModel()
    {
        _steps.Add(FellingLicenceApplicationSection.SelectedCompartments, new SelectedCompartmentsModel());
        _steps.Add(FellingLicenceApplicationSection.ConstraintCheck, new ConstraintCheckModel());
        _steps.Add(FellingLicenceApplicationSection.OperationDetails, new OperationDetailsModel());
        _steps.Add(FellingLicenceApplicationSection.FellingAndRestockingDetails, new FellingAndRestockingDetails());
        _steps.Add(FellingLicenceApplicationSection.SupportingDocumentation, new SupportingDocumentationModel());
        _steps.Add(FellingLicenceApplicationSection.FlaTermsAndConditionsViewModel, new FlaTermsAndConditionsViewModel());
        _steps.Add(FellingLicenceApplicationSection.EnvironmentalImpactAssessment, new EnvironmentalImpactAssessmentViewModel());
    }
    
    public bool IsComplete => _steps.Values
        .Where(x => x.StepRequiredForApplication)
        .All(s => s.Status == ApplicationStepStatus.Completed);
    
    public int StepsCount => _steps.Values.Count(x => x.StepRequiredForApplication);
    
    public int CompletedStepsCount => _steps.Values
        .Where(x => x.StepRequiredForApplication)
        .Count(s => s.Status == ApplicationStepStatus.Completed);
    
    public FellingLicenceApplicationSummary ApplicationSummary { get; set; } = null!;

    public SelectedCompartmentsModel SelectedCompartments
    {
        get => (_steps[FellingLicenceApplicationSection.SelectedCompartments] as SelectedCompartmentsModel)!;
        set =>_steps[FellingLicenceApplicationSection.SelectedCompartments] = value;
    }
    
    public OperationDetailsModel OperationDetails
    {
        get => (_steps[FellingLicenceApplicationSection.OperationDetails] as OperationDetailsModel)!;
        set =>_steps[FellingLicenceApplicationSection.OperationDetails] = value;
    }

    public FellingAndRestockingDetails FellingAndRestockingDetails
    {
        get => (_steps[FellingLicenceApplicationSection.FellingAndRestockingDetails] as FellingAndRestockingDetails)!;
        set =>_steps[FellingLicenceApplicationSection.FellingAndRestockingDetails] = value;
    }

    public ConstraintCheckModel ConstraintCheck
    {
        get => (_steps[FellingLicenceApplicationSection.ConstraintCheck] as ConstraintCheckModel)!;
        set => _steps[FellingLicenceApplicationSection.ConstraintCheck] = value;
    }
    
    public FlaTermsAndConditionsViewModel FlaTermsAndConditionsViewModel
    {
        get => (_steps[FellingLicenceApplicationSection.FlaTermsAndConditionsViewModel] as FlaTermsAndConditionsViewModel)!;
        set => _steps[FellingLicenceApplicationSection.FlaTermsAndConditionsViewModel] = value;
    }

    public SupportingDocumentationModel SupportingDocumentation
    {
        get => (_steps[FellingLicenceApplicationSection.SupportingDocumentation] as SupportingDocumentationModel)!;
        set =>_steps[FellingLicenceApplicationSection.SupportingDocumentation] = value;
    }

    public EnvironmentalImpactAssessmentViewModel EnvironmentalImpactAssessment
    {
        get => (_steps[FellingLicenceApplicationSection.EnvironmentalImpactAssessment] as EnvironmentalImpactAssessmentViewModel)!;
        set => _steps[FellingLicenceApplicationSection.EnvironmentalImpactAssessment] = value;
    }

    [HiddenInput]
    public Guid ApplicationId { get; set;}

    [HiddenInput]
    public Guid WoodlandOwnerId { get; set; }

    [HiddenInput]
    public Guid? AgencyId { get; set; }

    public BreadcrumbsModel? Breadcrumbs { get; set; }

    public bool HasCaseNotes { get; set; }

    public bool IsCBWapplication
    {
        get
        {
            bool areAllSpeciesCBW = FellingAndRestockingDetails.DetailsList
                .All(detail =>
                    detail.FellingDetails.All(fellingDetail =>
                        fellingDetail.Species.All(species => species.Key == "CBW")
                    ) &&
                    detail.FellingDetails.All(fellingDetail =>
                        fellingDetail.ProposedRestockingDetails.All(restockingDetail =>
                            restockingDetail.Species.All(species => species.Key == "CBW")
                        )
                    )
                );

            bool areAllFellingIndividualTrees = FellingAndRestockingDetails.DetailsList
                .All(detail => detail.FellingDetails
                    .All(fellingDetail => fellingDetail.OperationType == Flo.Services.FellingLicenceApplications.Entities.FellingOperationType.FellingIndividualTrees));

            bool areAllRestockingIndividualTrees = FellingAndRestockingDetails.DetailsList
                .All(detail => detail.FellingDetails
                    .All(fellingDetail => fellingDetail.ProposedRestockingDetails
                        .All(restockingDetail => restockingDetail.RestockingProposal == Flo.Services.FellingLicenceApplications.Entities.TypeOfProposal.RestockWithIndividualTrees)));

            return areAllSpeciesCBW && areAllFellingIndividualTrees && areAllRestockingIndividualTrees;
        }
    }

    public int TotalNumberOfTreesRestocking
    {
        get
        {
            return FellingAndRestockingDetails.DetailsList?
                .Sum(detail => detail.FellingDetails
                    .Sum(fellingDetail => fellingDetail.ProposedRestockingDetails
                        .Sum(restockingDetail => restockingDetail.NumberOfTrees))) ?? 0;
        }
    }
}