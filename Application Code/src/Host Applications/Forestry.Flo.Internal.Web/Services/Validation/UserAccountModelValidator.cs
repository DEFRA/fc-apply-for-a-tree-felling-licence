using FluentValidation;
using Forestry.Flo.Internal.Web.Models.UserAccount;
using Forestry.Flo.Services.Common.User;

namespace Forestry.Flo.Internal.Web.Services.Validation
{
    public class UserAccountModelValidator : AbstractValidator<UserRegistrationDetailsModel>
    {
        public UserAccountModelValidator()
        {
            RuleFor(a => a.RequestedAccountTypeOther)
                .Must(x => x is not null)
                .When(x => x.RequestedAccountType == AccountTypeInternal.Other)
                .WithMessage("A Job title other must be selected when Job title is Other");
        }
    }
}
