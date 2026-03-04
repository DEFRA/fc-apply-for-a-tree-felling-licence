using Forestry.Flo.Services.Gis.Models.Esri.Responses;

namespace Forestry.Flo.Services.Gis.Tests.Services;

public partial class ForestryServicesTests
{
    [Theory]
    [InlineData(49.882531, -4.0524339, 0, true, "SX 252646 000051")]
    [InlineData(49.882531, -4.0524339, 12, true, "SX 52646 00051")]
    [InlineData(49.882531, -4.0524339, 8, true, "SX 526 000")]
    [InlineData(49.882531, -4.0524339, 12, false, "SX5264600051")]
    [InlineData(51.557540, -1.7366200, 0, true, "SU 418356 184328")]
    [InlineData(51.557540, -1.7366200, 12, true, "SU 18356 84328")]
    [InlineData(51.557540, -1.7366200, 8, true, "SU 183 843")]
    [InlineData(51.557540, -1.7366200, 12, false, "SU1835684328")]
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
    [InlineData(51.557540, -1.7366200, 1,true, "SU 183 843")]
    [InlineData(51.557540, -1.7366200, 9, true, "SU 183 843")]
    public void ConvertLatLongToOSGrid_InvalidGridLengthDefaultsToEight(float lat, float lon, int length, bool includeSpaces, string expectedResult)
    {
        var sut = CreateSut();

        var result = sut.ConvertLatLongToOSGrid(new LatLongObj { Longitude = lon, Latitude = lat }, length, includeSpaces, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(expectedResult, result.Value);
    }

}
