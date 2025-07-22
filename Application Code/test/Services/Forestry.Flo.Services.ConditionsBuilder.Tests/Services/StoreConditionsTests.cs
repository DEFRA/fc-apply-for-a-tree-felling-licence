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

public class StoreConditionsTests
{
    private Mock<IAuditService<CalculateConditionsService>> _mockAudits = new();
    private Mock<IConditionsBuilderRepository> _mockRepository = new();
    private Mock<IUnitOfWork> _mockUnitOfWork = new();
    protected readonly string RequestContextCorrelationId = Guid.NewGuid().ToString();
    protected readonly Guid RequestContextUserId = Guid.NewGuid();

    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [Theory, AutoData]
    public async Task WhenNoConditionsAndSaveEntitiesFails(
        Guid applicationId,
        Guid performingUserId,
        UserDbErrorReason error)
    {
        var request = new StoreConditionsRequest
        {
            FellingLicenceApplicationId = applicationId,
            Conditions = new List<CalculatedCondition>(0)
        };

        var sut = CreateSut();

        _mockUnitOfWork
            .Setup(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Failure(error));

        var result = await sut.StoreConditionsAsync(request, performingUserId, CancellationToken.None);

        Assert.True(result.IsFailure);

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
        _mockAudits.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task WhenOneConditionAndSaveEntitiesFails(
        Guid applicationId,
        Guid performingUserId,
        CalculatedCondition condition,
        UserDbErrorReason error)
    {
        var request = new StoreConditionsRequest
        {
            FellingLicenceApplicationId = applicationId,
            Conditions = new List<CalculatedCondition> { condition }
        };

        var sut = CreateSut();

        _mockUnitOfWork
            .Setup(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Failure(error));

        var result = await sut.StoreConditionsAsync(request, performingUserId, CancellationToken.None);

        Assert.True(result.IsFailure);

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
        _mockAudits.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task WhenSuccessfulSave(
        Guid applicationId,
        Guid performingUserId,
        CalculatedCondition condition)
    {
        var request = new StoreConditionsRequest
        {
            FellingLicenceApplicationId = applicationId,
            Conditions = new List<CalculatedCondition> { condition }
        };

        var sut = CreateSut();

        _mockUnitOfWork
            .Setup(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());

        var result = await sut.StoreConditionsAsync(request, performingUserId, CancellationToken.None);

        Assert.True(result.IsSuccess);

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
        _mockUnitOfWork.Reset();

        _mockRepository.SetupGet(x => x.UnitOfWork).Returns(_mockUnitOfWork.Object);

        return new CalculateConditionsService(
            _mockRepository.Object,
            Array.Empty<IBuildCondition>(),
            _mockAudits.Object,
            requestContext,
            new NullLogger<CalculateConditionsService>());
    }
}