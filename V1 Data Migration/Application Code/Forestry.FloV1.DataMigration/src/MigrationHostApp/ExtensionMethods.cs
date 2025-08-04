namespace MigrationHostApp;

public static class ExtensionMethods
{
    public static string Clean(string value)
    {
        return !string.IsNullOrEmpty(value) ? value.Trim() : value;
    }

    public static string FormatTimeSpanForDisplay(this TimeSpan ts)
    {
        return $"{ts.Hours:00}h:{ts.Minutes:00}m:{ts.Seconds:00}s.{ts.Milliseconds / 10:00}ms";
    }
}