using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;

public class ConfirmInspectionLogRequiredAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        var model = (LarchCheckModel)validationContext.ObjectInstance;
        if (model.Zone1 && !model.ConfirmInspectionLog)
        {
            return new ValidationResult("You must confirm that the relevant compartments have been recorded in the tree health inspection log when Zone 1 is selected", 
                new[] { nameof(LarchCheckModel.ConfirmInspectionLog) });
        }
        return ValidationResult.Success;
    }
}
