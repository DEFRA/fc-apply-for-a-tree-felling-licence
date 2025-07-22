using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.Common.User;

namespace Forestry.Flo.Services.Common.Services;


/// <summary>
/// Contract for services that look up activity feed items.
/// </summary>
public interface IActivityFeedService
{
    /// <summary>
    /// Convert case notes, assignee histories and status histories into activity feed items.
    /// </summary>
    /// <param name="providerModel">An instance of <see cref="ActivityFeedItemProviderModel"/> containing data related to the felling licence application.</param>
    /// <param name="requestingActorType"> The type of actor requesting the activity feed items.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Result"/> indicating the outcome of the operation containing a list of <see cref="ActivityFeedItemModel"/>.</returns>
    Task<Result<IList<ActivityFeedItemModel>>> RetrieveActivityFeedItemsAsync(
        ActivityFeedItemProviderModel providerModel,
        ActorType requestingActorType,
        CancellationToken cancellationToken);

    /// <summary>
    /// Determines which activity feed item types are supported by an implementation.
    /// </summary>
    /// <returns>An array of <see cref="ActivityFeedItemType"/> that are supported by an implementation.</returns>
    ActivityFeedItemType[] SupportedItemTypes();
}

