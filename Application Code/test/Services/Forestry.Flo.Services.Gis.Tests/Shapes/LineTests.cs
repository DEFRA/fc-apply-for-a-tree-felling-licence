using Forestry.Flo.Services.Gis.Models.Internal.MapObjects;
using Newtonsoft.Json;

namespace Forestry.Flo.Services.Gis.Tests.Shapes;
public class LineTests
{
        private string shape = "{\"spatialReference\": { \"latestWkid\": 27700, \"wkid\": 27700 },\"paths\": [  [[359143.2589656495, 173103.44454633517],[359166.5483727052, 173115.089249863],[359174.01292624866, 173115.38783200472]  ]]  }";
        [Fact]
        public void GetExtent_Test()
        {
            var sut = JsonConvert.DeserializeObject<Line>(shape);
            var result = sut.GetExtent();

            Assert.True(result.HasValue);
            Assert.Equal(359174F, result.Value.X_max);
            Assert.Equal(173115.39F, result.Value.Y_max);
            Assert.Equal(173103.44F, result.Value.Y_min);
            Assert.Equal(359143.25F, result.Value.X_min);
        }
    }
