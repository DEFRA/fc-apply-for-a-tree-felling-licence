using FluentValidation;
using Forestry.Flo.Internal.Web.Models.WoodlandOfficerReview;
using Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview;

namespace Forestry.Flo.Internal.Web.Services.Validation;

public class UpdateDesignationsViewModelValidator : AbstractValidator<UpdateDesignationsViewModel>
{
    public UpdateDesignationsViewModelValidator()
    {
        RuleFor(m => m.CompartmentDesignations)
            .Must(m =>
                ((m.Sssi || m.Sacs || m.Spa || m.Ramsar || m.Sbi || m.Other || m.Paws) && !m.None)
                || m is { Sssi: false, Sacs: false, Spa: false, Ramsar: false, Sbi: false, Other: false, Paws:false, None: true })
            .WithMessage("Select at least one designation or 'None'");
       
        When(m => m.CompartmentDesignations.Other, () =>
        {
            RuleFor(m => m.CompartmentDesignations.OtherDesignationDetails)
                .NotEmpty()
                .WithMessage("Enter the other designation details when 'Other designation' is selected");
        });

        When(m => m.CompartmentDesignations.Paws, () =>
        {
            RuleFor(m => m.CompartmentDesignations.ProportionBeforeFelling)
                .NotNull()
                .WithMessage("Select the proportion of native tree species that make up this compartment");

            RuleFor(m => m.CompartmentDesignations.ProportionAfterFelling)
                .NotNull()
                .WithMessage("Select the proportion of native tree species that will be left in the compartment after felling");
        });
    }
}