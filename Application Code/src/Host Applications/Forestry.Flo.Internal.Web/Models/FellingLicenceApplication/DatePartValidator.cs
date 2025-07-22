using FluentValidation;
using Forestry.Flo.Services.Common.Models;

namespace Forestry.Flo.External.Web.Services.Validation;

public class DatePartValidator: AbstractValidator<DatePart>
{
    public DatePartValidator(string? parentName = null)
    {
        RuleFor(d => d.Year)
            .Must(IsValidYear)
            .WithMessage($"{parentName} year is invalid");
        RuleFor(d => d.Month)
            .Must(IsValidMonth)
            .WithMessage($"{parentName} month is invalid");
        RuleFor(d => d.Day)
            .Must((date, day) => day > 0
                                 && day <=  (IsValidMonth(date.Month) && IsValidYear(date.Year) ? DateTime.DaysInMonth(date.Year, date.Month) : 31))
            .WithMessage($"{parentName} day is invalid");
    }

    private static bool IsValidMonth(int month) => month is >= 1 and <= 12;

    private static bool IsValidYear(int year) => year >= DateTime.MinValue.Year && year <= DateTime.MaxValue.Year;
}