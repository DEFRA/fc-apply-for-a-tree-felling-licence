namespace Forestry.Flo.External.Web.Models.FellingLicenceApplication.PawsDesignations;

/// <summary>
/// Result of looking up data for PAWS redirection after saving a compartment's designations data.
/// </summary>
/// <param name="NextCompartmentDesignationsId">The id of the next compartment designations entity, if there is one.</param>
/// <param name="RequiresEia">A bool indicating whether this application requires an EIA check.</param>
public record PawsRedirectResult(
    Guid? NextCompartmentDesignationsId,
    bool? RequiresEia);