using Forestry.Flo.Services.FellingLicenceApplications.Entities;

namespace Forestry.Flo.Services.FellingLicenceApplications.Services;

/// <summary>
/// Static class to hold constants related to Cricket Bat Willow application detection.
/// </summary>
public static class CricketBatWillowConstants
{
    /// <summary>
    /// List of species codes that are considered Cricket Bat Willow; only if all species in an application
    /// are in this list is the application considered a CBW application.
    /// </summary>
    public static readonly HashSet<string> CricketBatWillowSpecies = ["CBW"];

    /// <summary>
    /// List of felling operation types that are valid for Cricket Bat Willow applications; only if all felling operations in an application
    /// are in this list is the application considered a CBW application.
    /// </summary>
    public static readonly HashSet<FellingOperationType> CricketBatWillowFellingTypes =
        [FellingOperationType.ClearFelling, FellingOperationType.FellingIndividualTrees];

    /// <summary>
    /// List of restocking proposal types that are valid for Cricket Bat Willow applications; only if all proposed restocking operations
    /// in an application are in this list is the application considered a CBW application.
    /// </summary>
    public static readonly HashSet<TypeOfProposal> CricketBatWillowRestockingTypes =
        [TypeOfProposal.RestockWithIndividualTrees, TypeOfProposal.ReplantTheFelledArea];
}