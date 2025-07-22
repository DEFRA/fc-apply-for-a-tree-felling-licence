namespace Forestry.Flo.Services.FellingLicenceApplications.Models;

public class TreeSpeciesModel
{
    public string Code { get; set; }
    public string Name { get; set; }
    public SpeciesType  SpeciesType { get; set; }
    public bool IsNative { get; set; }
    public bool IsLarch { get; set; }
}