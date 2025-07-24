using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.Services.Common.Models;

public class DatePart
{
    /// <summary>
    /// A year part of the date.
    /// </summary>
    [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:.}")]
    public int Year { get; set; }
    /// <summary>
    /// A month part of the date.
    /// </summary>
    [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:.}")]
    public int Month { get; set; }
    /// <summary>
    /// A day part of the date.
    /// </summary>
    [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:.}")]
    public int Day { get; set; }

    public string? FieldName { get; set; }

    public DatePart()
    {

    }

    public DatePart(DateTime date, string fieldName)
    {
        Year = date.Year;
        Month = date.Month;
        Day = date.Day;
        FieldName = fieldName;
    }

    /// <summary>
    /// Calculates a date for the year, the month and the day.
    /// </summary>
    /// <returns>A date time calculated from the day, month and year.</returns>
    /// <remarks>
    /// As only the date portion of the date time is relevant, the time is set to 12:00:00 to avoid
    /// issues arising from converting the date to UTC.
    /// </remarks>
    public DateTime CalculateDate() => new(Year, Month, Day, 12, 0, 0);

    /// <summary>
    /// Determines whether a date part is empty.
    /// </summary>
    /// <returns>A flag indicating whether the date part is empty.</returns>
    public bool IsEmpty() =>
        Year is 0
        && Month is 0
        && Day is 0;

    /// <summary>
    /// Determines whether a date part is populated.
    /// </summary>
    /// <returns>A flag indicating whether the date part is populated.</returns>
    public bool IsPopulated() => (Year is 0 || Month is 0 || Day is 0) == false;
}