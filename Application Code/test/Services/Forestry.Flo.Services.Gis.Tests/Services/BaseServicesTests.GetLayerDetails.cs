﻿

using FluentAssertions;
using Forestry.Flo.Services.Gis.Models.Esri.Configuration;

namespace Forestry.Flo.Services.Gis.Tests.Services;

public partial class BaseServicesTests
{
    private readonly List<FeatureLayerConfig> _layerSettings =
    [
        new FeatureLayerConfig()
        {
            Name = "Country_Boundaries_Generalised", ServiceURI = "https://www.AGOL.com/Admin",
            Fields = [], NeedsToken = true
        },

        new FeatureLayerConfig()
        {
            Name = "Woodland_Officers", ServiceURI = "https://www.AGOL.com/WoodlandOfficer",
            Fields = [], NeedsToken = true
        },

        new FeatureLayerConfig()
        {
            Name = "LocalAuthority_Areas", ServiceURI = "https://www.AGOL.com/LA",
            Fields = [], NeedsToken = true
        },

        new FeatureLayerConfig()
        {
            Name = "SiteVisitCompartments", ServiceURI = "https://www.AGOL.com/SiteVisitCompartments",
            Fields = [], NeedsToken = true
        }

    ];

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData(" ")]
    public void GetLayerDetails_ShouldReturnNoneFromBadString(string value)
    {
        var sut = CreateSut(layerSettings: _layerSettings);
        var result = sut.GetLayerDetail(value);

        result.HasNoValue.Should().BeTrue();
    }

    [Theory]
    [InlineData(" Country_Boundaries_Generalised")]
    [InlineData("Country_Boundaries_Generalised ")]
    [InlineData(" Country_Boundaries_Generalised ")]
    [InlineData("Country_Boundaries_Generalised")]
    public void GetLayerDetails_ShouldReturnValue_FromSloppyDeveloper(string value)
    {
        var sut = CreateSut(layerSettings: _layerSettings);
        var result = sut.GetLayerDetail(value);

        result.HasNoValue.Should().BeFalse();
        result.Value.Name.Should().Be("Country_Boundaries_Generalised");
    }

    public void GetLayerDetails_ShouldReturnNoneFromNoMatch()
    {
        var sut = CreateSut(layerSettings: _layerSettings);
        var result = sut.GetLayerDetail("Not Here");

        result.HasNoValue.Should().BeFalse();
        result.Value.Name.Should().Be("Country_Boundaries_Generalised");
    }


    [Fact]
    public void GetLayerDetails_Success()
    {
        string expected = "Country_Boundaries_Generalised";
        var sut = CreateSut(layerSettings: _layerSettings);
        var result = sut.GetLayerDetail(expected);

        result.HasNoValue.Should().BeFalse();
        result.Value.Name.Should().Be(expected);
    }
}
