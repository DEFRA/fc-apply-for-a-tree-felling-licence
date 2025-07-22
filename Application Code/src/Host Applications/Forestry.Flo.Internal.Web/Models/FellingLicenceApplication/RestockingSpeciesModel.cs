using Forestry.Flo.Services.FellingLicenceApplications.Entities;

namespace Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;

public class RestockingSpeciesModel
{
    /// <summary>
    /// Gets or sets the species Id
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the species code
    /// </summary>
    public string Species { get; set; }

    /// <summary>
    /// Gets or sets the species name
    /// </summary>
    public string SpeciesName { get; set; }

    /// <summary>
    /// Gets or sets the percentage
    /// </summary>
    public int? Percentage { get; set; }

}