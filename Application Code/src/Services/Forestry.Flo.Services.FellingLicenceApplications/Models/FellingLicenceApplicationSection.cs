using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.Services.FellingLicenceApplications.Models;

public enum FellingLicenceApplicationSection
{
    [Description("Select compartments")]
    SelectedCompartments,
    [Description("Check for constraints")]
    ConstraintCheck,
    [Description("Type of felling")]
    OperationDetails,
    [Description("Felling and restocking details")]
    FellingAndRestockingDetails,
    [Description("Supporting documentation")]
    SupportingDocumentation,
    [Description("Terms and conditions")]
    FlaTermsAndConditionsViewModel,
    [Description("Environmental Impact Assessment")]
    EnvironmentalImpactAssessment
}