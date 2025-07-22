using CSharpFunctionalExtensions;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;

namespace Forestry.Flo.Services.FellingLicenceApplications.Services;

/// <summary>
/// Defines the contract for a service that updates a felling licence application following submissions.
/// </summary>
public interface ISubmitFellingLicenceService
{
    /// <summary>
    /// Auto assigns a woodland officer to an application based on location.
    /// </summary>
    /// <param name="applicationId">The id of the application to auto assign the woodland officer to.</param>
    /// <param name="externalApplicantId">The id of the applicant submitting the application.</param>
    /// <param name="linkToApplication">A link to the internal application for woodland officer.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Result"/> containing an <see cref="AutoAssignWoRecord"/> detailing assigned and unassigned users for notifications.</returns>
    Task<Result<AutoAssignWoRecord>> AutoAssignWoodlandOfficerAsync(
        Guid applicationId,
        Guid externalApplicantId,
        string linkToApplication,
        CancellationToken cancellationToken);

    /// <summary>
    /// Calculates and sets the centre point of a property profile.
    /// </summary>
    /// <param name="applicationId">The id of the application to calculate the centre point for.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <param name="compartments">The Compartments to calculate</param>
    /// <returns>Returns a string value of the centre point</returns>
    Task<Result<string>> CalculateCentrePointForApplicationAsync(Guid applicationId,
        List<string> compartments,
        CancellationToken cancellationToken);


    /// <summary>
    /// Takes the serialized value of the centre point and converts it into a OS grid.
    /// </summary>
    /// <param name="centrePointString">The serialized centre point </param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The OS Grid reference</returns>
    Task<Result<string>> CalculateOSGridAsync(string centrePointString,
        CancellationToken cancellationToken);

    /// <summary>
    /// Takes the serialized value of the centre point and gets the <see cref="ConfiguredFcArea"/>> for where the center point sits.
    /// </summary>
    /// <param name="centrePointString">The serialized centre point.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The <see cref="ConfiguredFcArea"/> for the Felling licence's centre point.</returns>
    Task<Result<ConfiguredFcArea>> GetConfiguredFcAreaAsync(string centrePointString,
        CancellationToken cancellationToken);
}