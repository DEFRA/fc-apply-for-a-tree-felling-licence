using AutoFixture.Xunit2;
using Forestry.Flo.External.Web.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Forestry.Flo.External.Web.Tests.Infrastructure;

public class TrimmedFormValueProviderTests
{
    [Theory, AutoData]
    public void ShouldTrimFormFieldStringValues(string name, string value)
    {
        //arrange
        var formCollection = new FormCollection(new Dictionary<string, StringValues> {
            { name, new StringValues( new[]{$"{value} ", value}) }});
        var sut = new TrimmedFormValueProvider(formCollection);
       
        //act
        var result = sut.GetValue(name);

        //assert
        Assert.Equal(value, result.FirstValue);
        Assert.Equal(value, result.LastOrDefault());
    }
}