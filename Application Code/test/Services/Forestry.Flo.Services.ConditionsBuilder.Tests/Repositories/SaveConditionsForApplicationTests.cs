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

public class SaveConditionsForApplicationTests
{
    private ConditionsBuilderContext _context;

    [Theory, AutoMoqData]
    public async Task WhenNoConditionsInDatabase(
        IEnumerable<FellingLicenceCondition> conditions, 
        Guid applicationId)
    {
        foreach (var fellingLicenceCondition in conditions)
        {
            fellingLicenceCondition.FellingLicenceApplicationId = applicationId;
        }

        var sut = CreateSut();

        await sut.SaveConditionsForApplicationAsync(conditions.ToList(), CancellationToken.None);
        
        //should not be in repository until complete unit of work
        Assert.Empty(_context.FellingLicenceConditions);

        await sut.UnitOfWork.SaveEntitiesAsync(CancellationToken.None);

        Assert.Equal(conditions.Count(), _context.FellingLicenceConditions.Count());

        foreach (var fellingLicenceCondition in conditions)
        {
            Assert.Contains(_context.FellingLicenceConditions, x => x == fellingLicenceCondition);
        }
    }

    [Theory, AutoMoqData]
    public async Task WhenConditionsExistForMultipleApplications(
        IEnumerable<FellingLicenceCondition> existingConditions,
        IEnumerable<FellingLicenceCondition> newConditions)
    {
        var sut = CreateSut();
        await _context.FellingLicenceConditions.AddRangeAsync(existingConditions);
        await _context.SaveEntitiesAsync(CancellationToken.None);

        await sut.SaveConditionsForApplicationAsync(newConditions.ToList(), CancellationToken.None);

        //new ones should not be in repository until complete unit of work
        foreach (var fellingLicenceCondition in existingConditions)
        {
            Assert.Contains(_context.FellingLicenceConditions, x => x == fellingLicenceCondition);
        }

        foreach (var fellingLicenceCondition in newConditions)
        {
            Assert.DoesNotContain(_context.FellingLicenceConditions, x => x == fellingLicenceCondition);
        }

        await sut.UnitOfWork.SaveEntitiesAsync(CancellationToken.None);

        foreach (var fellingLicenceCondition in existingConditions)
        {
            Assert.Contains(_context.FellingLicenceConditions, x => x == fellingLicenceCondition);
        }

        foreach (var fellingLicenceCondition in newConditions)
        {
            Assert.Contains(_context.FellingLicenceConditions, x => x == fellingLicenceCondition);
        }
    }

    [Theory, AutoMoqData]
    public async Task CanRemoveOldConditionsAndAddNewOnesInSingleTransaction(
        IEnumerable<FellingLicenceCondition> existingConditions,
        IEnumerable<FellingLicenceCondition> newConditions,
        Guid applicationId)
    {
        foreach (var fellingLicenceCondition in existingConditions)
        {
            fellingLicenceCondition.FellingLicenceApplicationId = applicationId;
        }
        foreach (var fellingLicenceCondition in newConditions)
        {
            fellingLicenceCondition.FellingLicenceApplicationId = applicationId;
        }

        var sut = CreateSut();
        await _context.FellingLicenceConditions.AddRangeAsync(existingConditions);
        await _context.SaveEntitiesAsync(CancellationToken.None);

        await sut.ClearConditionsForApplicationAsync(applicationId, CancellationToken.None);
        await sut.SaveConditionsForApplicationAsync(newConditions.ToList(), CancellationToken.None);

        //old ones should remain and new ones should not be in repository until we complete unit of work
        foreach (var fellingLicenceCondition in existingConditions)
        {
            Assert.Contains(_context.FellingLicenceConditions, x => x == fellingLicenceCondition);
        }
        foreach (var fellingLicenceCondition in newConditions)
        {
            Assert.DoesNotContain(_context.FellingLicenceConditions, x => x == fellingLicenceCondition);
        }

        //complete the transaction, now old ones should be gone and new ones saved
        await sut.UnitOfWork.SaveEntitiesAsync(CancellationToken.None);

        foreach (var fellingLicenceCondition in existingConditions)
        {
            Assert.DoesNotContain(_context.FellingLicenceConditions, x => x == fellingLicenceCondition);
        }
        foreach (var fellingLicenceCondition in newConditions)
        {
            Assert.Contains(_context.FellingLicenceConditions, x => x == fellingLicenceCondition);
        }
    }

    private ConditionsBuilderRepository CreateSut()
    {
        _context = TestConditionsBuilderDatabaseFactory.CreateDefaultTestContext();
        return new ConditionsBuilderRepository(_context);
    }
}