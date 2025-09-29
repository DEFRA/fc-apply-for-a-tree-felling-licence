using AutoFixture;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Tests.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Forestry.Flo.Services.FellingLicenceApplications.Tests.Repositories;

public class InternalUserContextFlaRepositoryPublicRegisterTests
{
    private readonly InternalUserContextFlaRepository _sut;
    private readonly FellingLicenceApplicationsContext _fellingLicenceApplicationsContext;
    private static readonly Fixture FixtureInstance = new();

    public InternalUserContextFlaRepositoryPublicRegisterTests()
    {
        _fellingLicenceApplicationsContext = TestFellingLicenceApplicationsDatabaseFactory.CreateDefaultTestContext();
        _sut = new InternalUserContextFlaRepository(_fellingLicenceApplicationsContext);

        FixtureInstance.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => FixtureInstance.Behaviors.Remove(b));
        FixtureInstance.Behaviors.Add(new OmitOnRecursionBehavior());
    }

    [Theory, AutoMoqData]
    public async Task RetrievesApplications_WhenPublicRegisterEndDateExceeded(List<FellingLicenceApplication> applications)
    {
        var currentDate = DateTime.UtcNow;

        foreach (var application in applications)
        {
            application.PublicRegister!.ConsultationPublicRegisterRemovedTimestamp = null;
            application.PublicRegister.ConsultationPublicRegisterExpiryTimestamp = currentDate.AddDays(-1).Date;
            _fellingLicenceApplicationsContext.FellingLicenceApplications.Add(application);
            application.PublicRegister.WoodlandOfficerSetAsExemptFromConsultationPublicRegister = false;
        }

        await _fellingLicenceApplicationsContext.SaveEntitiesAsync(CancellationToken.None);

        var result =
            await _sut.GetApplicationsWithExpiredConsultationPublicRegisterPeriodsAsync(currentDate, CancellationToken.None);

        Assert.Equal(applications.Count, result.Count);
    }

    [Theory, AutoMoqData]
    public async Task RetrievesNoApplications_WhenAlreadyRemovedFromPublicRegister(List<FellingLicenceApplication> applications)
    {
        var currentDate = DateTime.UtcNow;

        foreach (var application in applications)
        {
            application.PublicRegister!.ConsultationPublicRegisterRemovedTimestamp = currentDate;
            application.PublicRegister.ConsultationPublicRegisterExpiryTimestamp = currentDate.AddDays(-1).Date;
            _fellingLicenceApplicationsContext.FellingLicenceApplications.Add(application);
        }

        await _fellingLicenceApplicationsContext.SaveEntitiesAsync(CancellationToken.None);

        var result =
            await _sut.GetApplicationsWithExpiredConsultationPublicRegisterPeriodsAsync(currentDate, CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task RetrievesApplications_WithMixtureOfApplicationsHavingExceeded()
    {
        var currentDate = DateTime.UtcNow;

        var applications = FixtureInstance.CreateMany<FellingLicenceApplication>(4).ToList();

        applications[0].PublicRegister!.ConsultationPublicRegisterRemovedTimestamp = null;
        applications[0].PublicRegister!.ConsultationPublicRegisterExpiryTimestamp = currentDate.AddDays(20);
        applications[0].PublicRegister!.WoodlandOfficerSetAsExemptFromConsultationPublicRegister = false;

        applications[1].PublicRegister!.ConsultationPublicRegisterRemovedTimestamp = currentDate;
        applications[1].PublicRegister!.ConsultationPublicRegisterExpiryTimestamp = currentDate.AddDays(20);
        applications[1].PublicRegister!.WoodlandOfficerSetAsExemptFromConsultationPublicRegister = false;

        applications[2].PublicRegister!.ConsultationPublicRegisterRemovedTimestamp = null;
        applications[2].PublicRegister!.ConsultationPublicRegisterExpiryTimestamp = currentDate.AddDays(-3);
        applications[2].PublicRegister!.WoodlandOfficerSetAsExemptFromConsultationPublicRegister = false;

        applications[3].PublicRegister!.ConsultationPublicRegisterPublicationTimestamp = null;
        applications[3].PublicRegister!.ConsultationPublicRegisterExpiryTimestamp = null;
        applications[3].PublicRegister!.ConsultationPublicRegisterRemovedTimestamp = null;
        applications[3].PublicRegister!.WoodlandOfficerSetAsExemptFromConsultationPublicRegister = true;

        foreach (var application in applications)
        {
            _fellingLicenceApplicationsContext.FellingLicenceApplications.Add(application);
        }

        await _fellingLicenceApplicationsContext.SaveEntitiesAsync(CancellationToken.None);

        var result =
            await _sut.GetApplicationsWithExpiredConsultationPublicRegisterPeriodsAsync(currentDate, CancellationToken.None);

        Assert.Single(result);
    }
}