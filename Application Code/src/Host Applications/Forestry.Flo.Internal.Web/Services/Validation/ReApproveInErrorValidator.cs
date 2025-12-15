using FluentValidation;
using Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;

namespace Forestry.Flo.Internal.Web.Services.Validation;

public class ReApproveInErrorValidator : AbstractValidator<ReApproveInErrorViewModel>
{
    public ReApproveInErrorValidator()
    {
        // NewLicenceExpiryDate is required if ReasonExpiryDate was selected
        When(x => x.ApprovedInErrorViewModel != null && x.ApprovedInErrorViewModel.ReasonExpiryDate, () =>
        {
            RuleFor(x => x.NewLicenceExpiryDate)
                .NotNull()
                .WithMessage("Enter a new licence expiry date");

            // Validate individual date fields - these will catch 0 values
            RuleFor(x => x.NewLicenceExpiryDate!.Day)
                .GreaterThan(0)
                .When(x => x.NewLicenceExpiryDate != null)
                .WithMessage("Enter a day for the licence expiry date")
                .WithName("NewLicenceExpiryDate.Day");

            RuleFor(x => x.NewLicenceExpiryDate!.Month)
                .GreaterThan(0)
                .When(x => x.NewLicenceExpiryDate != null)
                .WithMessage("Enter a month for the licence expiry date")
                .WithName("NewLicenceExpiryDate.Month");

            RuleFor(x => x.NewLicenceExpiryDate!.Year)
                .GreaterThan(0)
                .When(x => x.NewLicenceExpiryDate != null)
                .WithMessage("Enter a year for the licence expiry date")
                .WithName("NewLicenceExpiryDate.Year");

            // Validate that the date is a valid calendar date
            // This must come after checking all fields are populated
            RuleFor(x => x.NewLicenceExpiryDate)
                .Must(BeAValidDate)
                .When(x => x.NewLicenceExpiryDate != null && 
                           x.NewLicenceExpiryDate.Day > 0 && 
                           x.NewLicenceExpiryDate.Month > 0 && 
                           x.NewLicenceExpiryDate.Year > 0)
                .WithMessage("Enter a valid licence expiry date");

            // Validate that the new expiry date is in the future
            // This should only run if the date is valid
            RuleFor(x => x.NewLicenceExpiryDate)
                .Must(BeInTheFuture)
                .When(x => x.NewLicenceExpiryDate != null && 
                           x.NewLicenceExpiryDate.Day > 0 && 
                           x.NewLicenceExpiryDate.Month > 0 && 
                           x.NewLicenceExpiryDate.Year > 0 && 
                           BeAValidDate(x.NewLicenceExpiryDate))
                .WithMessage("The licence expiry date must be in the future");

            // Validate that the new expiry date is within the next 100 years
            // This should only run if the date is valid and in the future
            RuleFor(x => x.NewLicenceExpiryDate)
                .Must(BeWithinNext100Years)
                .When(x => x.NewLicenceExpiryDate != null && 
                           x.NewLicenceExpiryDate.Day > 0 && 
                           x.NewLicenceExpiryDate.Month > 0 && 
                           x.NewLicenceExpiryDate.Year > 0 && 
                           BeAValidDate(x.NewLicenceExpiryDate) &&
                           BeInTheFuture(x.NewLicenceExpiryDate))
                .WithMessage("The licence expiry date must be within the next 100 years");

            // Validate ReasonExpiryDateText is required
            RuleFor(x => x.ApprovedInErrorViewModel!.ReasonExpiryDateText)
                .NotEmpty()
                .WithMessage("Enter a reason for changing the licence expiry date")
                .OverridePropertyName("ApprovedInErrorViewModel.ReasonExpiryDateText");
        });

        // NewSupplementaryPoints is required if ReasonSupplementaryPoints was selected
        When(x => x.ApprovedInErrorViewModel != null && x.ApprovedInErrorViewModel.ReasonSupplementaryPoints, () =>
        {
            RuleFor(x => x.ApprovedInErrorViewModel!.SupplementaryPointsText)
                .NotEmpty()
                .WithMessage("Enter new supplementary points")
                .OverridePropertyName("ApprovedInErrorViewModel.SupplementaryPointsText");
        });
    }

    private bool BeAValidDate(Forestry.Flo.Services.Common.Models.DatePart? datePart)
    {
        if (datePart == null)
            return false;

        // Check that all parts are greater than 0
        if (datePart.Day <= 0 || datePart.Month <= 0 || datePart.Year <= 0)
            return false;

        try
        {
            // Try to create a DateTime - this will throw if the date is invalid (e.g., Feb 30, invalid year/month/day combination)
            _ = new DateTime(datePart.Year, datePart.Month, datePart.Day);
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            // This catches invalid dates like Feb 30, month 13, day 32, etc.
            return false;
        }
        catch
        {
            return false;
        }
    }

    private bool BeInTheFuture(Forestry.Flo.Services.Common.Models.DatePart? datePart)
    {
        if (datePart == null)
            return false;

        // Check that all parts are greater than 0
        if (datePart.Day <= 0 || datePart.Month <= 0 || datePart.Year <= 0)
            return false;

        try
        {
            var date = new DateTime(datePart.Year, datePart.Month, datePart.Day, 0, 0, 0, DateTimeKind.Utc);
            var today = DateTime.UtcNow.Date;
            return date.Date > today;
        }
        catch (ArgumentOutOfRangeException)
        {
            // If we can't create a valid date, it's definitely not in the future
            return false;
        }
        catch
        {
            return false;
        }
    }

    private bool BeWithinNext100Years(Forestry.Flo.Services.Common.Models.DatePart? datePart)
    {
        if (datePart == null)
            return false;

        // Check that all parts are greater than 0
        if (datePart.Day <= 0 || datePart.Month <= 0 || datePart.Year <= 0)
            return false;

        try
        {
            var date = new DateTime(datePart.Year, datePart.Month, datePart.Day, 0, 0, 0, DateTimeKind.Utc);
            var today = DateTime.UtcNow.Date;
            var maxDate = today.AddYears(100);
            return date.Date <= maxDate;
        }
        catch (ArgumentOutOfRangeException)
        {
            // If we can't create a valid date, it's not within the valid range
            return false;
        }
        catch
        {
            return false;
        }
    }
}
