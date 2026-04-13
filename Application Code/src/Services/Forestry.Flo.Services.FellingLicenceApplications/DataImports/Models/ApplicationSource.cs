using CsvHelper.Configuration.Attributes;

namespace Forestry.Flo.Services.FellingLicenceApplications.DataImports.Models;

/// <summary>
/// Model class representing an application as defined in a data import source file.
/// </summary>
public class ApplicationSource
{
    /// <summary>
    /// Gets and sets the unique identifier for the application within the import file set.
    /// </summary>
    [Name("applicationid")]
    public int ApplicationId { get; set; }

    /// <summary>
    /// Gets and sets the name of the property within FLOv2 that this application relates to.
    /// </summary>
    [Name("flov2propertyname")]
    public string Flov2PropertyName { get; set; }

    /// <summary>
    /// Gets and sets the proposed felling start date for the application.
    /// </summary>
    [Name("proposedfellingstart")]
    public DateOnly? ProposedFellingStart { get; set; }

    /// <summary>
    /// Gets and sets the proposed felling end date for the application.
    /// </summary>
    [Name("proposedfellingend")]
    public DateOnly? ProposedFellingEnd { get; set; }
}