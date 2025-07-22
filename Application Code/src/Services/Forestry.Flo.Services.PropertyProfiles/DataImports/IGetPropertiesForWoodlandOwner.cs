using System.Security.Principal;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common.Models;

namespace Forestry.Flo.Services.PropertyProfiles.DataImports;

/// <summary>
/// Contract fore a service that retrieves property and compartment information for a woodland owner
/// to use in an applications data import.
/// </summary>
public interface IGetPropertiesForWoodlandOwner
{
    /// <summary>
    /// Retrieves the required property information for a particular woodland owner to be used
    /// in performing applications data import.
    /// </summary>
    /// <param name="userAccessModel">The user access model to verify user access to the woodland owner.</param>
    /// <param name="woodlandOwnerId">The woodland owner id to retrieve properties for.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns></returns>
    Task<Result<IEnumerable<PropertyIds>>> GetPropertiesForDataImport(
        UserAccessModel userAccessModel,
        Guid woodlandOwnerId,
        CancellationToken cancellationToken);
}