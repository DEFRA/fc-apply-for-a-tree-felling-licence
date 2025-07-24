using FluentValidation;
using Forestry.Flo.Services.Common.Models;

namespace Forestry.Flo.External.Web.Services.Validation;

public class DatePartValidator: AbstractValidator<DatePart>
{
    public DatePartValidator(string? parentName = null)
    {
        RuleFor(d => d)
            .NotNull()
            .Must(x => x.IsEmpty() is false)
            .WithMessage($"Enter a {parentName} including day, month and year, for example 27 3 2026");
        RuleFor(d => d.Year)
            .Must(IsValidYear)
            .When(x => x.IsEmpty() is false)
            .WithMessage($"Enter a valid year for {parentName}, for example 2026");
        RuleFor(d => d.Month)
            .Must(IsValidMonth)
            .When(x => x.IsEmpty() is false)
            .WithMessage($"Enter a valid month between 1 and 12 for {parentName}, for example 3");
        RuleFor(d => d.Day)
            .Must((date, day) => day > 0
                                 && day <=  (IsValidMonth(date.Month) && IsValidYear(date.Year) ? DateTime.DaysInMonth(date.Year, date.Month) : 31))
            .When(x => x.IsEmpty() is false)
            .WithMessage($"Enter a valid day between 1 and 31 for {parentName}, depending on the month"); ;
    }
    private static bool IsValidMonth(int month) => month is >= 1 and <= 12;

    private static bool IsValidYear(int year) => year >= DateTime.MinValue.Year && year <= DateTime.MaxValue.Year;
}