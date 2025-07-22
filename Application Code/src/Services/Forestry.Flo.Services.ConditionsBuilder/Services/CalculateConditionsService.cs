using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.Extensions;
using Forestry.Flo.Services.ConditionsBuilder.Entities;
using Forestry.Flo.Services.ConditionsBuilder.Models;
using Forestry.Flo.Services.ConditionsBuilder.Repositories;
using Microsoft.Extensions.Logging;
using System;

namespace Forestry.Flo.Services.ConditionsBuilder.Services;

/// <summary>
/// Implementation of <see cref="ICalculateConditions"/>.
/// </summary>
public class CalculateConditionsService : ICalculateConditions
{
    private readonly IAuditService<CalculateConditionsService> _auditService;
    private readonly IConditionsBuilderRepository _repository;
    private readonly IEnumerable<IBuildCondition> _conditions;
    private readonly RequestContext _requestContext;
    private readonly ILogger<CalculateConditionsService> _logger;

    /// <summary>
    /// Creates a new instance of a <see cref="CalculateConditionsService"/>.
    /// </summary>
    /// <param name="repository">An <see cref="IConditionsBuilderRepository"/> implementation to store calculated conditions.</param>
    /// <param name="conditions">A set of <see cref="IBuildCondition"/> implementations that build the conditions.</param>
    /// <param name="auditService">An <see cref="IAuditService{T}"/> implementation to audit operation outcomes.</param>
    /// <param name="requestContext">The request context for the operation.</param>
    /// <param name="logger">An <see cref="ILogger"/> implementation to log out messages.</param>
    public CalculateConditionsService(
        IConditionsBuilderRepository repository,
        IEnumerable<IBuildCondition> conditions,
        IAuditService<CalculateConditionsService> auditService,
        RequestContext requestContext,
        ILogger<CalculateConditionsService> logger)
    {
        _repository = Guard.Against.Null(repository);
        _auditService = Guard.Against.Null(auditService);
        _conditions = Guard.Against.Null(conditions);
        _requestContext = Guard.Against.Null(requestContext);
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<ConditionsResponse>> CalculateConditionsAsync(
        CalculateConditionsRequest request, 
        Guid performingUserId,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Request received to calculate conditions for {RestockingCount} restocking operations", request.RestockingOperations.Count);

        var calculatedConditions = new List<CalculatedCondition>();
        foreach (var buildCondition in _conditions)
        {
            var operations = request.RestockingOperations
                .Where(x => buildCondition.AppliesToOperation(x))
                .ToList();

            if (operations.NotAny())
            {
                _logger.LogDebug("No restocking operations applicable for condition {ConditionName}", buildCondition.GetType().FullName);
                continue;
            }

            _logger.LogDebug("Condition {ConditionName} processing {Count} applicable restocking operations", buildCondition.GetType().FullName, operations.Count);
            var (_, isFailure, nextConditions, error) = buildCondition.CalculateCondition(operations);

            if (isFailure)
            {
                _logger.LogError("Calculating conditions for condition {ConditionName} failed with error {Error}", buildCondition.GetType().FullName, error);
                await RaiseAuditAsync(
                    AuditEvents.ConditionsBuiltForApplicationFailure,
                    request.FellingLicenceApplicationId,
                    performingUserId, 
                    new
                    {
                        IsDraft = request.IsDraft,
                        Error = error
                    });
                return Result.Failure<ConditionsResponse>(error);
            }

            calculatedConditions.AddRange(nextConditions);
        }

        if (request.IsDraft is false)
        {
            _logger.LogDebug("Saving {Count} calculated conditions for application with id {ApplicationId}", 
                calculatedConditions.Count, request.FellingLicenceApplicationId);
            var saveResult = await SaveConditionsAsync(
                request.FellingLicenceApplicationId,
                performingUserId,
                calculatedConditions,
                cancellationToken);

            if (saveResult.IsFailure)
            {
                await RaiseAuditAsync(
                    AuditEvents.ConditionsBuiltForApplicationFailure,
                    request.FellingLicenceApplicationId,
                    performingUserId,
                    new
                    {
                        IsDraft = request.IsDraft,
                        Error = saveResult.Error
                    });
                return Result.Failure<ConditionsResponse>(saveResult.Error);
            }
        }

        await RaiseAuditAsync(
            AuditEvents.ConditionsBuiltForApplication,
            request.FellingLicenceApplicationId,
            performingUserId, 
            new
            {
                ConditionsCount = calculatedConditions.Count,
                IsDraft = request.IsDraft
            });

        var response = new ConditionsResponse
        {
            Conditions = calculatedConditions
        };
        return Result.Success(response);
    }

    /// <inheritdoc />
    public async Task<Result> StoreConditionsAsync(
        StoreConditionsRequest request, 
        Guid performingUserId, 
        CancellationToken cancellationToken)
    {
        return await SaveConditionsAsync(
            request.FellingLicenceApplicationId,
            performingUserId,
            request.Conditions,
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ConditionsResponse> RetrieveExistingConditionsAsync(
        Guid applicationId, 
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Request received for existing conditions within the repository for application with id {ApplicationId}", applicationId);

        var existingConditions = await _repository.GetConditionsForApplicationAsync(applicationId, cancellationToken);
        _logger.LogDebug("{Count} conditions retrieved from the repository for application with id {ApplicationId}", existingConditions.Count, applicationId);

        var calculatedConditions = existingConditions.Select(x => new CalculatedCondition
        {
            Parameters = x.Parameters,
            ConditionsText = x.ConditionsText.ToArray(),
            AppliesToSubmittedCompartmentIds = x.AppliesToSubmittedCompartmentIds
        });

        return new ConditionsResponse
        {
            Conditions = calculatedConditions.ToList()
        };
    }

    private async Task<Result> SaveConditionsAsync(
        Guid applicationId,
        Guid performingUserId,
        List<CalculatedCondition> conditions,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Clearing existing conditions in the repository for application with id {ApplicationId}", applicationId);
        await _repository.ClearConditionsForApplicationAsync(applicationId, cancellationToken);

        if (conditions.Any())
        {
            _logger.LogDebug("Storing {Count} conditions in the repository for application with id {ApplicationId}", conditions.Count, applicationId);
            var conditionEntities = conditions.Select(x => new FellingLicenceCondition
            {
                AppliesToSubmittedCompartmentIds = x.AppliesToSubmittedCompartmentIds,
                ConditionsText = x.ConditionsText.ToList(),
                FellingLicenceApplicationId = applicationId,
                Parameters = x.Parameters
            }).ToList();
            await _repository.SaveConditionsForApplicationAsync(conditionEntities, cancellationToken);
        }

        _logger.LogDebug("Saving changes to the repository");
        var saveResult = await _repository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
        if (saveResult.IsFailure)
        {
            _logger.LogError(
                "Error encountered saving conditions for application with id {ApplicationId}, {Error}",
                applicationId, saveResult.Error);
            await RaiseAuditAsync(
                AuditEvents.ConditionsSavedForApplicationFailure,
                applicationId,
                performingUserId,
                new
                {
                    Error = saveResult.Error
                });
            return Result.Failure("Could not store conditions in repository");
        }

        await RaiseAuditAsync(
            AuditEvents.ConditionsSavedForApplication,
            applicationId,
            performingUserId,
            new
            {
                ConditionsCount = conditions.Count
            });

        return Result.Success();
    }

    private async Task RaiseAuditAsync(
        string eventName, 
        Guid applicationId, 
        Guid performingUserId,
        object auditData)
    {
        await _auditService.PublishAuditEventAsync(new AuditEvent(
            eventName,
            applicationId,
            performingUserId,
            _requestContext,
            auditData));
    }
}