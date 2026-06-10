namespace Forestry.Flo.Services.FellingLicenceApplications.Models;

public class SpeciesModel
{
    public readonly static string OpenSpace = "OPEN_SPACE";

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

    /// <summary>
    /// Gets whether this SpeciesModel instance is for open space.
    /// </summary>
    public bool IsOpenSpace => string.Equals(OpenSpace, Species, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Initialises a new instance of the SpeciesModel class with the OpenSpace species code and name, and an optional percentage.
    /// </summary>
    /// <param name="percentage">The percentage of the restocking area that will be left open.</param>
    /// <returns>A new instance of <see cref="SpeciesModel"/> representing open space.</returns>
    public static SpeciesModel OpenSpaceSpecies(double? percentage = null) => new()
    {
        Id = Guid.Empty,
        Species = OpenSpace,
        SpeciesName = "Area to be left as open space",
        Percentage = percentage
    };
}