using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.Extensions;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.Gis.Interfaces;
using Forestry.Flo.Services.InternalUsers.Repositories;
using Forestry.Flo.Services.Notifications.Entities;
using Forestry.Flo.Services.Notifications.Models;
using Forestry.Flo.Services.Notifications.Services;
using Microsoft.Extensions.Logging;
using NodaTime;
using Forestry.Flo.Services.Gis.Models.Internal.MapObjects;
using Forestry.Flo.Services.InternalUsers.Entities.UserAccount;
using Newtonsoft.Json;
using System.Threading;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using CSharpFunctionalExtensions.ValueTasks;
using Forestry.Flo.Services.Common.Models;

namespace Forestry.Flo.Services.FellingLicenceApplications.Services;

/// <summary>
/// Defines the contract for a service that updates a felling licence application for withdrawal.
/// </summary>
public interface IWithdrawFellingLicenceService
{
    /// <summary>
    /// Withdraws the application and returns the result containing a list of internal users assigned to the application, or an empty IList if no internal user is assigned to it.
    /// </summary>
    /// <param name="applicationId">The id of the application to withdraw.</param>
    /// <param name="userAccessModel">The user access model used to check permission to the felling licence application.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Result"/> containing a list of Guid representing the users assigned to the application.</returns>
    Task<Result<IList<Guid>>> WithdrawApplication(
        Guid applicationId, 
        UserAccessModel userAccessModel, 
        CancellationToken cancellationToken);

    /// <summary>
    /// Removes the assignment of woodland officers of an application using the felling licence application id to identify the fla and the IList of users to be removed form their assignment.
    /// </summary>
    /// <param name="applicationId">The id of the application to identify the correct application.</param>
    /// <param name="internalUsers">The ids of users to remove assignment from the application.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Result"/> for the outcome of removing the assignment of the internal user to the application</returns>
    Task<Result> RemoveAssignedWoodlandOfficerAsync(
        Guid applicationId,
        IList<Guid> internalUsers,
        CancellationToken cancellationToken);

    /// <summary>
    /// Updates the <see cref="PublicRegister"/> entity for an application with the removed
    /// timestamp.
    /// </summary>
    /// <param name="applicationId">The id of the application to update.</param>
    /// <param name="userId">The optional id of the user making the update.</param>
    /// <param name="removedDateTime">The date and time that the application was removed from the public register.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Result"/> indicating the success or failure of the operation.</returns>
    Task<Result> UpdatePublicRegisterEntityToRemovedAsync(
        Guid applicationId,
        Guid? userId,
        DateTime removedDateTime,
        CancellationToken cancellationToken);
}
