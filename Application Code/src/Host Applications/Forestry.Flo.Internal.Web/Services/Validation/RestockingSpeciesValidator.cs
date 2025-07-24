using FluentValidation;
using Forestry.Flo.Internal.Web.Models.WoodlandOfficerReview;

namespace Forestry.Flo.Internal.Web.Services.Validation;

public class RestockingSpeciesValidator : AbstractValidator<ConfirmedRestockingSpeciesModel>
{
    public RestockingSpeciesValidator(string? compartmentName = null)
    {
        //IMPORTANT - all error messages must start with the compartment name as "Compartment {x.CompartmentName} - etc etc" in order
        //for the error sorting in the controller to function - when adding a new error please make sure you follow this pattern!

        RuleFor(v => v.Percentage)
            .NotNull()
            .WithMessage(s => $"Compartment {compartmentName} - Percentage must be provided for all restocking species")
            .DependentRules(() => RuleFor(m => m.Percentage)
                .Must(m => (double) m! is > 0.00 and <= 100.00)
                .WithMessage(s => $"Compartment {compartmentName} - Percentage must be greater than zero and less than or equal to 100% for restocking species {s.Species}"));
    }
}