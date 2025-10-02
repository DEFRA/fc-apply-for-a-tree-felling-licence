using FluentEmail.Core;
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
        Assert.NotEmpty(result);
        
        result.Values.ForEach(model =>
        {
            Assert.Matches("^[A-Z]+$", model.Code);
            Assert.Matches("^[\\w-/()' ]+$", model.Name);
        });
    }
}