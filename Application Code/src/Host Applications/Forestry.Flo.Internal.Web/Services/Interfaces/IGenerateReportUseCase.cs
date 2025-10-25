using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Models.Reports;
using Microsoft.AspNetCore.Mvc;

namespace Forestry.Flo.Internal.Web.Services.Interfaces;

public interface IGenerateReportUseCase
{
    /// <summary>
    /// Generates a report based on the provided view model and user.
    /// </summary>
    /// <param name="viewModel">The report request view model.</param>
    /// <param name="user">The internal user requesting the report.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing an IActionResult for the generated report.</returns>
    Task<Result<IActionResult>> GenerateReportAsync(
        ReportRequestViewModel viewModel,
        InternalUser user,
        CancellationToken cancellationToken);

    /// <summary>
    /// Gets a reference model for the report request.
    /// </summary>
    /// <param name="user">The internal user requesting the reference model.</param>
    /// <param name="addDefaultDates">Whether to add default dates to the model.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the populated ReportRequestViewModel.</returns>
    Task<Result<ReportRequestViewModel>> GetReferenceModelAsync(
        InternalUser user,
        bool addDefaultDates = false,
        CancellationToken cancellationToken = default);
}