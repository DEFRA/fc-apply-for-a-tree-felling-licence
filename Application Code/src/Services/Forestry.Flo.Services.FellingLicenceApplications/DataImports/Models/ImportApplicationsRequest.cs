using Forestry.Flo.Services.PropertyProfiles.DataImports;

namespace Forestry.Flo.Services.FellingLicenceApplications.DataImports.Models;

/// <summary>
/// Model class for a request to import applications into the FLOv2 system.
/// </summary>
public class ImportApplicationsRequest
{
    /// <summary>
    /// Gets and sets the ID of the woodland owner the data being imported is for.
    /// </summary>
    public Guid WoodlandOwnerId { get; set; }

    /// <summary>
    /// Gets and sets the user account ID of the user performing the data import.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets and sets the ID of the agency that the user performing the import belongs to.
    /// </summary>
    public Guid AgencyId { get; set; }

    /// <summary>
    /// Gets and sets a value indicating whether the user performing the import is an FC user.
    /// </summary>
    public bool IsFcUser { get; set; }

    /// <summary>
    /// Gets and sets a list of <see cref="ApplicationSource"/> records representing applications to import.
    /// </summary>
    public List<ApplicationSource> ApplicationRecords { get; set; }
    
    /// <summary>
    /// Gets and sets a list of <see cref="ProposedFellingSource"/> records representing felling details to import.
    /// </summary>
    public List<ProposedFellingSource> FellingRecords { get; set; }
    
    /// <summary>
    /// Gets and sets a list of <see cref="ProposedRestockingSource"/> records representing restocking details to import.
    /// </summary>
    public List<ProposedRestockingSource> RestockingRecords { get; set; }

    /// <summary>
    /// Gets and sets a list of properties within FLOv2 that the application import files relate to.
    /// </summary>
    public IEnumerable<PropertyIds> PropertyIds { get; set; } = new List<PropertyIds>();
}