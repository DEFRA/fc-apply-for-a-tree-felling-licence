using AutoFixture.Xunit2;
using Forestry.Flo.External.Web.Models.Compartment;

namespace Forestry.Flo.External.Web.Tests.Model;

public class CompartmentModelTests
{
    [Theory, AutoData]
    public void ShouldCreateDisplayNameSplittingCompartmentNumberAndSubCompartmentName(CompartmentModel compartmentModel)
    {
        //arrange
        
        //act
        var result = compartmentModel.DisplayName;

        //assert
        Assert.Equal($"{compartmentModel.CompartmentNumber}", result);
    }
}