using System.Globalization;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Gis;
using Forestry.Flo.Services.Gis.Models.Internal.MapObjects;
using Forestry.Flo.Services.Gis.Models.Internal.Request;

namespace Forestry.Flo.Services.Gis.Tests
{
    public class ShapeHelperTests
    {

        [Fact]
        public void ConvertShape_ReturnsNoneWhenNull()
        {
#pragma warning disable CS8625
            Assert.Throws<ArgumentNullException>(() => ShapeHelper.ConvertShape(null));
#pragma warning restore CS8625
        }

        [Fact]
        public void ConvertShape_ReturnsNoneWhenEmpty()
        {
            var val = new FlowShape<string> { ShapeDetails = "" };
            var resx = ShapeHelper.ConvertShape(val);

            Assert.Equal(Maybe.None, resx);
        }

        [Fact]
        public void ConvertShape_ReturnsNoneWhenTypeIsNullEmpty()
        {
            var val = new FlowShape<string> { ShapeDetails = "Not empty" };
            var resx = ShapeHelper.ConvertShape(val);

            Assert.Equal(Maybe.None, resx);
        }

        [Fact]
        public void ConvertShape_ReturnsPolygon()
        {
            var val = new
                FlowShape<string>
            {
                ShapeDetails =
                        "{\"spatialReference\":{\"latestWkid\":27700,\"wkid\":27700},\"rings\":[[[415600.22153238964,185159.7867313574],[415592.28401651455,185081.83371086774],[415591.8209947553,185082.59438947268],[415552.7287290708,185105.81162340715],[415592.1186516007,185082.89204631778],[415600.22153238964,185159.7867313574]]]}",
                ShapeType = "POLYGON"
            };
            var resx = ShapeHelper.ConvertShape(val);

            Assert.IsType<Polygon>(resx.Value.ShapeDetails);
        }

        [Fact]
        public void ConvertShape_ReturnLine()
        {
            var val = new
                FlowShape<string>
            {
                ShapeDetails =
                        "{\"spatialReference\":{\"latestWkid\":27700,\"wkid\":27700},\"paths\":[[[414793.94007884094,184988.3515220493],[414860.41677429446,184755.1869932202],[414767.1509627627,184675.8118344697]]]}",
                ShapeType = "polyLine"
            };
            var resx = ShapeHelper.ConvertShape(val);

            Assert.IsType<Line>(resx.Value.ShapeDetails);

        }

        [Fact]
        public void ConvertShape_ReturnPoint()
        {
            var val = new
                FlowShape<string>
            {
                ShapeDetails = "{\"spatialReference\":{\"latestWkid\":27700,\"wkid\":27700},\"x\":414455.27571093803,\"y\":184971.48362504947}",
                ShapeType = "point"
            };
            var resx = ShapeHelper.ConvertShape(val);

            Assert.IsType<Point>(resx.Value.ShapeDetails);
        }

        [Fact]
        public void ConvertShape_FailsWhenNotSetCorrectly()
        {
            var val = new
                FlowShape<string>
            {
                ShapeDetails = "{\"spatialReference\":{\"latestWkid\":27700,\"wkid\":27700},\"t\":414455.27571093803,\"y\":184971.48362504947}",
                ShapeType = "point"
            };

            var resx = ShapeHelper.ConvertShape(val);

            Assert.True(resx.HasNoValue);
        }
    }
}
