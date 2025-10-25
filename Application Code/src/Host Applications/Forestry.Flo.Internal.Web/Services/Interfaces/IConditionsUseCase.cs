using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Models.WoodlandOfficerReview;
using Forestry.Flo.Services.ConditionsBuilder.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview;

namespace Forestry.Flo.Internal.Web.Services.Interfaces;

/// <summary>
/// Defines the contract for a use case that manages conditions for felling licence applications.
/// </summary>
public interface IConditionsUseCase
{
    /// <summary>
    /// Retrieves the conditions and conditional status for a given application.
    /// </summary>
    /// <param name="applicationId">The ID of the application.</param>
    /// <param name="user">The internal user performing the operation.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A result containing the <see cref="ConditionsViewModel"/>.</returns>
    Task<Result<ConditionsViewModel>> GetConditionsAsync(
        Guid applicationId,
        InternalUser user,
        CancellationToken cancellationToken);

    /// <summary>
    /// Stores the updated conditional status for an application.
    /// </summary>
    /// <param name="applicationId">The ID of the application.</param>
    /// <param name="user">The internal user performing the operation.</param>
    /// <param name="model">The conditions status model.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> SaveConditionStatusAsync(
        Guid applicationId,
        InternalUser user,
        ConditionsStatusModel model,
        CancellationToken cancellationToken);

    /// <summary>
    /// Generates conditions for an application based on confirmed felling and restocking details.
    /// </summary>
    /// <param name="applicationId">The ID of the application.</param>
    /// <param name="user">The internal user performing the operation.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A result containing the <see cref="ConditionsResponse"/>.</returns>
    Task<Result<ConditionsResponse>> GenerateConditionsAsync(
        Guid applicationId,
        InternalUser user,
        CancellationToken cancellationToken);

    /// <summary>
    /// Saves the specified conditions for an application.
    /// </summary>
    /// <param name="applicationId">The ID of the application.</param>
    /// <param name="user">The internal user performing the operation.</param>
    /// <param name="conditions">The list of calculated conditions.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> SaveConditionsAsync(
        Guid applicationId,
        InternalUser user,
        List<CalculatedCondition> conditions,
        CancellationToken cancellationToken);

    /// <summary>
    /// Sends the conditions to the applicant for a given application.
    /// </summary>
    /// <param name="applicationId">The ID of the application.</param>
    /// <param name="user">The internal user performing the operation.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> SendConditionsToApplicantAsync(
        Guid applicationId,
        InternalUser user,
        CancellationToken cancellationToken);
}