using Newtonsoft.Json;
using Forestry.Flo.Services.Gis.Models.Internal.MapObjects;
namespace Forestry.Flo.Services.Gis.Tests.Shapes
{
    public class PolygonTests
    {
        private string shape = "{\"spatialReference\":{\"wkid\":27700},\"rings\": [[[359125.0454550035, 173149.57548723381],[359170.4299405478, 173168.08758002162],[359177.5959119495, 173171.073401439],[359176.998747666, 173171.073401439],[359179.98456908343, 173164.50459432075],[359184.76188335125, 173129.27190159555],[359180.5817333669, 173117.328615926],[359175.80441909906, 173128.0775730286],[359165.05546199647, 173126.88324446164],[359161.4724762956, 173128.67473731207],[359139.3773978069, 173118.52294449296],[359125.04545500345, 173149.57548723381]]]}";

        [Fact]
        public void GetExtent_Test()
        {
            var sut = JsonConvert.DeserializeObject<Polygon>(shape);
            var result = sut.GetExtent();

            Assert.True(result.HasValue);
            Assert.Equal(359184.75F, result.Value.X_max);
            Assert.Equal(173171.08F, result.Value.Y_max);
            Assert.Equal(173117.33F, result.Value.Y_min);
            Assert.Equal(359125.031F, result.Value.X_min);
        }
    }
}
