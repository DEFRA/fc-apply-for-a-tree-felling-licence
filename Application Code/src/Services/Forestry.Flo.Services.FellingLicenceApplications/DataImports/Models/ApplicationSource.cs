namespace Forestry.Flo.Services.FellingLicenceApplications.DataImports.Models;

/// <summary>
/// Model class representing an application as defined in a data import source file.
/// </summary>
public class ApplicationSource
{
    /// <summary>
    /// Gets and sets the unique identifier for the application within the import file set.
    /// </summary>
    public int ApplicationId { get; set; }

    /// <summary>
    /// Gets and sets the name of the property within FLOv2 that this application relates to.
    /// </summary>
    public string Flov2PropertyName { get; set; }

    /// <summary>
    /// Gets and sets the proposed felling start date for the application.
    /// </summary>
    public DateOnly? ProposedFellingStart { get; set; }

    /// <summary>
    /// Gets and sets the proposed felling end date for the application.
    /// </summary>
    public DateOnly? ProposedFellingEnd { get; set; }

    /// <summary>
    /// Gets and sets any proposed measures to safeguard protected species or habitats as part of the application.
    /// </summary>
    public string? Measures { get; set; }

    /// <summary>
    /// Gets and sets optional extra details about the proposed timings for the application.
    /// </summary>
    public string? ProposedTiming { get; set; }
}