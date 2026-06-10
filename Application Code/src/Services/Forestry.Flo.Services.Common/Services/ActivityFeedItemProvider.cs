using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.Common.User;
using Microsoft.Extensions.Logging;

namespace Forestry.Flo.Services.Common.Services;

/// <summary>
/// Implementation of <see cref="IActivityFeedItemProvider"/> which coordinates calls to multiple <see cref="IActivityFeedService"/>
/// to retrieve activity feed items for a given felling licence, and applies any necessary filtering to the results before returning them.
/// </summary>
public class ActivityFeedItemProvider : IActivityFeedItemProvider
{
    private readonly ILogger<ActivityFeedItemProvider> _logger;
    private readonly IEnumerable<IActivityFeedService> _activityFeedServices;
    private readonly IAuditService<ActivityFeedItemProvider> _audit;
    private readonly RequestContext _requestContext;

    /// <summary>
    /// Creates a new instance of <see cref="ActivityFeedItemProvider"/>.
    /// </summary>
    /// <param name="activityFeedServices">A collection of <see cref="IActivityFeedService"/> to retrieve activity feed items.</param>
    /// <param name="logger">A logging instance.</param>
    /// <param name="audit">An auditing instance.</param>
    /// <param name="requestContext">The request context.</param>
    public ActivityFeedItemProvider(
        IEnumerable<IActivityFeedService> activityFeedServices,
        ILogger<ActivityFeedItemProvider> logger,
        IAuditService<ActivityFeedItemProvider> audit,
        RequestContext requestContext)
    {
        _logger = Guard.Against.Null(logger);
        _activityFeedServices = Guard.Against.Null(activityFeedServices);
        _audit = Guard.Against.Null(audit);
        _requestContext = Guard.Against.Null(requestContext);
    }

    /// <inheritdoc/>
    public async Task<Result<IList<ActivityFeedItemModel>>> RetrieveAllRelevantActivityFeedItemsAsync(
        ActivityFeedItemProviderModel providerModel,
        ActorType requestingActorType,
        CancellationToken cancellation)
    {
        var activityFeedItemModels = new List<ActivityFeedItemModel>();

        foreach (var activityFeedService in _activityFeedServices)
        {
            var types = providerModel.ItemTypes?
                .Where(x => activityFeedService.SupportedItemTypes().Contains(x))
                .ToArray() ?? [];

            if (types.Length == 0) continue;

            var (_, isFailure, value, error) = await activityFeedService.RetrieveActivityFeedItemsAsync(providerModel, requestingActorType, cancellation);

            if (isFailure)
            {
                _logger.LogError("Unable to retrieve activity feed items for id: {id}, error: {error}, service type: {service}", providerModel.FellingLicenceId, error, activityFeedService.GetType());
                await _audit.PublishAuditEventAsync(new Auditing.AuditEvent(
                    AuditEvents.RetrieveActivityFeedItemsFailure,
                    providerModel.FellingLicenceId,
                    null, _requestContext,
                    new { Error = error } ),
                    cancellation);
                return Result.Failure<IList<ActivityFeedItemModel>>(error);
            }

            activityFeedItemModels.AddRange(value);
        }

        if (providerModel.VisibleToApplicant.HasValue)
            activityFeedItemModels = activityFeedItemModels.FindAll(x => x.VisibleToApplicant == providerModel.VisibleToApplicant.Value);
        if (providerModel.VisibleToConsultee.HasValue)
            activityFeedItemModels = activityFeedItemModels.FindAll(x => x.VisibleToConsultee == providerModel.VisibleToConsultee.Value);

        await _audit.PublishAuditEventAsync(new Auditing.AuditEvent(
                AuditEvents.RetrieveActivityFeedItems,
                providerModel.FellingLicenceId,
                null, _requestContext),
            cancellation);

        return Result.Success<IList<ActivityFeedItemModel>>(activityFeedItemModels.OrderByDescending(x => x.CreatedTimestamp).ToList());
    }
}

