using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common.Infrastructure;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.Common.User;
using Microsoft.Extensions.Logging;

namespace Forestry.Flo.Services.Common.Services;

public interface IActivityFeedItemProvider
{
    /// <summary>
    /// Collates a list of filtered <see cref="ActivityFeedItemModel"/> using a list of <see cref="IActivityFeedService"/>.
    /// </summary>
    /// <param name="providerModel">An instance of <see cref="ActivityFeedItemProviderModel"/> containing data related to the felling licence application.</param>
    /// <param name="requestingActorType"> The type of actor requesting the activity feed items.</param>
    /// <param name="cancellation">A cancellation token.</param>
    /// <returns>A <see cref="Result"/> indicating the outcome of the operation containing a list of <see cref="ActivityFeedItemModel"/>.</returns>
    Task<Result<IList<ActivityFeedItemModel>>> RetrieveAllRelevantActivityFeedItemsAsync(
        ActivityFeedItemProviderModel providerModel,
        ActorType requestingActorType,
        CancellationToken cancellation);
}

