using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.ConditionsBuilder.Entities;
using Forestry.Flo.Services.ConditionsBuilder.Repositories;
using Forestry.Flo.Services.ConditionsBuilder.Services;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Forestry.Flo.Services.ConditionsBuilder.Tests.Services;

public class RetrieveExistingConditionsTests
{
    private Mock<IConditionsBuilderRepository> _mockRepository = new();

    [Theory, AutoMoqData]
    public async Task WhenNoConditionsExistForApplication(
        Guid applicationId)
    {
        var sut = CreateSut();

        _mockRepository
            .Setup(x => x.GetConditionsForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FellingLicenceCondition>(0));

        var result = await sut.RetrieveExistingConditionsAsync(applicationId, CancellationToken.None);

        Assert.Empty(result.Conditions);

        _mockRepository.Verify(x => x.GetConditionsForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenConditionsExistForApplication(
        Guid applicationId,
        List<FellingLicenceCondition> conditionEntities)
    {
        var sut = CreateSut();

        _mockRepository
            .Setup(x => x.GetConditionsForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(conditionEntities);

        var result = await sut.RetrieveExistingConditionsAsync(applicationId, CancellationToken.None);

        Assert.Equal(conditionEntities.Count, result.Conditions.Count);
        foreach (var condition in conditionEntities)
        {
            Assert.Contains(result.Conditions, x =>
                x.AppliesToSubmittedCompartmentIds == condition.AppliesToSubmittedCompartmentIds
                && x.Parameters == condition.Parameters
                && string.Join(",", x.ConditionsText) == string.Join(",", condition.ConditionsText));
        }

        _mockRepository.Verify(x => x.GetConditionsForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.VerifyNoOtherCalls();
    }

    private CalculateConditionsService CreateSut()
    {
        var user = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal();

        _mockRepository.Reset();

        return new CalculateConditionsService(
            _mockRepository.Object,
            Array.Empty<IBuildCondition>(),
            new Mock<IAuditService<CalculateConditionsService>>().Object,
            new RequestContext(Guid.NewGuid().ToString(), new RequestUserModel(user)),
            new NullLogger<CalculateConditionsService>());
    }
}