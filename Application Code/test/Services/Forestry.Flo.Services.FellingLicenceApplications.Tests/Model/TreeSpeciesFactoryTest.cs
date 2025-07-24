using FluentAssertions;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Xunit;

namespace Forestry.Flo.Services.FellingLicenceApplications.Tests.Model;

public class TreeSpeciesFactoryTest
{
    [Fact]
    public void ShouldReturnSpeciesDictionary()
    {
        //arrange
        //act
        var result = TreeSpeciesFactory.SpeciesDictionary;

        //assert
        result.Should().NotBeEmpty();
        result.Values.Should().AllSatisfy(model =>
        {
            model.Code.Should().MatchRegex("^[A-Z]+$");
            model.Name.Should().MatchRegex("^[\\w-/()' ]+$");
            model.SpeciesType.Should().BeOneOf(SpeciesType.Broadleaf, SpeciesType.Conifer);
        });
    }
}