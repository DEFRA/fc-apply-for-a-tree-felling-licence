namespace Forestry.Flo.Services.Common.Extensions;

public static class DateTimeExtensions
{
    /// <summary>
    /// Returns a formatted string of the Date Time
    /// </summary>
    /// <param name="inputValue"></param>
    /// <returns>{numericDay}{suffix} {month} {year}, Example: 19th June 2022</returns>
    public static string CreateFormattedDate(this DateTime inputValue)
    {
        var month = inputValue.ToString("MMMM");
        var year = inputValue.ToString("yyyy");
        var numericDay = inputValue.Day;

        var suffix = numericDay switch
        {
            1 => "st",
            21 => "st",
            31 => "st",
            2 => "nd",
            22 => "nd",
            3 => "rd",
            23 => "rd",
            _ => "th"
        };

        return $"{numericDay}{suffix} {month} {year}";
    }

    public static DateTime FirstDayOfWeek(this DateTime dt)
    {
        var culture = System.Threading.Thread.CurrentThread.CurrentCulture;
        var diff = dt.DayOfWeek - culture.DateTimeFormat.FirstDayOfWeek;

        if (diff < 0)
        {
            diff += 7;
        }

        return dt.AddDays(-diff).Date;
    }

    public static DateTime LastDayOfWeek(this DateTime dt) =>
        dt.FirstDayOfWeek().AddDays(6);

    public static DateTime FirstDayOfMonth(this DateTime dt) =>
        new DateTime(dt.Year, dt.Month, 1);

    public static DateTime LastDayOfMonth(this DateTime dt) =>
        dt.FirstDayOfMonth().AddMonths(1).AddDays(-1);

    public static DateTime FirstDayOfNextMonth(this DateTime dt) =>
        dt.FirstDayOfMonth().AddMonths(1);
}