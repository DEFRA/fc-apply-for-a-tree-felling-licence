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
/// Defines the contract for a service that updates a felling licence application to Delete.
/// </summary>
public interface IDeleteFellingLicenceService
{
    /// <summary>
    /// Deletes the application and returns the result.
    /// </summary>
    /// <param name="applicationId">The id of the application to delete.</param>
    /// <param name="userAccessModel">The user access model.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Result"/> depending on the success of failure is deleting an application.</returns>
    Task<Result> DeleteDraftApplicationAsync(
        Guid applicationId,
        UserAccessModel userAccessModel,
        CancellationToken cancellationToken);


    /// <summary>
    /// Hard deletes supporting documentation from an application using the Document parsed.
    /// </summary>
    /// <param name="applicationId">The id of the application to hard delete the document from.</param>
    /// <param name="document">The document to hard delete.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The result of the operation.</returns>
    Task<Result> PermanentlyRemoveDocumentAsync(
        Guid applicationId,
        Document document,
        CancellationToken cancellationToken);
}
