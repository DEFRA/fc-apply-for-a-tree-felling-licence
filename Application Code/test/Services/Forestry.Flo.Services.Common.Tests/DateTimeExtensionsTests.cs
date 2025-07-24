using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AutoFixture.Xunit2;
using Forestry.Flo.Services.Common.Extensions;
using Forestry.Flo.Services.Common.User;
using Xunit;

namespace Forestry.Flo.Services.Common.Tests;

public class DateTimeExtensionsTests
{
    [Theory]
    [AutoData]
    public void CanHandleDateTime(DateTime dateTime)
    {
        var formattedDate = dateTime.CreateFormattedDate();

        Assert.NotNull(formattedDate);
    }

    [Theory]
    [AutoData]
    public void CanMakeAccurateDateTime()
    {
        var dateTime = new DateTime(2022, 06, 19);

        var formattedDate = dateTime.CreateFormattedDate();

        Assert.NotNull(formattedDate);
        Assert.True(formattedDate is "19th June 2022" or "19th Mehefin 2022");
    }

    [Theory]
    [AutoData]
    public void CanHandleEmptyDateTime()
    {
        var dateTime = new DateTime();

        var formattedDate = dateTime.CreateFormattedDate();

        Assert.NotNull(formattedDate);

    }
}