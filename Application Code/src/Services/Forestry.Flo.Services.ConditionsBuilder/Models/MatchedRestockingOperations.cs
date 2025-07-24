namespace Forestry.Flo.Services.ConditionsBuilder.Models;

/// <summary>
/// Model class for a collection of matched compartments
/// </summary>
internal class MatchedRestockingOperations
{
    internal List<RestockingOperationDetails> RestockingOperations { get; set; }

    internal string RestockingCompartmentNames()
    {
        var names = RestockingOperations
            .DistinctBy(x => $"{x.RestockingCompartmentNumber}, {x.RestockingSubcompartmentName}")
            .OrderBy(x => x.RestockingCompartmentNumber)
            .ThenBy(x => x.RestockingSubcompartmentName)
            .Select(x => x.RestockingCompartmentNumber)
            .ToList();

        var prefix = names.Count == 1 ? "compartment " : "compartments ";
        return prefix + string.Join(", ", names);
    }

    internal string FellingCompartmentNames() {
        var names = RestockingOperations
            .DistinctBy(x => $"{x.FellingCompartmentNumber}, {x.FellingSubcompartmentName}")
            .OrderBy(x => x.FellingCompartmentNumber)
            .ThenBy(x => x.FellingSubcompartmentName)
            .Select(x => x.FellingCompartmentNumber)
            .ToList();

        var prefix = names.Count == 1 ? "compartment " : "compartments ";
        return prefix + string.Join(", ", names);
    }

    internal MatchedRestockingOperations(List<RestockingOperationDetails> compartments)
    {
        RestockingOperations = compartments;
    }
}