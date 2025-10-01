using FluentAssertions;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Moq;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Forestry.Flo.Services.FellingLicenceApplications.Tests.Services;

public class RetrieveApplicationsOnTheConsultationPublicRegisterAsyncTests
{
    private readonly Mock<IFellingLicenceApplicationInternalRepository> _repo = new();
    private readonly Mock<IClock> _clock = new();

    private GetFellingLicenceApplicationForInternalUsersService CreateSut()
    {
        _clock.Setup(x => x.GetCurrentInstant()).Returns(Instant.FromDateTimeUtc(DateTime.UtcNow));
        return new GetFellingLicenceApplicationForInternalUsersService(_repo.Object, _clock.Object);
    }

    [Fact]
    public async Task ReturnsEmptyList_WhenNoApplications()
    {
        _repo.Setup(r => r.GetApplicationsOnConsultationPublicRegisterPeriodsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FellingLicenceApplication>());

        var sut = CreateSut();
        var result = await sut.RetrieveApplicationsOnTheConsultationPublicRegisterAsync(CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ReturnsFilteredApplications_WithCorrectProperties()
    {
        var app1 = new FellingLicenceApplication
        {
            ApplicationReference = "REF1",
            PublicRegister = new PublicRegister
            {
                ConsultationPublicRegisterPublicationTimestamp = DateTime.UtcNow.AddDays(-2),
                ConsultationPublicRegisterRemovedTimestamp = null
            },
            SubmittedFlaPropertyDetail = new SubmittedFlaPropertyDetail { Name = "Prop1" },
            AdministrativeRegion = "Region1",
            AssigneeHistories = new List<AssigneeHistory>
            {
                new AssigneeHistory { AssignedUserId = Guid.NewGuid(), TimestampUnassigned = null, Role = AssignedUserRole.FieldManager }
            }
        };
        var app2 = new FellingLicenceApplication
        {
            ApplicationReference = "REF2",
            PublicRegister = new PublicRegister
            {
                ConsultationPublicRegisterPublicationTimestamp = DateTime.UtcNow.AddDays(-3),
                ConsultationPublicRegisterRemovedTimestamp = null
            },
            SubmittedFlaPropertyDetail = new SubmittedFlaPropertyDetail { Name = "Prop2" },
            AdministrativeRegion = "Region2",
            AssigneeHistories = new List<AssigneeHistory>
            {
                new AssigneeHistory { AssignedUserId = Guid.NewGuid(), TimestampUnassigned = null, Role = AssignedUserRole.FieldManager }
            }
        };

        var apps = new List<FellingLicenceApplication> { app1, app2 };

        _repo.Setup(r => r.GetApplicationsOnConsultationPublicRegisterPeriodsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(apps);

        var sut = CreateSut();
        var result = await sut.RetrieveApplicationsOnTheConsultationPublicRegisterAsync(CancellationToken.None);

        result.Count.Should().Be(2);
        result[0].ApplicationReference.Should().Be("REF1");
        result[0].PropertyName.Should().Be("Prop1");
        result[0].AdminHubName.Should().Be("Region1");
        result[0].AssignedUserIds.Should().Contain(app1.AssigneeHistories[0].AssignedUserId);

        result[1].ApplicationReference.Should().Be("REF2");
        result[1].PropertyName.Should().Be("Prop2");
        result[1].AdminHubName.Should().Be("Region2");
        result[1].AssignedUserIds.Should().Contain(app2.AssigneeHistories[0].AssignedUserId);
    }

    [Fact]
    public async Task FiltersOutApplications_WithoutPublicationTimestampOrWithRemovedTimestamp()
    {
        var app1 = new FellingLicenceApplication
        {
            ApplicationReference = "REF1",
            PublicRegister = new PublicRegister
            {
                ConsultationPublicRegisterPublicationTimestamp = null,
                ConsultationPublicRegisterRemovedTimestamp = null
            }
        };
        var app2 = new FellingLicenceApplication
        {
            ApplicationReference = "REF2",
            PublicRegister = new PublicRegister
            {
                ConsultationPublicRegisterPublicationTimestamp = DateTime.UtcNow,
                ConsultationPublicRegisterRemovedTimestamp = DateTime.UtcNow
            }
        };
        var app3 = new FellingLicenceApplication
        {
            ApplicationReference = "REF3",
            PublicRegister = new PublicRegister
            {
                ConsultationPublicRegisterPublicationTimestamp = DateTime.UtcNow,
                ConsultationPublicRegisterRemovedTimestamp = null
            }
        };

        var apps = new List<FellingLicenceApplication> { app1, app2, app3 };

        _repo.Setup(r => r.GetApplicationsOnConsultationPublicRegisterPeriodsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(apps);

        var sut = CreateSut();
        var result = await sut.RetrieveApplicationsOnTheConsultationPublicRegisterAsync(CancellationToken.None);

        result.Count.Should().Be(1);
        result[0].ApplicationReference.Should().Be("REF3");
    }
}
