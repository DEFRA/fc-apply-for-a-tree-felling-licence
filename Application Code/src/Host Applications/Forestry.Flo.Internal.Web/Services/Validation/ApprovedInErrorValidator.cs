using FluentValidation;
using Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;

namespace Forestry.Flo.Internal.Web.Services.Validation;

public class ApprovedInErrorValidator : AbstractValidator<ApprovedInErrorViewModel>
{
 public ApprovedInErrorValidator()
 {
 // At least one reason must be selected
 RuleFor(x => x.ReasonExpiryDate)
 .Must((model, _) => model.ReasonExpiryDate || model.ReasonSupplementaryPoints || model.ReasonOther)
 .WithMessage("Select at least one reason");

 // If Other is selected, a CaseNote is required
 RuleFor(x => x.CaseNote)
 .NotEmpty()
 .When(x => x.ReasonOther)
 .WithMessage("Enter details for Other");

 // If Other is selected, Expiry date must not be selected
 RuleFor(x => x.ReasonExpiryDate)
 .Equal(false)
 .When(x => x.ReasonOther)
 .WithMessage("Do not select Expiry date when Other is selected");

 // If Other is selected, Supplementary points must not be selected
 RuleFor(x => x.ReasonSupplementaryPoints)
 .Equal(false)
 .When(x => x.ReasonOther)
 .WithMessage("Do not select Supplementary points when Other is selected");
 }
}
