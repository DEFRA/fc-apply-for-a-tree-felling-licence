using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.External.Web.Models.UserAccount;

public class AccountTermsAndConditionsModel : IValidatableObject
{
    /// <summary>
    /// Gets and Sets whether the user is agreeing to the terms and conditions.
    /// </summary>
    [DisplayName("Accept Terms and Conditions?")]
    public bool AcceptsTermsAndConditions { get; set; }

    /// <summary>
    /// Gets and sets whether the user is agreeing to the privacy policy.
    /// </summary>
    [DisplayName("Accept Privacy Policy?")]
    public bool AcceptsPrivacyPolicy { get; set; }

    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!AcceptsTermsAndConditions && !AcceptsPrivacyPolicy)
        {
            yield return new ValidationResult(
                "You must agree to the terms and conditions and privacy policy to continue.",
                new[] { nameof(AcceptsTermsAndConditions), nameof(AcceptsPrivacyPolicy) });
        }
    }
}