using FluentValidation;
using Forestry.Flo.External.Web.Models.FellingLicenceApplication;

namespace Forestry.Flo.External.Web.Services.Validation
{
    public class SelectRestockingOptionsViewModelValidator : AbstractValidator<SelectRestockingOptionsViewModel>
    {
        public SelectRestockingOptionsViewModelValidator()
        {
            RuleFor(m => m.RestockingOptions)
                .Must(m => m.Count > 0)
                .WithMessage("Select at least one option for this compartment");
        }
    }
}
