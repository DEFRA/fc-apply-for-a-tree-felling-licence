using Forestry.Flo.Services.Gis.Models.Esri.Responses;

namespace Forestry.Flo.Services.Gis.Tests.Services;

public partial class ForestryServicesTests
{
    [Theory]
    [InlineData(51.557540, -1.7366200, 12, true, "SU 18356 84328")]
    [InlineData(51.557540, -1.7366200, 12, false, "SU1835684328")]
    [InlineData(51.557540, -1.7366200, 8, true, "SU 183 843")]
    public void ConvertLatLongToOSGrid_Checks(float lat, float lon, int length, bool includeSpaces, string expectedResult)
    {
        var sut = CreateSut();

        var result = sut.ConvertLatLongToOSGrid(new LatLongObj { Longitude = lon, Latitude = lat}, length, includeSpaces, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(expectedResult, result.Value);
    }

    [Fact]
    public void ConvertLatLongToOSGrid_NoInUK()
    {
        var sut = CreateSut();

        var result = sut.ConvertLatLongToOSGrid(new LatLongObj { Longitude = 100, Latitude = 10 }, 0, true, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("The given points are not in the uk", result.Error);
    }


    [Theory]
    [InlineData(51.557540, -1.7366200, 0,true, "SU 183 843")]
    [InlineData(51.557540, -1.7366200, 9, true, "SU 183 843")]
    public void ConvertLatLongToOSGrid_InvalidGridLengthDefaultsToEight(float lat, float lon, int length, bool includeSpaces, string expectedResult)
    {
        var sut = CreateSut();

        var result = sut.ConvertLatLongToOSGrid(new LatLongObj { Longitude = lon, Latitude = lat }, length, includeSpaces, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(expectedResult, result.Value);
    }

}
