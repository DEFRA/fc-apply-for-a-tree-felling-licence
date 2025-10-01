namespace Forestry.Flo.Services.Common
{
    public static class DateTimeDisplay
    {
        /// <summary>
        /// Converts UTC date time to UK date string with format 'dd/MMMM/yyyy'.
        /// </summary>
        /// <param name="value">A <see cref="DateTime"/> in UTC.</param>
        /// <returns>A string representation of the converted <see cref="DateTime"/>.</returns>
        public static string GetDateDisplayString(DateTime? value)
        {
            return value.HasValue
                ? ConvertToUkTime(value.Value).ToString("dd MMMM yyyy")
                : " - ";
        }

        /// <summary>
        /// Converts UTC date time to UK time string with format 'HH:mm'.
        /// </summary>
        /// <param name="value">A <see cref="DateTime"/> in UTC.</param>
        /// <returns>A string representation of the converted <see cref="DateTime"/>.</returns>
        public static string GetTimeDisplayString(DateTime? value)
        {
            return value.HasValue
                ? ConvertToUkTime(value.Value).ToString("HH:mm")
                : " - ";
        }

        /// <summary>
        /// Converts UTC date time to UK date time string with format 'dd MMMM yyyy HH:mm'.
        /// </summary>
        /// <param name="value">A <see cref="DateTime"/> in UTC.</param>
        /// <param name="delimiter">A string delimiter between date and time.</param>
        /// <returns>A string representation of the converted <see cref="DateTime"/>.</returns>
        public static string GetDateTimeDisplayString(DateTime? value, string delimiter = " ")
        {
            return value.HasValue
                ? $"{ConvertToUkTime(value.Value):dd MMMM yyyy}{delimiter}{ConvertToUkTime(value.Value):HH:mm}"
                : " - ";
        }

        private static DateTime ConvertToUkTime(DateTime dateTime)
        {
            // this time zone supports daylight saving time 
            var timeZoneInfoUk = TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time");

            return dateTime.Kind == DateTimeKind.Utc
                ? TimeZoneInfo.ConvertTimeFromUtc(dateTime, timeZoneInfoUk)
                : TimeZoneInfo.ConvertTime(dateTime, timeZoneInfoUk);
        }
    }
}
