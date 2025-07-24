using FluentValidation.Results;
using Forestry.Flo.External.Web.Models.AccountAdministration;
using Forestry.Flo.External.Web.Models.UserAccount;
using Forestry.Flo.External.Web.Services.Validation;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Forestry.Flo.External.Web.Services
{
    public class ValidationProvider
    {
        public List<ValidationFailure> ValidateSection(UserAccountModel model, string modelSection, ModelStateDictionary modelState)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));
            if (string.IsNullOrEmpty(modelSection)) throw new ArgumentException(nameof(modelSection));

            RemoveValidationForOtherSection(modelState, modelSection);

            var validator = new UserAccountModelValidator(modelSection);
            var result = validator.Validate(model);
            return result.Errors;
        }

        public List<ValidationFailure> ValidateAmendUserAccountSection(AmendExternalUserAccountModel model, string modelSection)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));
            if (string.IsNullOrEmpty(modelSection)) throw new ArgumentException(nameof(modelSection));

            var validator = new ExternalUserAccountModelValidator();
            var result = validator.Validate(model);
            return result.Errors;
        }

        private void RemoveValidationForOtherSection(ModelStateDictionary modelState, string modelSection)
        {
            var argument = string.Empty;

            switch (modelSection)
            {
                case nameof(UserTypeModel):
                    argument = "UserTypeModel";
                    break;
                case nameof(AccountPersonNameModel):
                    argument = "PersonName";
                    break;
                case nameof(AccountPersonContactModel):
                    argument = "PersonContactsDetails";
                    break;
                default:
                    return;
            }

            foreach (var key in modelState.Keys)
            {
                var keyNameParts = key.Split('.');

                if (keyNameParts.All(x => x != argument))
                {
                    modelState.Remove(key);
                }
            }
        }

    }
}
