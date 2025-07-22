using System;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Tests.Common;
using Xunit;

namespace Forestry.Flo.Services.FellingLicenceApplications.Tests;

public class ApplicationReferenceHelperTests
{
    private readonly ApplicationReferenceHelper SUT = new();

    [Theory, AutoMoqData]
    public void GenerateReferenceNumber_ShouldReturnCorrectFormat(FellingLicenceApplication application)
    {
        var result = SUT.GenerateReferenceNumber(application, 1, null, null);

        var expected = $"---/001/{application.CreatedTimestamp.Year}";
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(1, "---/001/2023")]
    [InlineData(10, "---/010/2023")]
    [InlineData(100, "---/100/2023")]
    public void GenerateReferenceNumber_ShouldFormatToLeadingZeros(int counter, string expected)
    {
        var application = new FellingLicenceApplication {
            CreatedTimestamp = new DateTime(2023, 1, 1)
        };
        var result = SUT.GenerateReferenceNumber(application, counter, null, null);
        Assert.Equal(expected, result);
    }

    [Theory, AutoMoqData]
    public void GenerateReferenceNumber_ShouldAddPostFix(FellingLicenceApplication application)
    {
        var result = SUT.GenerateReferenceNumber(application, 1, "test", null);
        var expected = $"---/001/{application.CreatedTimestamp.Year}/test";

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(1,null, "---/001/2023")]
    [InlineData(1, 100, "---/101/2023")]
    [InlineData(10,100 ,"---/110/2023")]
    [InlineData(100, 5000, "---/5100/2023")]
    public void GenerateReferenceNumber_ShouldReturnCorrectFormatWithOffset(int counter, int? offset, string expected)
    {
        var application = new FellingLicenceApplication {
            CreatedTimestamp = new DateTime(2023, 1, 1)
        };
        var result = SUT.GenerateReferenceNumber(application, counter, null, offset);
        Assert.Equal(expected, result);
    }


    [Theory]
    [InlineData("ABC/123/2023", "XYZ", "XYZ/123/2023")]
    [InlineData("DEF/456/2022", "GHI", "GHI/456/2022")]
    [InlineData("JKL/789/2021", "MNO", "MNO/789/2021")]
    public void UpdateReferenceNumber_ShouldUpdatePrefix(string reference, string prefix, string expected)
    {
        var result = SUT.UpdateReferenceNumber(reference, prefix);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void UpdateReferenceNumber_ShouldHandleEmptyReference()
    {
        var reference = "";
        var prefix = "XYZ";

        var result = SUT.UpdateReferenceNumber(reference, prefix);

        Assert.Equal("XYZ/", result);
    }

    [Fact]
    public void UpdateReferenceNumber_ShouldHandleEmptyPrefix()
    {
        var reference = "ABC/123/2023";
        var prefix = "";

        var result = SUT.UpdateReferenceNumber(reference, prefix);

        Assert.Equal("/123/2023", result);
    }


    [Fact]
    public void UpdateReferenceNumber_ShouldHandleNullPrefix()
    {
        var reference = "ABC/123/2023";
        string? prefix = null;

        var result = SUT.UpdateReferenceNumber(reference, prefix!);

        Assert.Equal("/123/2023", result);
    }
}
