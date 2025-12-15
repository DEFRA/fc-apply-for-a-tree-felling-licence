using FluentValidation;
using Forestry.Flo.External.Web.Models.FellingLicenceApplication.PawsDesignations;

namespace Forestry.Flo.External.Web.Services.Validation;

public class PawsDesignationsViewModelValidator : AbstractValidator<PawsDesignationsViewModel>
{
    public PawsDesignationsViewModelValidator()
    {
        RuleFor(x => x.CompartmentDesignation.ProportionBeforeFelling)
            .NotNull()
            .WithMessage("Select the proportion of native tree species that make up this compartment");

        RuleFor(x => x.CompartmentDesignation.ProportionAfterFelling)
            .NotNull()
            .WithMessage("Select the proportion of native tree species that will be left in the compartment after felling");

        RuleFor(x => x.CompartmentDesignation.IsRestoringCompartment)
            .NotNull()
            .WithMessage("Select whether you are restoring this compartment");

        RuleFor(x => x.CompartmentDesignation.RestorationDetails)
            .NotNull()
            .When(x => x.CompartmentDesignation.IsRestoringCompartment is true)
            .WithMessage("Enter why this compartment is being restored");
    }
    
}