using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.Infrastructure;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.Common.User;
using Microsoft.Extensions.Logging;
using AuditEvent = Forestry.Flo.Services.Common.Migrations.AuditEvent;

namespace Forestry.Flo.Services.Common.Services;

public class ActivityFeedItemProvider : IActivityFeedItemProvider
{
    private readonly ILogger<ActivityFeedItemProvider> _logger;
    private readonly IEnumerable<IActivityFeedService> _activityFeedServices;
    private readonly IAuditService<ActivityFeedItemProvider> _audit;
    private readonly RequestContext _requestContext;

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

    public async Task<Result<IList<ActivityFeedItemModel>>> RetrieveAllRelevantActivityFeedItemsAsync(
        ActivityFeedItemProviderModel providerModel,
        ActorType requestingActorType,
        CancellationToken cancellation)
    {
        var activityFeedItemModels = new List<ActivityFeedItemModel>();

        foreach (var activityFeedService in _activityFeedServices)
        {
            var types = providerModel.ItemTypes.Where(x => activityFeedService.SupportedItemTypes().Contains(x)).ToArray();
            if (!types.Any()) continue;

            var (_, isFailure, value, error) = await activityFeedService.RetrieveActivityFeedItemsAsync(providerModel, requestingActorType, cancellation);

            if (isFailure)
            {
                _logger.LogError("Unable to retrieve activity feed items for id: {id}, error: {error}, service type: {service}", providerModel.FellingLicenceId, error, activityFeedService.GetType());
                await _audit.PublishAuditEventAsync(new Auditing.AuditEvent(
                    AuditEvents.RetrieveActivityFeedItemsFailure,
                    providerModel.FellingLicenceId,
                    null, _requestContext,
                    new { Error = error} ),
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

