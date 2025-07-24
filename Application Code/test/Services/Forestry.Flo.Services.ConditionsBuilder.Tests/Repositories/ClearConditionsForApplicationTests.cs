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

public class ClearConditionsForApplicationTests
{
    private ConditionsBuilderContext _context;

    [Theory, AutoMoqData]
    public async Task WhenNoConditionsInDatabase(
        Guid fellingLicenceApplicationId)
    {
        var sut = CreateSut();

        await sut.ClearConditionsForApplicationAsync(fellingLicenceApplicationId, CancellationToken.None);

        Assert.Empty(_context.FellingLicenceConditions);
    }

    [Theory, AutoMoqData]
    public async Task WhenConditionsExistForMultipleApplications(
        IEnumerable<FellingLicenceCondition> conditions)
    {
        var sut = CreateSut();
        await _context.FellingLicenceConditions.AddRangeAsync(conditions);
        await _context.SaveEntitiesAsync(CancellationToken.None);

        var expectedCondition = conditions.First();

        await sut.ClearConditionsForApplicationAsync(expectedCondition.FellingLicenceApplicationId, CancellationToken.None);

        //should not be removed until transaction completes
        Assert.Contains(_context.FellingLicenceConditions, x => x == expectedCondition);

        await sut.UnitOfWork.SaveEntitiesAsync(CancellationToken.None);

        Assert.DoesNotContain(_context.FellingLicenceConditions, x => x == expectedCondition);

        Assert.Equal(conditions.Count() - 1, _context.FellingLicenceConditions.Count());
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

        await sut.ClearConditionsForApplicationAsync(applicationId, CancellationToken.None);

        Assert.Equal(conditions.Count(), _context.FellingLicenceConditions.Count());

        await sut.UnitOfWork.SaveEntitiesAsync(CancellationToken.None);

        Assert.Empty(_context.FellingLicenceConditions);
    }

    private ConditionsBuilderRepository CreateSut()
    {
        _context = TestConditionsBuilderDatabaseFactory.CreateDefaultTestContext();
        return new ConditionsBuilderRepository(_context);
    }
}