using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common.MassTransit.Messages;

namespace Forestry.Flo.External.Web.Services.Interfaces;

/// <summary>
/// Defines the contract for a use case that checks if a Felling Licence Application requires PAWS data.
/// </summary>
public interface ICheckForPawsRequirementUseCase
{
    /// <summary>
    /// Checks if the application compartments intersect PAWS areas and therefore
    /// requires extra data input from the applicant.
    /// </summary>
    /// <param name="message">A <see cref="PawsRequirementCheckMessage"/> request.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Result"/> struct indicating the outcome of the operation.</returns>
    Task<Result> CheckAndUpdateApplicationForPaws(
        PawsRequirementCheckMessage message,
        CancellationToken cancellationToken);
}