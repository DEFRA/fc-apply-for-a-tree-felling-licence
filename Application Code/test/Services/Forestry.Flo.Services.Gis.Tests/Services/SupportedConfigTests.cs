using Forestry.Flo.Services.Gis.Services;
using Forestry.Flo.Services.Gis.Models.Esri.Configuration;

namespace Forestry.Flo.Services.Gis.Tests.Services;
public class SupportedConfigTests
{
    [Fact]
    public void GetSupportedFileTypes_ThrowsWhenAGOLNotSet()
    {
        var supportedConfig = new SupportedConfig(new EsriConfig() { });
        Assert.Throws<ArgumentNullException>(() => supportedConfig.GetSupportedFileTypes());
    }

    [Fact]
    public void GetSupportedFileTypes_ThrowsWhenFeaturesServiceNotSet()
    {
        var supportedConfig = new SupportedConfig(new EsriConfig() { Forestry = new ForestryConfig() });
        Assert.Throws<ArgumentNullException>(() => supportedConfig.GetSupportedFileTypes());
    }

    [Fact]
    public void GetSupportedFileTypes_ThrowsWhenGenerateServiceNotSet()
    {
        var supportedConfig = new SupportedConfig(new EsriConfig() { Forestry = new ForestryConfig() { FeaturesService = new FeaturesServiceSettings() } });
        Assert.Throws<ArgumentNullException>(() => supportedConfig.GetSupportedFileTypes());
    }

    [Fact]
    public void GetSupportedFileTypes_ThrowsWhenFileTypesNotSet()
    {
        var supportedConfig = new SupportedConfig(new EsriConfig() { Forestry = new ForestryConfig() { FeaturesService = new FeaturesServiceSettings() { GenerateService = new GenerateServiceSettings() } } });
        Assert.Throws<ArgumentNullException>(() => supportedConfig.GetSupportedFileTypes());
    }

    [Theory]
    [InlineData(".kml", ".shp")]
    public void GetSupportedFileTypesTest(params string[] filesStrings)
    {
        var controller = new SupportedConfig(new EsriConfig() {
            Forestry = new ForestryConfig() {
                FeaturesService = new FeaturesServiceSettings() {
                    GenerateService = new GenerateServiceSettings() {
                        SupportedFileImports = filesStrings
                    }
                }
            }
        });
        var result = controller.GetSupportedFileTypes();
        Assert.Equal(result, filesStrings);
    }
}
