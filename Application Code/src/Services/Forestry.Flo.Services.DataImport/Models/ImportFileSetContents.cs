using Forestry.Flo.Services.FellingLicenceApplications.DataImports.Models;

namespace Forestry.Flo.Services.DataImport.Models;

/// <summary>
/// Model class representing the data that has been read from a set of import CSV files.
/// </summary>
public class ImportFileSetContents
{
    /// <summary>
    /// Gets and sets the collection of application records read from the import files.
    /// </summary>
    public List<ApplicationSource>? ApplicationSourceRecords { get; set; }

    /// <summary>
    /// Gets and sets the collection of proposed felling operation records read from the import files.
    /// </summary>
    public List<ProposedFellingSource>? ProposedFellingSourceRecords { get; set; }

    /// <summary>
    /// Gets and sets the collection of proposed restocking operation records read from the import files.
    /// </summary>
    public List<ProposedRestockingSource>? ProposedRestockingSourceRecords { get; set; }
}