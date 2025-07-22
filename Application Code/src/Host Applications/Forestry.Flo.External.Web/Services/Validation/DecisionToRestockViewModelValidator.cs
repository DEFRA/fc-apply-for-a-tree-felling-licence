using FluentValidation;
using Forestry.Flo.External.Web.Models.FellingLicenceApplication;
namespace Forestry.Flo.External.Web.Services.Validation
{
    public class DecisionToRestockViewModelValidator : AbstractValidator<DecisionToRestockViewModel>
    {
        public DecisionToRestockViewModelValidator()
        {
            RuleFor(d => d.Reason)
            .NotEmpty()
            .When(m => !m.IsRestockSelected)
            .WithMessage("Enter a reason for not restocking");
        }
    }
}
