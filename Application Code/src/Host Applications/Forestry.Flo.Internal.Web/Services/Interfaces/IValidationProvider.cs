using FluentValidation.Results;
using Forestry.Flo.Internal.Web.Models.UserAccount;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Forestry.Flo.Internal.Web.Services.Interfaces
{
    /// <summary>
    /// Provides methods for validating user account models and external user account models.
    /// </summary>
    public interface IValidationProvider
    {
        /// <summary>
        /// Validates a section of the <see cref="UserRegistrationDetailsModel"/>.
        /// </summary>
        /// <param name="model">The user registration details model to validate.</param>
        /// <param name="modelSection">The section of the model to validate.</param>
        /// <param name="modelState">The model state dictionary for tracking validation errors.</param>
        /// <returns>A list of <see cref="ValidationFailure"/> objects representing validation errors.</returns>
        List<ValidationFailure> ValidateSection(UserRegistrationDetailsModel model, string modelSection, ModelStateDictionary modelState);

        /// <summary>
        /// Validates a section of the <see cref="AmendExternalUserAccountModel"/>.
        /// </summary>
        /// <param name="model">The external user account model to validate.</param>
        /// <param name="modelSection">The section of the model to validate.</param>
        /// <param name="modelState">The model state dictionary for tracking validation errors.</param>
        /// <returns>A list of <see cref="ValidationFailure"/> objects representing validation errors.</returns>
        List<ValidationFailure> ValidateExternalUserAccountSection(AmendExternalUserAccountModel model, string modelSection, ModelStateDictionary modelState);
    }
}
