using CSharpFunctionalExtensions;
using Forestry.Flo.Services.FellingLicenceApplications.Models.Reports;

namespace Forestry.Flo.Services.FellingLicenceApplications.Services;

/// <summary>
/// Defines the contract for a service that queries the system for the purpose of reporting
/// </summary>
public interface IReportQueryService
{
    /// <summary>
    /// Retrieves felling licence applications which match the supplied query model.
    /// </summary>
    /// <param name="query">Model holding the details of the query to be ran against the Felling licences data.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A populated <see cref="FellingLicenceApplicationsReportQueryResultModel"/> providing the data of the query which was ran.</returns>
    Task<Result<FellingLicenceApplicationsReportQueryResultModel>> QueryFellingLicenceApplicationsAsync(
        FellingLicenceApplicationsReportQuery query,
        CancellationToken cancellationToken);
}