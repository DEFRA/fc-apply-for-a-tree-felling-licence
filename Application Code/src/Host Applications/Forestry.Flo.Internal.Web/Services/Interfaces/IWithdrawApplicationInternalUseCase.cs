using CSharpFunctionalExtensions;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;

namespace Forestry.Flo.Internal.Web.Services.Interfaces;

/// <summary>
/// Contract definition for a use case class for performing the withdrawal of an application from
/// the internal system, via automated tasks.
/// </summary>
public interface IWithdrawApplicationInternalUseCase
{
    /// <summary>
    /// Processes the application withdrawal, storing the provided reasons and triggering the required notifications
    /// and audit entries for the process.
    /// </summary>
    /// <param name="applicationId">The id of the application being withdrawn.</param>
    /// <param name="withdrawalReason">A <see cref="WithdrawalReason"/> to store against the application. Only one is provided
    /// as this method is intended for nightly tasks. If withdrawal is made available in the internal interface, this should
    /// be expanded to allow for multiple reasons and a string parameter for "Other" details.</param>
    /// <param name="linkToApplication">A link to the application in the internal interface to include in notifications.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Result"/> struct indicating the outcome.</returns>
    Task<Result> WithdrawApplicationAsync(
        Guid applicationId,
        WithdrawalReason withdrawalReason,
        string linkToApplication,
        CancellationToken cancellationToken);
}