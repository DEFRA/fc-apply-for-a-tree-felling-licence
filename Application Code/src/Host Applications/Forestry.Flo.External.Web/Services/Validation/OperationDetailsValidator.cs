using FluentValidation;
using Forestry.Flo.External.Web.Models.FellingLicenceApplication;

namespace Forestry.Flo.External.Web.Services.Validation;

public class OperationDetailsValidator: AbstractValidator<OperationDetailsModel>
{
    public OperationDetailsValidator()
    {
        RuleFor(d => d.ProposedFellingStart)
            .SetValidator(new DatePartValidator("proposed start date")!)
            .DependentRules(() =>
            {
                var now = DateTime.Now;
                RuleFor(d => d.ProposedFellingStart)
                    .Must( d => d.CalculateDate() >= new DateTime(now.Year, now.Month, now.Day))
                    .WithMessage("Enter a proposed start date in the future");
            });

        RuleFor(d => d.ProposedFellingEnd)
            .SetValidator(new DatePartValidator("proposed completion date")!)
            .DependentRules(() =>
            {
                RuleFor(d => d.ProposedFellingEnd)
                    .Must((m, d) => d.CalculateDate() >= m.ProposedFellingStart.CalculateDate())
                    .When(m => new DatePartValidator().Validate(m.ProposedFellingStart).IsValid)
                    .WithMessage("Enter a proposed completion date after the proposed start date");
            });

        RuleFor(d => d.DateReceived)
            .SetValidator(new DatePartValidator("application received date")!)
            .DependentRules(() =>
            {
                RuleFor(d => d.DateReceived)
                    .Must(x => x!.CalculateDate().Date <= DateTime.UtcNow.Date)
                    .WithMessage("Enter an application received date on or before today's date");
            })
            .When(d =>
                d.DisplayDateReceived
                && d.DateReceived is not null
                && d.DateReceived!.IsPopulated());
    }
}