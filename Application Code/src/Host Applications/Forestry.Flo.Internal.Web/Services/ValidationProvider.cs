using FluentValidation.Results;
using Forestry.Flo.Internal.Web.Models.UserAccount;
using Forestry.Flo.Internal.Web.Services.Interfaces;
using Forestry.Flo.Internal.Web.Services.Validation;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Forestry.Flo.Internal.Web.Services
{
    public class ValidationProvider : IValidationProvider
    {
        public List<ValidationFailure> ValidateSection(UserRegistrationDetailsModel model, string modelSection, ModelStateDictionary modelState)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));
            if (string.IsNullOrEmpty(modelSection)) throw new ArgumentException(nameof(modelSection));

            var validator = new UserAccountModelValidator();
            var result = validator.Validate(model);
            return result.Errors;
        }

        public List<ValidationFailure> ValidateExternalUserAccountSection(AmendExternalUserAccountModel model, string modelSection, ModelStateDictionary modelState)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));
            if (string.IsNullOrEmpty(modelSection)) throw new ArgumentException(nameof(modelSection));

            var validator = new ExternalUserAccountModelValidator();
            var result = validator.Validate(model);
            return result.Errors;
        }
    }
}
