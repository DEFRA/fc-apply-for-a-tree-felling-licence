using System.Globalization;
using FluentValidation;
using Forestry.Flo.Internal.Web.Models.Reports;

namespace Forestry.Flo.Internal.Web.Services.Validation;

public class FellingLicenceApplicationsReportRequestViewModelValidator : AbstractValidator<ReportRequestViewModel>
{
    public FellingLicenceApplicationsReportRequestViewModelValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        
        RuleFor(x => x)
            .Must(FromDateIsParseableToDate)
            .WithMessage("The specified FROM date is not a valid date.")
            .WithName("FromDay");

        RuleFor(x => x)
            .Must(ToDateIsParseableToDate)
            .WithMessage("The specified TO date is not a valid date.")
            .WithName("ToDay");

    }
    private static bool FromDateIsParseableToDate(ReportRequestViewModel viewModel)
    {
        return ParseableToDate(viewModel.FromYear, viewModel.FromMonth, viewModel.FromDay);
    }

    private static bool ToDateIsParseableToDate(ReportRequestViewModel viewModel)
    {
        return ParseableToDate(viewModel.ToYear, viewModel.ToMonth, viewModel.ToDay);
    }

    private static bool ParseableToDate(string? year, string? month, string? day)
    {
        if (string.IsNullOrEmpty(year) || string.IsNullOrEmpty(month) || string.IsNullOrEmpty(day))
        {
            return false;
        }

        var result = TryCreateDateFromParts(year, month, day);

        return result.HasValue;
    }
    
    private static DateTime? TryCreateDateFromParts(string? year, string? month, string? day)
    {
        if (string.IsNullOrEmpty(year) || string.IsNullOrEmpty(month) || string.IsNullOrEmpty(day))
        {
            return null;
        }

        if (DateTime.TryParseExact($"{year}-{month.PadLeft(2, '0')}-{day.PadLeft(2, '0')}", "yyyy-MM-dd",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
        {
            return result;
        }

        return null;
    }
}
