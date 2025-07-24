using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.ConditionsBuilder.Entities;
using Forestry.Flo.Services.ConditionsBuilder.Models;
using Forestry.Flo.Services.ConditionsBuilder.Repositories;
using Forestry.Flo.Services.ConditionsBuilder.Services;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Forestry.Flo.Services.ConditionsBuilder.Tests.Services;

public class CalculateConditionsTests
{
    private Mock<IAuditService<CalculateConditionsService>> _mockAudits = new();
    private Mock<IConditionsBuilderRepository> _mockRepository = new();
    private Mock<IUnitOfWork> _mockUnitOfWork = new();
    private Mock<IBuildCondition> _mockBuildCondition = new();
    protected readonly string RequestContextCorrelationId = Guid.NewGuid().ToString();
    protected readonly Guid RequestContextUserId = Guid.NewGuid();

    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [Theory, AutoData]
    public async Task WhenDraftRequestAndNoCompartmentsMatch(
        CalculateConditionsRequest request,
        Guid performingUserId)
    {
        var sut = CreateSut();

        _mockBuildCondition
            .Setup(x => x.AppliesToOperation(It.IsAny<RestockingOperationDetails>()))
            .Returns(false);

        request.IsDraft = true;

        var result = await sut.CalculateConditionsAsync(request, performingUserId, CancellationToken.None);

        Assert.True(result.IsSuccess);

        foreach (var compartment in request.RestockingOperations)
        {
            _mockBuildCondition.Verify(x => x.AppliesToOperation(It.Is<RestockingOperationDetails>(y => y == compartment)), Times.Once);
        }
        _mockBuildCondition.VerifyNoOtherCalls();

        _mockRepository.VerifyNoOtherCalls();

        _mockAudits.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.ConditionsBuiltForApplication
                && a.ActorType == ActorType.InternalUser
                && a.UserId == performingUserId
                && a.SourceEntityId == request.FellingLicenceApplicationId
                && a.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && a.CorrelationId == RequestContextCorrelationId
                && JsonSerializer.Serialize(a.AuditData, _serializerOptions) ==
                JsonSerializer.Serialize(new
                {
                    conditionsCount = 0,
                    isDraft = true,
                }, _serializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);
        _mockAudits.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task WhenDraftRequestAndOneCompartmentMatchesButConditionBuilderFails(
        CalculateConditionsRequest request,
        Guid performingUserId,
        string error)
    {
        var sut = CreateSut();

        var applicableCompartment = request.RestockingOperations.First();

        _mockBuildCondition
            .Setup(x => x.AppliesToOperation(It.Is<RestockingOperationDetails>(y => y == applicableCompartment)))
            .Returns(true);
        _mockBuildCondition
            .Setup(x => x.AppliesToOperation(It.Is<RestockingOperationDetails>(y => y != applicableCompartment)))
            .Returns(false);
        _mockBuildCondition
            .Setup(x => x.CalculateCondition(It.IsAny<List<RestockingOperationDetails>>()))
            .Returns(Result.Failure<List<CalculatedCondition>>(error));

        request.IsDraft = true;

        var result = await sut.CalculateConditionsAsync(request, performingUserId, CancellationToken.None);

        Assert.True(result.IsFailure);

        foreach (var compartment in request.RestockingOperations)
        {
            _mockBuildCondition.Verify(x => x.AppliesToOperation(It.Is<RestockingOperationDetails>(y => y == compartment)), Times.Once);
        }
        _mockBuildCondition.Verify(x => x.CalculateCondition(It.Is<List<RestockingOperationDetails>>(y => y.Single() == applicableCompartment)), Times.Once);
        _mockBuildCondition.VerifyNoOtherCalls();

        _mockRepository.VerifyNoOtherCalls();

        _mockAudits.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.ConditionsBuiltForApplicationFailure
                && a.ActorType == ActorType.InternalUser
                && a.UserId == performingUserId
                && a.SourceEntityId == request.FellingLicenceApplicationId
                && a.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && a.CorrelationId == RequestContextCorrelationId
                && JsonSerializer.Serialize(a.AuditData, _serializerOptions) ==
                JsonSerializer.Serialize(new
                {
                    isDraft = true,
                    error = error
                }, _serializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);
        _mockAudits.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task WhenDraftRequestAndOneCompartmentMatchesAndConditionsBuilt(
        CalculateConditionsRequest request,
        Guid performingUserId,
        List<CalculatedCondition> conditions)
    {
        var sut = CreateSut();

        var applicableCompartment = request.RestockingOperations.First();

        _mockBuildCondition
            .Setup(x => x.AppliesToOperation(It.Is<RestockingOperationDetails>(y => y == applicableCompartment)))
            .Returns(true);
        _mockBuildCondition
            .Setup(x => x.AppliesToOperation(It.Is<RestockingOperationDetails>(y => y != applicableCompartment)))
            .Returns(false);
        _mockBuildCondition
            .Setup(x => x.CalculateCondition(It.IsAny<List<RestockingOperationDetails>>()))
            .Returns(Result.Success(conditions));

        request.IsDraft = true;

        var result = await sut.CalculateConditionsAsync(request, performingUserId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(conditions, result.Value.Conditions);

        foreach (var compartment in request.RestockingOperations)
        {
            _mockBuildCondition.Verify(x => x.AppliesToOperation(It.Is<RestockingOperationDetails>(y => y == compartment)), Times.Once);
        }
        _mockBuildCondition.Verify(x => x.CalculateCondition(It.Is<List<RestockingOperationDetails>>(y => y.Single() == applicableCompartment)), Times.Once);
        _mockBuildCondition.VerifyNoOtherCalls();

        _mockRepository.VerifyNoOtherCalls();

        _mockAudits.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.ConditionsBuiltForApplication
                && a.ActorType == ActorType.InternalUser
                && a.UserId == performingUserId
                && a.SourceEntityId == request.FellingLicenceApplicationId
                && a.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && a.CorrelationId == RequestContextCorrelationId
                && JsonSerializer.Serialize(a.AuditData, _serializerOptions) ==
                JsonSerializer.Serialize(new
                {
                    conditionsCount = conditions.Count,
                    isDraft = true
                }, _serializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);
        _mockAudits.VerifyNoOtherCalls();
    }


    [Theory, AutoData]
    public async Task WhenNotDraftRequestAndNoCompartmentsMatch(
        CalculateConditionsRequest request,
        Guid performingUserId)
    {
        var sut = CreateSut();

        _mockBuildCondition
            .Setup(x => x.AppliesToOperation(It.IsAny<RestockingOperationDetails>()))
            .Returns(false);
        _mockUnitOfWork
            .Setup(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());

        request.IsDraft = false;

        var result = await sut.CalculateConditionsAsync(request, performingUserId, CancellationToken.None);

        Assert.True(result.IsSuccess);

        foreach (var compartment in request.RestockingOperations)
        {
            _mockBuildCondition.Verify(x => x.AppliesToOperation(It.Is<RestockingOperationDetails>(y => y == compartment)), Times.Once);
        }
        _mockBuildCondition.VerifyNoOtherCalls();

        _mockRepository.Verify(x => x.ClearConditionsForApplicationAsync(request.FellingLicenceApplicationId, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.VerifyGet(x => x.UnitOfWork);
        _mockUnitOfWork.Verify(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.VerifyNoOtherCalls();
        _mockRepository.VerifyNoOtherCalls();

        _mockAudits.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.ConditionsSavedForApplication
                && a.ActorType == ActorType.InternalUser
                && a.UserId == performingUserId
                && a.SourceEntityId == request.FellingLicenceApplicationId
                && a.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && a.CorrelationId == RequestContextCorrelationId
                && JsonSerializer.Serialize(a.AuditData, _serializerOptions) ==
                JsonSerializer.Serialize(new
                {
                    conditionsCount = 0
                }, _serializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);
        _mockAudits.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.ConditionsBuiltForApplication
                && a.ActorType == ActorType.InternalUser
                && a.UserId == performingUserId
                && a.SourceEntityId == request.FellingLicenceApplicationId
                && a.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && a.CorrelationId == RequestContextCorrelationId
                && JsonSerializer.Serialize(a.AuditData, _serializerOptions) ==
                JsonSerializer.Serialize(new
                {
                    conditionsCount = 0,
                    isDraft = false,
                }, _serializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);
        _mockAudits.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task WhenNotDraftRequestAndNoCompartmentsMatchAndDbUpdateFails(
        CalculateConditionsRequest request,
        Guid performingUserId,
        UserDbErrorReason error)
    {
        var sut = CreateSut();

        _mockBuildCondition
            .Setup(x => x.AppliesToOperation(It.IsAny<RestockingOperationDetails>()))
            .Returns(false);
        _mockUnitOfWork
            .Setup(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Failure(error));

        request.IsDraft = false;

        var result = await sut.CalculateConditionsAsync(request, performingUserId, CancellationToken.None);

        Assert.True(result.IsFailure);

        foreach (var compartment in request.RestockingOperations)
        {
            _mockBuildCondition.Verify(x => x.AppliesToOperation(It.Is<RestockingOperationDetails>(y => y == compartment)), Times.Once);
        }
        _mockBuildCondition.VerifyNoOtherCalls();

        _mockRepository.Verify(x => x.ClearConditionsForApplicationAsync(request.FellingLicenceApplicationId, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.VerifyGet(x => x.UnitOfWork);
        _mockUnitOfWork.Verify(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.VerifyNoOtherCalls();
        _mockRepository.VerifyNoOtherCalls();

        _mockAudits.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.ConditionsSavedForApplicationFailure
                && a.ActorType == ActorType.InternalUser
                && a.UserId == performingUserId
                && a.SourceEntityId == request.FellingLicenceApplicationId
                && a.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && a.CorrelationId == RequestContextCorrelationId
                && JsonSerializer.Serialize(a.AuditData, _serializerOptions) ==
                JsonSerializer.Serialize(new
                {
                    error = error
                }, _serializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);
        _mockAudits.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.ConditionsBuiltForApplicationFailure
                && a.ActorType == ActorType.InternalUser
                && a.UserId == performingUserId
                && a.SourceEntityId == request.FellingLicenceApplicationId
                && a.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && a.CorrelationId == RequestContextCorrelationId
                && JsonSerializer.Serialize(a.AuditData, _serializerOptions) ==
                JsonSerializer.Serialize(new
                {
                    isDraft = false,
                    error = "Could not store conditions in repository"
                }, _serializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);
        _mockAudits.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task WhenNotDraftRequestAndOneCompartmentMatchesAndDbUpdateSucceeds(
        CalculateConditionsRequest request,
        CalculatedCondition condition,
        Guid performingUserId)
    {
        var sut = CreateSut();

        var applicableCompartment = request.RestockingOperations.First();
        _mockBuildCondition
            .Setup(x => x.AppliesToOperation(It.Is<RestockingOperationDetails>(y => y == applicableCompartment)))
            .Returns(true);
        _mockBuildCondition
            .Setup(x => x.AppliesToOperation(It.Is<RestockingOperationDetails>(y => y != applicableCompartment)))
            .Returns(false);
        _mockBuildCondition
            .Setup(x => x.CalculateCondition(It.IsAny<List<RestockingOperationDetails>>()))
            .Returns(Result.Success(new List<CalculatedCondition> { condition }));
        _mockUnitOfWork
            .Setup(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());

        request.IsDraft = false;

        var result = await sut.CalculateConditionsAsync(request, performingUserId, CancellationToken.None);

        Assert.True(result.IsSuccess);

        Assert.Equal(condition, result.Value.Conditions.Single());

        foreach (var compartment in request.RestockingOperations)
        {
            _mockBuildCondition.Verify(x => x.AppliesToOperation(It.Is<RestockingOperationDetails>(y => y == compartment)), Times.Once);
        }
        _mockBuildCondition.Verify(x => x.CalculateCondition(It.Is<List<RestockingOperationDetails>>(y => y.Single() == applicableCompartment)), Times.Once);
        _mockBuildCondition.VerifyNoOtherCalls();

        _mockRepository.Verify(x => x.ClearConditionsForApplicationAsync(request.FellingLicenceApplicationId, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(x => x.SaveConditionsForApplicationAsync(It.Is<List<FellingLicenceCondition>>(e => 
            e.Count == 1
            && e.Single().FellingLicenceApplicationId == request.FellingLicenceApplicationId
            && e.Single().AppliesToSubmittedCompartmentIds == condition.AppliesToSubmittedCompartmentIds
            && string.Join(",", e.Single().ConditionsText) == string.Join(",", condition.ConditionsText)
            && e.Single().Parameters == condition.Parameters
            ), It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.VerifyGet(x => x.UnitOfWork);
        _mockUnitOfWork.Verify(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.VerifyNoOtherCalls();
        _mockRepository.VerifyNoOtherCalls();

        _mockAudits.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.ConditionsSavedForApplication
                && a.ActorType == ActorType.InternalUser
                && a.UserId == performingUserId
                && a.SourceEntityId == request.FellingLicenceApplicationId
                && a.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && a.CorrelationId == RequestContextCorrelationId
                && JsonSerializer.Serialize(a.AuditData, _serializerOptions) ==
                JsonSerializer.Serialize(new
                {
                    conditionsCount = 1
                }, _serializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);
        _mockAudits.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.ConditionsBuiltForApplication
                && a.ActorType == ActorType.InternalUser
                && a.UserId == performingUserId
                && a.SourceEntityId == request.FellingLicenceApplicationId
                && a.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && a.CorrelationId == RequestContextCorrelationId
                && JsonSerializer.Serialize(a.AuditData, _serializerOptions) ==
                JsonSerializer.Serialize(new
                {
                    conditionsCount = 1,
                    isDraft = false
                }, _serializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);
        _mockAudits.VerifyNoOtherCalls();
    }

    private CalculateConditionsService CreateSut()
    {
        var user = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(
            localAccountId: RequestContextUserId,
            accountTypeInternal: AccountTypeInternal.WoodlandOfficer);
        var requestContext = new RequestContext(
            RequestContextCorrelationId,
            new RequestUserModel(user));

        _mockAudits.Reset();
        _mockRepository.Reset();
        _mockBuildCondition.Reset();
        _mockUnitOfWork.Reset();

        _mockRepository.SetupGet(x => x.UnitOfWork).Returns(_mockUnitOfWork.Object);

        return new CalculateConditionsService(
            _mockRepository.Object,
            new[] { _mockBuildCondition.Object },
            _mockAudits.Object, 
            requestContext,
            new NullLogger<CalculateConditionsService>());
    }
}