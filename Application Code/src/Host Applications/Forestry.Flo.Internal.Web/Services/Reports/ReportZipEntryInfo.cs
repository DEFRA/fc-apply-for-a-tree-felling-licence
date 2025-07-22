namespace Forestry.Flo.Internal.Web.Services.Reports;

/// <summary>
/// Class to hold information about a report, for addition into archive
/// </summary>
public class ReportZipEntryInfo
{
    /// <summary>
    /// Name of the report file, to be added to the archive.
    /// </summary>
    public string ReportName { get; set; }

    /// <summary>
    /// The stream containing the report data.
    /// </summary>
    public MemoryStream DataMemoryStream { get; set; }

}