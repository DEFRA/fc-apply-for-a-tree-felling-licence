using FluentValidation;
using Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;

namespace Forestry.Flo.External.Web.Services.Validation;

public class LarchFlyoverValidator : AbstractValidator<LarchFlyoverModel>
{
    public LarchFlyoverValidator()
    {
        RuleFor(d => d.FlyoverDate)
            .SetValidator(new DatePartValidator("Flyover Date")!)
            .DependentRules(() =>
            {
                RuleFor(d => d.FlyoverDate)
                    .Must((m, d) => d.CalculateDate() >= m.SubmissionDate)
                    .When(m => m.FlyoverDate != null && new DatePartValidator().Validate(m.FlyoverDate).IsValid)
            .WithMessage(m => $"Flyover date cannot exist before the submission date ({m.SubmissionDate:dd/MM/yyyy})");
            });

        RuleFor(d => d.FlightObservations)
            .NotEmpty()
            .WithMessage("Flight observations must not be empty.")
            .MaximumLength(250)
            .WithMessage("Flight observations must not exceed 250 characters.");
    }
}