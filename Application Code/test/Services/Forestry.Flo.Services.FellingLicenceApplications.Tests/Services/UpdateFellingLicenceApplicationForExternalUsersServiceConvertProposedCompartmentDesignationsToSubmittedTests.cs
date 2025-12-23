using Forestry.Flo.Services.FellingLicenceApplications.Configuration;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using NodaTime.Testing;
using NodaTime;
using System;
using System.Linq;
using System.Text.Json;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using System.Threading.Tasks;
using System.Threading;
using Xunit;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using LinqKit;

namespace Forestry.Flo.Services.FellingLicenceApplications.Tests.Services;

public class UpdateFellingLicenceApplicationForExternalUsersServiceConvertProposedCompartmentDesignationsToSubmittedTests
{
    private IFellingLicenceApplicationExternalRepository? _fellingLicenceApplicationRepository;
    private FellingLicenceApplicationsContext? _fellingLicenceApplicationsContext;

    private readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [Theory, AutoMoqData]
    public async Task WhenCannotFindApplication(
        Guid applicationId,
        UserAccessModel uam)
    {
        var sut = CreateSut();

        var result = await sut.ConvertProposedCompartmentDesignationsToSubmittedAsync(
            applicationId,
            uam,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
    }

    [Theory, AutoMoqData]
    public async Task WhenApplicationDoesNotHaveASubmittedFlaPropertyRecord(
        FellingLicenceApplication application)
    {
        var sut = CreateSut();

        application.LinkedPropertyProfile.ProposedCompartmentDesignations =
        [
            new ProposedCompartmentDesignations
            {
                LinkedPropertyProfile = application.LinkedPropertyProfile,
                LinkedPropertyProfileId = application.LinkedPropertyProfile.Id,
                CrossesPawsZones = ["ARW"],
                Id = Guid.NewGuid(),
                PropertyProfileCompartmentId = application.SubmittedFlaPropertyDetail.SubmittedFlaPropertyCompartments.First().CompartmentId,
            }
        ];

        application.SubmittedFlaPropertyDetail = null;

        UserAccessModel uam = new UserAccessModel
        {
            AgencyId = Guid.NewGuid(),
            IsFcUser = false,
            UserAccountId = Guid.NewGuid(),
            WoodlandOwnerIds = [application.WoodlandOwnerId]
        };

        _fellingLicenceApplicationsContext!.Add(application);
        await _fellingLicenceApplicationsContext.SaveChangesAsync();

        var result = await sut.ConvertProposedCompartmentDesignationsToSubmittedAsync(
            application.Id,
            uam,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
    }

    [Theory, AutoMoqData]
    public async Task WhenUserIsNotAuthorisedForApplication(
        FellingLicenceApplication application)
    {
        var sut = CreateSut();

        application.LinkedPropertyProfile.ProposedCompartmentDesignations =
        [
            new ProposedCompartmentDesignations
            {
                LinkedPropertyProfile = application.LinkedPropertyProfile,
                LinkedPropertyProfileId = application.LinkedPropertyProfile.Id,
                CrossesPawsZones = ["ARW"],
                Id = Guid.NewGuid(),
                PropertyProfileCompartmentId = application.SubmittedFlaPropertyDetail.SubmittedFlaPropertyCompartments.First().CompartmentId,
            }
        ];

        application.SubmittedFlaPropertyDetail.SubmittedFlaPropertyCompartments
            .ForEach(x => x.SubmittedCompartmentDesignations = null);

        UserAccessModel uam = new UserAccessModel
        {
            AgencyId = Guid.NewGuid(),
            IsFcUser = false,
            UserAccountId = Guid.NewGuid(),
            WoodlandOwnerIds = [Guid.NewGuid()]
        };

        _fellingLicenceApplicationsContext!.Add(application);
        await _fellingLicenceApplicationsContext.SaveChangesAsync();

        var result = await sut.ConvertProposedCompartmentDesignationsToSubmittedAsync(
            application.Id,
            uam,
            CancellationToken.None);

        Assert.False(result.IsSuccess);

        var updatedApplication =
            await _fellingLicenceApplicationRepository.GetAsync(application.Id, CancellationToken.None);

        foreach (var submittedCompartment in 
                 updatedApplication.Value.SubmittedFlaPropertyDetail.SubmittedFlaPropertyCompartments)
        {
            Assert.Null(submittedCompartment.SubmittedCompartmentDesignations);
        }
    }

    [Theory, AutoMoqData]
    public async Task WhenApplicationDoesNotHaveAnyProposedDesignationRecords(
        FellingLicenceApplication application)
    {
        var sut = CreateSut();

        application.LinkedPropertyProfile.ProposedCompartmentDesignations = [];

        UserAccessModel uam = new UserAccessModel
        {
            AgencyId = Guid.NewGuid(),
            IsFcUser = false,
            UserAccountId = Guid.NewGuid(),
            WoodlandOwnerIds = [application.WoodlandOwnerId]
        };

        application.SubmittedFlaPropertyDetail.SubmittedFlaPropertyCompartments
            .ForEach(x => x.SubmittedCompartmentDesignations = null);

        _fellingLicenceApplicationsContext!.Add(application);
        await _fellingLicenceApplicationsContext.SaveChangesAsync();

        var result = await sut.ConvertProposedCompartmentDesignationsToSubmittedAsync(
            application.Id,
            uam,
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        var updatedApplication =
            await _fellingLicenceApplicationRepository.GetAsync(application.Id, CancellationToken.None);

        foreach (var submittedCompartment in
                 updatedApplication.Value.SubmittedFlaPropertyDetail.SubmittedFlaPropertyCompartments)
        {
            Assert.Null(submittedCompartment.SubmittedCompartmentDesignations);
        }
    }

    [Theory, AutoMoqData]
    public async Task WhenApplicationDoesHaveProposedDesignationRecords(
        FellingLicenceApplication application)
    {
        var sut = CreateSut();

        var cptWithDesignations = 
            application.SubmittedFlaPropertyDetail.SubmittedFlaPropertyCompartments.First().CompartmentId;
        
        application.LinkedPropertyProfile.ProposedCompartmentDesignations = 
        [
            new ProposedCompartmentDesignations
            {
                LinkedPropertyProfile = application.LinkedPropertyProfile,
                LinkedPropertyProfileId = application.LinkedPropertyProfile.Id,
                CrossesPawsZones = ["ARW"],
                Id = Guid.NewGuid(),
                PropertyProfileCompartmentId = cptWithDesignations
            }
        ];

        application.SubmittedFlaPropertyDetail.SubmittedFlaPropertyCompartments
            .ForEach(x => x.SubmittedCompartmentDesignations = null);

        UserAccessModel uam = new UserAccessModel
        {
            AgencyId = Guid.NewGuid(),
            IsFcUser = false,
            UserAccountId = Guid.NewGuid(),
            WoodlandOwnerIds = [application.WoodlandOwnerId]
        };

        _fellingLicenceApplicationsContext!.Add(application);
        await _fellingLicenceApplicationsContext.SaveChangesAsync();

        var result = await sut.ConvertProposedCompartmentDesignationsToSubmittedAsync(
            application.Id,
            uam,
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        var updatedApplication =
            await _fellingLicenceApplicationRepository.GetAsync(application.Id, CancellationToken.None);

        foreach (var submittedCompartment in
                 updatedApplication.Value.SubmittedFlaPropertyDetail.SubmittedFlaPropertyCompartments)
        {
            if (submittedCompartment.CompartmentId == cptWithDesignations)
            {
                Assert.NotNull(submittedCompartment.SubmittedCompartmentDesignations);
                var designation = submittedCompartment.SubmittedCompartmentDesignations;
                var proposedDesignation =
                    application.LinkedPropertyProfile.ProposedCompartmentDesignations
                    .First(x => x.PropertyProfileCompartmentId == cptWithDesignations);
                Assert.Equal(proposedDesignation.CrossesPawsZones.Any(), designation.Paws);
                Assert.Equal(proposedDesignation.ProportionAfterFelling, designation.ProportionAfterFelling);
                Assert.Equal(proposedDesignation.ProportionBeforeFelling, designation.ProportionBeforeFelling);
            }
            else
            {
                Assert.Null(submittedCompartment.SubmittedCompartmentDesignations);
            }
        }
    }

    private UpdateFellingLicenceApplicationForExternalUsersService CreateSut()
    {
        _fellingLicenceApplicationsContext = TestFellingLicenceApplicationsDatabaseFactory.CreateDefaultTestContext();
        _fellingLicenceApplicationRepository = new ExternalUserContextFlaRepository(
            _fellingLicenceApplicationsContext,
            new Mock<IApplicationReferenceHelper>().Object,
            new Mock<IFellingLicenceApplicationReferenceRepository>().Object);

        return new UpdateFellingLicenceApplicationForExternalUsersService(
            _fellingLicenceApplicationRepository,
            new FakeClock(Instant.FromDateTimeUtc(DateTime.Now.ToUniversalTime())),
            new OptionsWrapper<FellingLicenceApplicationOptions>(new FellingLicenceApplicationOptions()),
            new NullLogger<UpdateFellingLicenceApplicationForExternalUsersService>());
    }
}