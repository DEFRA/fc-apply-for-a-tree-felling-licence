namespace Forestry.Flo.Services.FellingLicenceApplications.Models;

public class SpeciesModel
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
    public double? Percentage { get; set; }
}