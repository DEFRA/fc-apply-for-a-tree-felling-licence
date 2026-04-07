using System.Linq;
using AutoFixture;
using Forestry.Flo.Services.ConditionsBuilder.Configuration;
using Forestry.Flo.Services.ConditionsBuilder.Models;
using Forestry.Flo.Services.ConditionsBuilder.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Forestry.Flo.Services.ConditionsBuilder.Tests.Services;

public class CBWConditionBuilderTests
{
    private readonly string[] _conditionText =
    [
        "Within two years after felling completion, the licence holder will ensure that the land where felling took place has been:",
        "• planted with cricket bat willow, {RESTOCKINGNUMBER}to at least 100 plants per hectare, evenly spread across the site"
    ];

    private readonly Fixture _fixture = new Fixture();

    [Fact]
    public void WhenOnlyIndividualTreesOperations()
    {
        var restocking = _fixture
            .Build<RestockingOperationDetails>()
            .With(x => x.FellingOperationType, FellingOperationType.FellingIndividualTrees)
            .With(x => x.RestockingProposalType, RestockingProposalType.RestockWithIndividualTrees)
            .CreateMany()
            .ToList();

        var numberOfTrees = restocking.Sum(x => x.NumberOfTrees);

        var sut = CreateSut();

        var result = sut.CalculateCBWCondition(restocking);

        Assert.True(result.IsSuccess);

        Assert.Equal(2, result.Value.ConditionsText.Length);
        Assert.Equal(_conditionText[0], result.Value.ConditionsText[0]);
        Assert.Equal(_conditionText[1].Replace("{RESTOCKINGNUMBER}", $"with {numberOfTrees} in total equivalent "), result.Value.ConditionsText[1]);
    }

    [Fact]
    public void WhenOnlyClearFellingOperations()
    {
        var restocking = _fixture
            .Build<RestockingOperationDetails>()
            .With(x => x.FellingOperationType, FellingOperationType.ClearFelling)
            .With(x => x.RestockingProposalType, RestockingProposalType.ReplantTheFelledArea)
            .Without(x => x.NumberOfTrees)
            .CreateMany()
            .ToList();

        var sut = CreateSut();

        var result = sut.CalculateCBWCondition(restocking);

        Assert.True(result.IsSuccess);

        Assert.Equal(2, result.Value.ConditionsText.Length);
        Assert.Equal(_conditionText[0], result.Value.ConditionsText[0]);
        Assert.Equal(_conditionText[1].Replace("{RESTOCKINGNUMBER}", string.Empty), result.Value.ConditionsText[1]);
    }

    [Fact]
    public void WhenIndividualTreesAndClearFellingOperationsInOneApplication()
    {
        var restocking1 = _fixture
            .Build<RestockingOperationDetails>()
            .With(x => x.FellingOperationType, FellingOperationType.ClearFelling)
            .With(x => x.RestockingProposalType, RestockingProposalType.ReplantTheFelledArea)
            .Without(x => x.NumberOfTrees)
            .Create();
        var restocking2 = _fixture
            .Build<RestockingOperationDetails>()
            .With(x => x.FellingOperationType, FellingOperationType.FellingIndividualTrees)
            .With(x => x.RestockingProposalType, RestockingProposalType.RestockWithIndividualTrees)
            .Create();

        var sut = CreateSut();

        var result = sut.CalculateCBWCondition([restocking1, restocking2]);

        Assert.True(result.IsSuccess);

        Assert.Equal(2, result.Value.ConditionsText.Length);
        Assert.Equal(_conditionText[0], result.Value.ConditionsText[0]);
        Assert.Equal(_conditionText[1].Replace("{RESTOCKINGNUMBER}", string.Empty), result.Value.ConditionsText[1]);
    }

    private CBWConditionBuilder CreateSut()
    {
        var options = new ConditionsBuilderOptions
        {
            CBWOptions = new ConditionOptions
            {
                ConditionParameters = [],
                ConditionText = _conditionText
            }
        };

        return new CBWConditionBuilder(new OptionsWrapper<ConditionsBuilderOptions>(options), new NullLogger<CBWConditionBuilder>());
    }
}