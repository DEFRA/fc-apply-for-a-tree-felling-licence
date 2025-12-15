using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Configuration;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using NodaTime;
using NodaTime.Testing;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Forestry.Flo.Services.FellingLicenceApplications.Tests.Services;

public class UpdateFellingLicenceApplicationForExternalUsersServiceUpdateApplicationPawsDesignationsDataTests
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
        UserAccessModel uam,
        PawsCompartmentDesignationsModel pawsDesignationsData)
    {
        var sut = CreateSut();

        var result = await sut.UpdateApplicationPawsDesignationsDataAsync(
            applicationId,
            uam,
            pawsDesignationsData,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
    }

    [Theory, AutoMoqData]
    public async Task WhenUserIsNotAuthorisedForApplication(
        PawsCompartmentDesignationsModel pawsDesignationsData,
        FellingLicenceApplication application)
    {
        var sut = CreateSut();

        application.FellingLicenceApplicationStepStatus.CompartmentDesignationsStatuses.Clear();
        application.FellingLicenceApplicationStepStatus.CompartmentDesignationsStatuses.Add(
            new CompartmentDesignationStatus
            {
                CompartmentId = pawsDesignationsData.PropertyProfileCompartmentId,
                Status = null
            }
        );

        application.LinkedPropertyProfile.ProposedCompartmentDesignations =
        [
            new ProposedCompartmentDesignations
            {
                LinkedPropertyProfile = application.LinkedPropertyProfile,
                LinkedPropertyProfileId = application.LinkedPropertyProfile.Id,
                CrossesPawsZones = ["ARW"],
                Id = pawsDesignationsData.Id,
                PropertyProfileCompartmentId = pawsDesignationsData.PropertyProfileCompartmentId
            }
        ];

        UserAccessModel uam = new UserAccessModel
        {
            AgencyId = Guid.NewGuid(),
            IsFcUser = false,
            UserAccountId = Guid.NewGuid(),
            WoodlandOwnerIds = [Guid.NewGuid()]
        };

        _fellingLicenceApplicationsContext!.Add(application);
        await _fellingLicenceApplicationsContext.SaveChangesAsync();


        var result = await sut.UpdateApplicationPawsDesignationsDataAsync(
            application.Id,
            uam,
            pawsDesignationsData,
            CancellationToken.None);

        _fellingLicenceApplicationsContext.ChangeTracker.Clear();

        Assert.False(result.IsSuccess);

        var updatedApplication =
            await _fellingLicenceApplicationRepository.GetAsync(application.Id, CancellationToken.None);

        Assert.Single(updatedApplication.Value.FellingLicenceApplicationStepStatus.CompartmentDesignationsStatuses);
        Assert.Null(updatedApplication.Value.FellingLicenceApplicationStepStatus.CompartmentDesignationsStatuses.Single().Status);

        Assert.Single(updatedApplication.Value.LinkedPropertyProfile.ProposedCompartmentDesignations);
        Assert.Null(updatedApplication.Value.LinkedPropertyProfile.ProposedCompartmentDesignations.Single().ProportionBeforeFelling);
        Assert.Null(updatedApplication.Value.LinkedPropertyProfile.ProposedCompartmentDesignations.Single().ProportionAfterFelling);
        Assert.Null(updatedApplication.Value.LinkedPropertyProfile.ProposedCompartmentDesignations.Single().IsRestoringCompartment);
        Assert.Null(updatedApplication.Value.LinkedPropertyProfile.ProposedCompartmentDesignations.Single().RestorationDetails);
    }

    [Theory, AutoMoqData]
    public async Task WhenThePawsDesignationsIdSpecifiedIsNotFoundOnTheApplication(
        PawsCompartmentDesignationsModel pawsDesignationsData,
        FellingLicenceApplication application)
    {
        var sut = CreateSut();

        application.FellingLicenceApplicationStepStatus.CompartmentDesignationsStatuses.Clear();
        application.FellingLicenceApplicationStepStatus.CompartmentDesignationsStatuses.Add(
            new CompartmentDesignationStatus
            {
                CompartmentId = pawsDesignationsData.PropertyProfileCompartmentId,
                Status = null
            }
        );

        application.LinkedPropertyProfile.ProposedCompartmentDesignations =
        [
            new ProposedCompartmentDesignations
            {
                LinkedPropertyProfile = application.LinkedPropertyProfile,
                LinkedPropertyProfileId = application.LinkedPropertyProfile.Id,
                CrossesPawsZones = ["ARW"],
                Id = Guid.NewGuid(),
                PropertyProfileCompartmentId = pawsDesignationsData.PropertyProfileCompartmentId
            }
        ];

        UserAccessModel uam = new UserAccessModel
        {
            AgencyId = Guid.NewGuid(),
            IsFcUser = false,
            UserAccountId = Guid.NewGuid(),
            WoodlandOwnerIds = [ application.WoodlandOwnerId ]
        };

        _fellingLicenceApplicationsContext!.Add(application);
        await _fellingLicenceApplicationsContext.SaveChangesAsync();


        var result = await sut.UpdateApplicationPawsDesignationsDataAsync(
            application.Id,
            uam,
            pawsDesignationsData,
            CancellationToken.None);

        _fellingLicenceApplicationsContext.ChangeTracker.Clear();

        Assert.False(result.IsSuccess);

        var updatedApplication =
            await _fellingLicenceApplicationRepository.GetAsync(application.Id, CancellationToken.None);

        Assert.Single(updatedApplication.Value.FellingLicenceApplicationStepStatus.CompartmentDesignationsStatuses);
        Assert.Null(updatedApplication.Value.FellingLicenceApplicationStepStatus.CompartmentDesignationsStatuses.Single().Status);

        Assert.Single(updatedApplication.Value.LinkedPropertyProfile.ProposedCompartmentDesignations);
        Assert.Null(updatedApplication.Value.LinkedPropertyProfile.ProposedCompartmentDesignations.Single().ProportionBeforeFelling);
        Assert.Null(updatedApplication.Value.LinkedPropertyProfile.ProposedCompartmentDesignations.Single().ProportionAfterFelling);
        Assert.Null(updatedApplication.Value.LinkedPropertyProfile.ProposedCompartmentDesignations.Single().IsRestoringCompartment);
        Assert.Null(updatedApplication.Value.LinkedPropertyProfile.ProposedCompartmentDesignations.Single().RestorationDetails);
    }

    [Theory, AutoMoqData]
    public async Task WhenThePawsDesignationsStepStatusIsNotFoundOnTheApplication(
        PawsCompartmentDesignationsModel pawsDesignationsData,
        FellingLicenceApplication application)
    {
        var sut = CreateSut();

        application.FellingLicenceApplicationStepStatus.CompartmentDesignationsStatuses.Clear();
        application.FellingLicenceApplicationStepStatus.CompartmentDesignationsStatuses.Add(
            new CompartmentDesignationStatus
            {
                CompartmentId = Guid.NewGuid(),
                Status = null
            }
        );

        application.LinkedPropertyProfile.ProposedCompartmentDesignations =
        [
            new ProposedCompartmentDesignations
            {
                LinkedPropertyProfile = application.LinkedPropertyProfile,
                LinkedPropertyProfileId = application.LinkedPropertyProfile.Id,
                CrossesPawsZones = ["ARW"],
                Id = pawsDesignationsData.Id,
                PropertyProfileCompartmentId = pawsDesignationsData.PropertyProfileCompartmentId
            }
        ];

        UserAccessModel uam = new UserAccessModel
        {
            AgencyId = Guid.NewGuid(),
            IsFcUser = false,
            UserAccountId = Guid.NewGuid(),
            WoodlandOwnerIds = [application.WoodlandOwnerId]
        };

        _fellingLicenceApplicationsContext!.Add(application);
        await _fellingLicenceApplicationsContext.SaveChangesAsync();

        _fellingLicenceApplicationsContext.ChangeTracker.Clear();

        var result = await sut.UpdateApplicationPawsDesignationsDataAsync(
            application.Id,
            uam,
            pawsDesignationsData,
            CancellationToken.None);

        _fellingLicenceApplicationsContext.ChangeTracker.Clear();

        Assert.False(result.IsSuccess);

        var updatedApplication =
            await _fellingLicenceApplicationRepository.GetAsync(application.Id, CancellationToken.None);

        Assert.Single(updatedApplication.Value.FellingLicenceApplicationStepStatus.CompartmentDesignationsStatuses);
        Assert.Null(updatedApplication.Value.FellingLicenceApplicationStepStatus.CompartmentDesignationsStatuses.Single().Status);

        Assert.Single(updatedApplication.Value.LinkedPropertyProfile.ProposedCompartmentDesignations);
        Assert.Null(updatedApplication.Value.LinkedPropertyProfile.ProposedCompartmentDesignations.Single().ProportionBeforeFelling);
        Assert.Null(updatedApplication.Value.LinkedPropertyProfile.ProposedCompartmentDesignations.Single().ProportionAfterFelling);
        Assert.Null(updatedApplication.Value.LinkedPropertyProfile.ProposedCompartmentDesignations.Single().IsRestoringCompartment);
        Assert.Null(updatedApplication.Value.LinkedPropertyProfile.ProposedCompartmentDesignations.Single().RestorationDetails);
    }

    [Theory, AutoMoqData]
    public async Task WhenThePawsDesignationsIsUpdatedSuccessfully(
        PawsCompartmentDesignationsModel pawsDesignationsData,
        FellingLicenceApplication application)
    {
        var sut = CreateSut();

        application.FellingLicenceApplicationStepStatus.CompartmentDesignationsStatuses.Clear();
        application.FellingLicenceApplicationStepStatus.CompartmentDesignationsStatuses.Add(
            new CompartmentDesignationStatus
            {
                CompartmentId = pawsDesignationsData.PropertyProfileCompartmentId,
                Status = null
            }
        );

        application.LinkedPropertyProfile.ProposedCompartmentDesignations =
        [
            new ProposedCompartmentDesignations
            {
                LinkedPropertyProfile = application.LinkedPropertyProfile,
                LinkedPropertyProfileId = application.LinkedPropertyProfile.Id,
                CrossesPawsZones = ["ARW"],
                Id = pawsDesignationsData.Id,
                PropertyProfileCompartmentId = pawsDesignationsData.PropertyProfileCompartmentId
            }
        ];

        UserAccessModel uam = new UserAccessModel
        {
            AgencyId = Guid.NewGuid(),
            IsFcUser = false,
            UserAccountId = Guid.NewGuid(),
            WoodlandOwnerIds = [application.WoodlandOwnerId]
        };

        _fellingLicenceApplicationsContext!.Add(application);
        await _fellingLicenceApplicationsContext.SaveChangesAsync();

        _fellingLicenceApplicationsContext.ChangeTracker.Clear();

        var result = await sut.UpdateApplicationPawsDesignationsDataAsync(
            application.Id,
            uam,
            pawsDesignationsData,
            CancellationToken.None);

        _fellingLicenceApplicationsContext.ChangeTracker.Clear();

        Assert.True(result.IsSuccess);

        var updatedApplication =
            await _fellingLicenceApplicationRepository.GetAsync(application.Id, CancellationToken.None);

        Assert.Single(updatedApplication.Value.FellingLicenceApplicationStepStatus.CompartmentDesignationsStatuses);
        Assert.True(updatedApplication.Value.FellingLicenceApplicationStepStatus.CompartmentDesignationsStatuses.Single().Status);

        Assert.Single(updatedApplication.Value.LinkedPropertyProfile.ProposedCompartmentDesignations);
        Assert.Equal(pawsDesignationsData.ProportionBeforeFelling, updatedApplication.Value.LinkedPropertyProfile.ProposedCompartmentDesignations.Single().ProportionBeforeFelling);
        Assert.Equal(pawsDesignationsData.ProportionAfterFelling, updatedApplication.Value.LinkedPropertyProfile.ProposedCompartmentDesignations.Single().ProportionAfterFelling);
        Assert.Equal(pawsDesignationsData.IsRestoringCompartment, updatedApplication.Value.LinkedPropertyProfile.ProposedCompartmentDesignations.Single().IsRestoringCompartment);
        if (pawsDesignationsData.IsRestoringCompartment is true)
        {
            Assert.Equal(pawsDesignationsData.RestorationDetails, updatedApplication.Value.LinkedPropertyProfile.ProposedCompartmentDesignations.Single().RestorationDetails);
        }
        else
        {
            Assert.Null(updatedApplication.Value.LinkedPropertyProfile.ProposedCompartmentDesignations.Single().RestorationDetails);
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