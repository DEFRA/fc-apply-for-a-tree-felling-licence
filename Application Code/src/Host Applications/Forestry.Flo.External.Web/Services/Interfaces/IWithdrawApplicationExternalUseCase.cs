using CSharpFunctionalExtensions;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;

namespace Forestry.Flo.External.Web.Services.Interfaces;

/// <summary>
/// Contract definition for a use case class for performing the withdrawal of an application from
/// the external applicant interface.
/// </summary>
public interface IWithdrawApplicationExternalUseCase
{
    /// <summary>
    /// Processes the application withdrawal, storing the provided reasons and triggering the required notifications
    /// and audit entries for the process.
    /// </summary>
    /// <param name="applicationId">The id of the application being withdrawn.</param>
    /// <param name="user">Then user withdrawing the application.</param>
    /// <param name="withdrawalReasons">The provided list of <see cref="WithdrawalReason"/> values.</param>
    /// <param name="withdrawalReasonsOtherDetails">The optional extra detail if <see cref="WithdrawalReason.Other"/>
    /// is selected as a reason.</param>
    /// <param name="linkToApplication">A link to the application in the external interface to include in notifications.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Result"/> struct indicating the outcome.</returns>
    Task<Result> WithdrawApplicationAsync(
        Guid applicationId,
        ExternalApplicant user,
        List<WithdrawalReason> withdrawalReasons,
        string? withdrawalReasonsOtherDetails,
        string linkToApplication,
        CancellationToken cancellationToken);
}