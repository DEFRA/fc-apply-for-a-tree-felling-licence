using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Forestry.Flo.Services.ConditionsBuilder.Entities;
using Forestry.Flo.Services.ConditionsBuilder.Repositories;
using Forestry.Flo.Tests.Common;
using Xunit;

namespace Forestry.Flo.Services.ConditionsBuilder.Tests.Repositories;

public class GetConditionsForApplicationTests
{
    private ConditionsBuilderContext _context;

    [Theory, AutoMoqData]
    public async Task WhenNoConditionsInDatabase(
        Guid fellingLicenceApplicationId)
    {
        var sut = CreateSut();

        var result = await sut.GetConditionsForApplicationAsync(fellingLicenceApplicationId, CancellationToken.None);

        Assert.Empty(result);
    }

    [Theory, AutoMoqData]
    public async Task WhenConditionsExistForMultipleApplications(
        IEnumerable<FellingLicenceCondition> conditions)
    {
        var sut = CreateSut();
        await _context.FellingLicenceConditions.AddRangeAsync(conditions);
        await _context.SaveEntitiesAsync(CancellationToken.None);

        var expectedCondition = conditions.First();

        var result =
            await sut.GetConditionsForApplicationAsync(expectedCondition.FellingLicenceApplicationId, CancellationToken.None);

        Assert.Equal(1, result.Count);
        Assert.Equal(expectedCondition, result.Single());
    }

    [Theory, AutoMoqData]
    public async Task WhenMultipleConditionsExistForApplication(
        IEnumerable<FellingLicenceCondition> conditions,
        Guid applicationId)
    {
        foreach (var fellingLicenceCondition in conditions)
        {
            fellingLicenceCondition.FellingLicenceApplicationId = applicationId;
        }

        var sut = CreateSut();
        await _context.FellingLicenceConditions.AddRangeAsync(conditions);
        await _context.SaveEntitiesAsync(CancellationToken.None);

        var result =
            await sut.GetConditionsForApplicationAsync(applicationId, CancellationToken.None);

        Assert.Equal(conditions.Count(), result.Count);
        foreach (var expectedCondition in conditions)
        {
            Assert.Contains(result, x => x == expectedCondition);
        }
    }

    private ConditionsBuilderRepository CreateSut()
    {
        _context = TestConditionsBuilderDatabaseFactory.CreateDefaultTestContext();
        return new ConditionsBuilderRepository(_context);
    }
}