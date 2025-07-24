using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;

namespace Forestry.Flo.Services.FellingLicenceApplications.Services;

/// <summary>
/// Contract for a service that updates the area code, central point and OS grid reference for the selected compartments of <see cref="FellingLicenceApplication"/>./>
/// </summary>
public interface IUpdateCentrePoint
{
    /// <summary>
    /// Updates the Area Code, Centre Point and OS grid reference for a given application.
    /// </summary>
    /// <param name="applicationId">The identifier for the application.</param>
    /// <param name="userAccessModel">User Access model</param>
    /// <param name="areaCode">The area code of the felling licence application</param>
    /// <param name="administrativeRegion">The administrative region of the felling licence application</param>
    /// <param name="centrePoint">The centre point value to populate the submitted property profile with.</param>
    /// <param name="osGridReference">The OS grid reference value to populate the submitted property profile with.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A result indicating whether the entity has been successfully updated.</returns>
    Task<Result> UpdateCentrePointAsync(
        Guid applicationId,
        UserAccessModel userAccessModel,
        string areaCode,
        string administrativeRegion,
        string centrePoint,
        string osGridReference,
        CancellationToken cancellationToken);
}