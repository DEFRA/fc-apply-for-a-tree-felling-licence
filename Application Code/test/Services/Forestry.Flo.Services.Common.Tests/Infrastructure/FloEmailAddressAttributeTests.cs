using FluentAssertions;
using Forestry.Flo.Services.Common.Infrastructure;
using Xunit;

namespace Forestry.Flo.Services.Common.Tests.Infrastructure;

public class FloEmailAddressAttributeTests
{
    private readonly FloEmailAddressAttribute _sut;
    
    public FloEmailAddressAttributeTests()
    {
        _sut = new FloEmailAddressAttribute();
    }

    [Fact]
    public void ShouldValidateSuccessfully_GivenValidEmailAddress()
    {
        //arrange
        const string email = "test@testdomain.com";

        //act
        var result = _sut.IsValid(email);

        //assert
        result.Should().BeTrue();
    } 
    
    [Fact]
    public void ShouldValidateWithError_GivenInvalidEmailAddress()
    {
        //arrange
        const string email = "test";

        //act
        var result = _sut.IsValid(email);

        //assert
        result.Should().BeFalse();
    }
}