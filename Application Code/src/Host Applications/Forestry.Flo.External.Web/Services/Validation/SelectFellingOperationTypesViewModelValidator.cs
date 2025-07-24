using FluentValidation;
using Forestry.Flo.External.Web.Models.FellingLicenceApplication;

namespace Forestry.Flo.External.Web.Services.Validation
{
    public class SelectFellingOperationTypesViewModelValidator : AbstractValidator<SelectFellingOperationTypesViewModel>
    {
        public SelectFellingOperationTypesViewModelValidator()
        {
            RuleFor(m => m.OperationTypes)
                .Must(m => m.Count > 0)
                .WithMessage("Select at least one option for this compartment");
        }
    }
}
