using FluentValidation;
using Forestry.Flo.Internal.Web.Models.WoodlandOfficerReview;

namespace Forestry.Flo.Internal.Web.Services.Validation;

public class ConfirmTreeHealthIssuesViewModelValidator: AbstractValidator<ConfirmTreeHealthIssuesViewModel>
{
    public ConfirmTreeHealthIssuesViewModelValidator()
    {
        RuleFor(x => x.Confirmed)
            .NotNull()
            .WithMessage("Select whether the submitted tree health answers are correct to the best of your understanding");
    }
}