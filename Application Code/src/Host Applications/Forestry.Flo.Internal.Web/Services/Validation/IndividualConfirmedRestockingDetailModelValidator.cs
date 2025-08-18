using FluentValidation;
using Forestry.Flo.Internal.Web.Models.WoodlandOfficerReview;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;

namespace Forestry.Flo.Internal.Web.Services.Validation;

public class IndividualConfirmedRestockingDetailModelValidator : AbstractValidator<IndividualConfirmedRestockingDetailModel>
{
    public IndividualConfirmedRestockingDetailModelValidator()
    {
        //IMPORTANT - all error messages must start with the compartment name as "Compartment {x.CompartmentName} - etc etc" in order
        //for the error sorting in the controller to function - when adding a new error please make sure you follow this pattern!

        // felling validation
        RuleFor(m => m.ConfirmedRestockingDetails)
            .SetValidator(x => new ConfirmedRestockingDetailValidatorWithRestockDecision(x));
    }
}