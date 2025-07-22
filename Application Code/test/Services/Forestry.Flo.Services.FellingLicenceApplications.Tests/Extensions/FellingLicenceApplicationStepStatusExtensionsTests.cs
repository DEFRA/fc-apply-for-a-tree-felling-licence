using AutoFixture;
using FluentAssertions;
using Forestry.Flo.Services.Common.Extensions;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Extensions;
using System;
using System.Collections.Generic;
using Xunit;

namespace Forestry.Flo.Services.FellingLicenceApplications.Tests.Extensions;

public class FellingLicenceApplicationStepStatusExtensionsTests
{
    private static readonly Fixture Fixture = new();

    [Fact]
    public void ReturnsNull_IfBothFellingAndRestockingAreNotStarted()
    {
        var status = CreateStatus(null, null, null, null);

        var result = status.OverallCompletion();

        result.HasValue.Should().BeFalse();
    }

    [Theory, CombinatorialData]
    public void ReturnsCorrectValue_IfBothFellingAndRestockingAreNotStarted(bool? compartmentStatus, bool? fellingStatus, bool? restockingCompartmentStatus, bool? restockingStatus)
    {
        var status = CreateStatus(compartmentStatus, fellingStatus, restockingCompartmentStatus, restockingStatus);

        var result = status.OverallCompletion();

        if (compartmentStatus is null && fellingStatus is null)
        {
            result.HasNoValue().Should().BeTrue();
        }
        else
        {
            var expectedValue = compartmentStatus.HasValue && compartmentStatus.Value
                && fellingStatus.HasValue && fellingStatus.Value
                && restockingCompartmentStatus.HasValue && restockingCompartmentStatus.Value
                && restockingStatus.HasValue && restockingStatus.Value;

            result.HasValue.Should().BeTrue();
            result!.Value.Should().Be(expectedValue);
        }
    }

    private CompartmentFellingRestockingStatus CreateStatus(bool? compartmentStatus, bool? fellingStatus, bool? restockingCompartmentStatus, bool? restockStatus)
    {
        var status = Fixture.Build<CompartmentFellingRestockingStatus>().Create();

        status.Status = compartmentStatus;
        status.FellingStatuses = new List<FellingStatus>()
        {
            new FellingStatus
            {
                Id = Guid.NewGuid(),
                Status = fellingStatus,
                RestockingCompartmentStatuses = new List<RestockingCompartmentStatus>()
                {
                    new RestockingCompartmentStatus
                    {
                        Status = restockingCompartmentStatus,
                        RestockingStatuses = new List<RestockingStatus>()
                        {
                            new RestockingStatus
                            {
                                Status = restockStatus
                            }
                        }
                    }
                }
            }
        };

        return status;
    }
}