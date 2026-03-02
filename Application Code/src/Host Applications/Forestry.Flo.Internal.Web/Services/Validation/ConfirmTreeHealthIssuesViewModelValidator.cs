using FluentValidation;
using Forestry.Flo.Internal.Web.Models.WoodlandOfficerReview;

namespace Forestry.Flo.Internal.Web.Services.Validation;

public class ConfirmTreeHealthIssuesViewModelValidator: AbstractValidator<ConfirmTreeHealthIssuesViewModel>
{
    public ConfirmTreeHealthIssuesViewModelValidator()
    {
        RuleFor(x => x.IsTreeHealthReasonToExpedite)
            .NotNull()
            .WithMessage("Select whether there are tree health or public safety reasons to expedite this application");
    }
}