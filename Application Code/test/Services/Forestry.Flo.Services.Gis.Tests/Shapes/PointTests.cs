using FluentAssertions;
using Forestry.Flo.Services.Gis.Models.Internal.MapObjects;
using Newtonsoft.Json;

namespace Forestry.Flo.Services.Gis.Tests.Shapes;

public class PointTests
{
    private string shape = "{\"spatialReference\": { \"latestWkid\": 27700, \"wkid\": 27700 },\"x\": 359170.4299405478,\"y\": 173119.12010877646  }";
    [Fact]
    public void GetExtent_Test()
    {
        var sut = JsonConvert.DeserializeObject<Point>(shape);
        var result = sut.GetExtent();

        result.HasValue.Should().BeTrue();
        result.Value.X_max.Should().Be(359170.44F);
        result.Value.Y_max.Should().Be(173119.12F);
        result.Value.Y_min.Should().Be(173119.12F);
        result.Value.X_max.Should().Be(359170.44F);
    }
}

