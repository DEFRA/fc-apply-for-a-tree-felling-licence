using FluentValidation;
using Forestry.Flo.External.Web.Models.UserAccount;

namespace Forestry.Flo.External.Web.Services.Validation;

public class AccountPersonNameModelValidator : AbstractValidator<AccountPersonNameModel>
{
    public AccountPersonNameModelValidator()
    {

    }
}