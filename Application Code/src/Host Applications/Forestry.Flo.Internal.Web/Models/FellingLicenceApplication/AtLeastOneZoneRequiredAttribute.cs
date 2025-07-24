using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;

public class AtLeastOneZoneRequiredAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        var model = (LarchCheckModel)validationContext.ObjectInstance;
        if (!model.Zone1 && !model.Zone2 && !model.Zone3)
        {
            return new ValidationResult("At least one zone must be selected", new[] { nameof(LarchCheckModel.Zone1) });
        }
        return ValidationResult.Success;
    }
}
