using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.FellingLicenceApplications.DataImports.Models;

namespace Forestry.Flo.Services.FellingLicenceApplications.DataImports;

/// <summary>
/// Defines the contract for a class that imports applications from an external
/// data source into the FLOv2 system.
/// </summary>
public interface IImportApplications
{
    /// <summary>
    /// Run the data import for a list of appointments and related data.
    /// </summary>
    /// <param name="request">A populated <see cref="ImportApplicationsRequest"/> model containing all the data to import.</param>
    /// <param name="requestContext">The <see cref="RequestContext"/> for the import request.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The number of applications successfully imported, or an error.</returns>
    public Task<Result<Dictionary<Guid, string>>> RunDataImportAsync(
        ImportApplicationsRequest request, 
        RequestContext requestContext,
        CancellationToken cancellationToken);
}