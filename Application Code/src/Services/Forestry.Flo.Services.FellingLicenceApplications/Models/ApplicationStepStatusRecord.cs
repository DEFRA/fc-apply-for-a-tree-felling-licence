using Forestry.Flo.Services.FellingLicenceApplications.Entities;

namespace Forestry.Flo.Services.FellingLicenceApplications.Models;

public record ApplicationStepStatusRecord
{
    public bool? AgentAuthorityFormComplete { get; init; } = null;

    public bool? SelectedCompartmentsComplete { get; init; } = null;

    public bool? OperationDetailsComplete { get; init; } = null;

    public IEnumerable<CompartmentFellingRestockingStatus> FellingAndRestockingDetailsComplete { get; init; } = Array.Empty<CompartmentFellingRestockingStatus>();

    public bool? ConstraintsCheckComplete { get; init; } = null;

    public bool? TermsAndConditionsComplete { get; init; } = null;

    public bool? SupportingDocumentationComplete { get; init; } = null;

    public bool? EnvironmentalImpactAssessmentComplete { get; init; } = null;

    public bool? PawsCheckComplete { get; init; } = null;
}