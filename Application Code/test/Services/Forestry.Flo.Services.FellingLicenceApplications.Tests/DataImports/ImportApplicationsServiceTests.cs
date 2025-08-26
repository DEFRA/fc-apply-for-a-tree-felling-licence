using AutoFixture;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.MassTransit.Messages;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Configuration;
using Forestry.Flo.Services.FellingLicenceApplications.DataImports;
using Forestry.Flo.Services.FellingLicenceApplications.DataImports.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Extensions;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.PropertyProfiles.DataImports;
using Forestry.Flo.Tests.Common;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Forestry.Flo.Services.FellingLicenceApplications.Tests.DataImports;

public class ImportApplicationsServiceTests
{
    private ExternalUserContextFlaRepository _repository;
    private FellingLicenceApplicationsContext _fellingLicenceApplicationsContext;
    private readonly Mock<IApplicationReferenceHelper> _mockReferenceGenerator = new();
    private readonly Mock<IFellingLicenceApplicationReferenceRepository> _mockReferenceRepository = new();
    private readonly Mock<IClock> _mockClock = new();
    private readonly Instant _now = new();
    private readonly Mock<IBus> _mockBus = new();
    private RequestContext Context;
    private readonly Mock<IAuditService<ImportApplicationsService>> _mockAuditService = new();
    private const string ReferenceSuffix = "IMPORT";

    private static Fixture FixtureInstance = new();

    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [Theory, AutoMoqData]
    public async Task WithNoApplicationsToImport(
        Guid userId,
        Guid woodlandOwnerId)
    {
        var sut = CreateSut();

        var request = new ImportApplicationsRequest {
            UserId = userId,
            WoodlandOwnerId = woodlandOwnerId,
            PropertyIds = [],
            ApplicationRecords = [],
            FellingRecords = [],
            RestockingRecords = [],
        };

        var result = await sut.RunDataImportAsync(request, Context, CancellationToken.None);

        Assert.True(result.IsSuccess);

        Assert.Empty(_fellingLicenceApplicationsContext.FellingLicenceApplications);

        _mockClock.Verify(x => x.GetCurrentInstant(), Times.Once);
        _mockClock.VerifyNoOtherCalls();

        _mockReferenceGenerator.VerifyNoOtherCalls();
        _mockReferenceRepository.VerifyNoOtherCalls();
        _mockBus.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WithSingleApplicationToImportWithNoFellingOrRestocking(
        Guid userId,
        Guid woodlandOwnerId,
        PropertyIds property,
        string expectedReference,
        long expectedReferenceNumber)
    {
        var sut = CreateSut();

        _mockReferenceRepository
            .Setup(x => x.GetNextApplicationReferenceIdValueAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedReferenceNumber);
        _mockReferenceGenerator
            .Setup(r => r.GenerateReferenceNumber(It.IsAny<FellingLicenceApplication>(), It.IsAny<long>(), It.IsAny<string>(), It.IsAny<int>()))
            .Returns(expectedReference);

        var applicationToImport = GenerateValidApplication(property);

        var request = new ImportApplicationsRequest
        {
            UserId = userId,
            WoodlandOwnerId = woodlandOwnerId,
            PropertyIds = [property],
            ApplicationRecords = [applicationToImport],
            FellingRecords = [],
            RestockingRecords = [],
        };

        var result = await sut.RunDataImportAsync(request, Context, CancellationToken.None);

        Assert.True(result.IsSuccess);

        Assert.Single(_fellingLicenceApplicationsContext.FellingLicenceApplications);

        _mockClock.Verify(x => x.GetCurrentInstant(), Times.Once);
        _mockClock.VerifyNoOtherCalls();

        _mockReferenceGenerator.Verify(x => x.GenerateReferenceNumber(It.IsAny<FellingLicenceApplication>(), expectedReferenceNumber, ReferenceSuffix, It.IsAny<int>()), Times.Once);
        _mockReferenceGenerator.VerifyNoOtherCalls();

        _mockReferenceRepository.Verify(x => x.GetNextApplicationReferenceIdValueAsync(_now.ToDateTimeUtc().Year, It.IsAny<CancellationToken>()), Times.Once);

        _mockBus.VerifyNoOtherCalls();   // shouldn't publish any messages as no felling details are present so compartments aren't selected

        var applicationEntity = await _repository.GetAsync(
            _fellingLicenceApplicationsContext.FellingLicenceApplications.Single().Id,
            CancellationToken.None);

        AssertApplication(
            applicationEntity.Value,
            woodlandOwnerId,
            property,
            applicationToImport,
            expectedReference,
            userId,
            [],
            []);

        VerifyAudit(applicationEntity.Value.Id, woodlandOwnerId, userId);
        _mockAuditService.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WithSingleApplicationToImportWithSingleFellingAndRestocking(
        Guid userId,
        Guid woodlandOwnerId,
        PropertyIds property,
        string expectedReference,
        long expectedReferenceNumber)
    {
        var sut = CreateSut();

        _mockReferenceRepository
            .Setup(x => x.GetNextApplicationReferenceIdValueAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedReferenceNumber);
        _mockReferenceGenerator
            .Setup(r => r.GenerateReferenceNumber(It.IsAny<FellingLicenceApplication>(), It.IsAny<long>(), It.IsAny<string>(), It.IsAny<int>()))
            .Returns(expectedReference);

        var applicationToImport = GenerateValidApplication(property);
        var felling = GenerateValidFelling(property, applicationToImport, 1);
        var restocking = GenerateValidRestocking(property, applicationToImport, felling.Single(), 1);

        var request = new ImportApplicationsRequest
        {
            UserId = userId,
            WoodlandOwnerId = woodlandOwnerId,
            PropertyIds = [property],
            ApplicationRecords = [applicationToImport],
            FellingRecords = felling,
            RestockingRecords = restocking,
        };

        var result = await sut.RunDataImportAsync(request, Context, CancellationToken.None);

        Assert.True(result.IsSuccess);

        Assert.Single(_fellingLicenceApplicationsContext.FellingLicenceApplications);

        _mockClock.Verify(x => x.GetCurrentInstant(), Times.Once);
        _mockClock.VerifyNoOtherCalls();

        _mockReferenceGenerator.Verify(x => x.GenerateReferenceNumber(It.IsAny<FellingLicenceApplication>(), expectedReferenceNumber, ReferenceSuffix, It.IsAny<int>()), Times.Once);
        _mockReferenceGenerator.VerifyNoOtherCalls();

        _mockReferenceRepository.Verify(x => x.GetNextApplicationReferenceIdValueAsync(_now.ToDateTimeUtc().Year, It.IsAny<CancellationToken>()), Times.Once);

        var applicationEntity = await _repository.GetAsync(
            _fellingLicenceApplicationsContext.FellingLicenceApplications.Single().Id,
            CancellationToken.None);

        _mockBus.Verify(x => x.Publish(
            It.Is<CentrePointCalculationMessage>(c => c.UserId == userId && c.WoodlandOwnerId == woodlandOwnerId && c.ApplicationId == applicationEntity.Value.Id), 
            It.IsAny<CancellationToken>()), Times.Once);

        AssertApplication(
            applicationEntity.Value,
            woodlandOwnerId,
            property,
            applicationToImport,
            expectedReference,
            userId,
            felling,
            restocking);

        VerifyAudit(applicationEntity.Value.Id, woodlandOwnerId, userId);
        _mockAuditService.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WithSingleApplicationToImportWithMultipleFellingAndRestocking(
        Guid userId,
        Guid woodlandOwnerId,
        PropertyIds property,
        string expectedReference,
        long expectedReferenceNumber)
    {
        var sut = CreateSut();

        _mockReferenceRepository
            .Setup(x => x.GetNextApplicationReferenceIdValueAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedReferenceNumber);
        _mockReferenceGenerator
            .Setup(r => r.GenerateReferenceNumber(It.IsAny<FellingLicenceApplication>(), It.IsAny<long>(), It.IsAny<string>(), It.IsAny<int>()))
            .Returns(expectedReference);

        var applicationToImport = GenerateValidApplication(property);
        var felling = GenerateValidFelling(property, applicationToImport);
        List<ProposedRestockingSource> restocking = [];

        foreach (var fellingSource in felling)
        {
            restocking.AddRange(GenerateValidRestocking(property, applicationToImport, fellingSource));
        }

        var request = new ImportApplicationsRequest
        {
            UserId = userId,
            WoodlandOwnerId = woodlandOwnerId,
            PropertyIds = [property],
            ApplicationRecords = [applicationToImport],
            FellingRecords = felling,
            RestockingRecords = restocking,
        };

        var result = await sut.RunDataImportAsync(request, Context, CancellationToken.None);

        Assert.True(result.IsSuccess);

        Assert.Single(_fellingLicenceApplicationsContext.FellingLicenceApplications);

        _mockClock.Verify(x => x.GetCurrentInstant(), Times.Once);
        _mockClock.VerifyNoOtherCalls();

        _mockReferenceGenerator.Verify(x => x.GenerateReferenceNumber(It.IsAny<FellingLicenceApplication>(), expectedReferenceNumber, ReferenceSuffix, It.IsAny<int>()), Times.Once);
        _mockReferenceGenerator.VerifyNoOtherCalls();

        _mockReferenceRepository.Verify(x => x.GetNextApplicationReferenceIdValueAsync(_now.ToDateTimeUtc().Year, It.IsAny<CancellationToken>()), Times.Once);

        var applicationEntity = await _repository.GetAsync(
            _fellingLicenceApplicationsContext.FellingLicenceApplications.Single().Id,
            CancellationToken.None);

        _mockBus.Verify(x => x.Publish(
            It.Is<CentrePointCalculationMessage>(c => c.UserId == userId && c.WoodlandOwnerId == woodlandOwnerId && c.ApplicationId == applicationEntity.Value.Id),
            It.IsAny<CancellationToken>()), Times.Once);

        AssertApplication(
            applicationEntity.Value,
            woodlandOwnerId,
            property,
            applicationToImport,
            expectedReference,
            userId,
            felling,
            restocking);

        VerifyAudit(applicationEntity.Value.Id, woodlandOwnerId, userId);
        _mockAuditService.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WithMultipleApplicationsToImportWithMultipleFellingAndRestocking(
        Guid userId,
        Guid woodlandOwnerId,
        PropertyIds[] properties,
        string expectedReference,
        long expectedReferenceNumber)
    {
        var sut = CreateSut();

        _mockReferenceRepository
            .Setup(x => x.GetNextApplicationReferenceIdValueAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedReferenceNumber);
        _mockReferenceGenerator
            .Setup(r => r.GenerateReferenceNumber(It.IsAny<FellingLicenceApplication>(), It.IsAny<long>(), It.IsAny<string>(), It.IsAny<int>()))
            .Returns(expectedReference);

        List<ApplicationSource> applicationsToImport = [];

        foreach (var property in properties)
        {
            applicationsToImport.Add(GenerateValidApplication(property));
        }

        List<ProposedFellingSource> felling = [];
        List<ProposedRestockingSource> restocking = [];

        foreach (var applicationToImport in applicationsToImport)
        {
            var property = properties.FirstOrDefault(p => p.Name.Equals(applicationToImport.Flov2PropertyName, StringComparison.InvariantCultureIgnoreCase));
            var nextFelling = GenerateValidFelling(property, applicationToImport);
            felling.AddRange(nextFelling);

            foreach (var fellingSource in nextFelling)
            {
                restocking.AddRange(GenerateValidRestocking(property, applicationToImport, fellingSource));
            }
        }

        var request = new ImportApplicationsRequest
        {
            UserId = userId,
            WoodlandOwnerId = woodlandOwnerId,
            PropertyIds = properties.ToList(),
            ApplicationRecords = applicationsToImport,
            FellingRecords = felling,
            RestockingRecords = restocking,
        };

        var result = await sut.RunDataImportAsync(request, Context, CancellationToken.None);

        Assert.True(result.IsSuccess);

        Assert.Equal(applicationsToImport.Count(), _fellingLicenceApplicationsContext.FellingLicenceApplications.Count());

        _mockClock.Verify(x => x.GetCurrentInstant(), Times.Once);
        _mockClock.VerifyNoOtherCalls();

        _mockReferenceGenerator.Verify(x => x.GenerateReferenceNumber(It.IsAny<FellingLicenceApplication>(), expectedReferenceNumber, ReferenceSuffix, It.IsAny<int>()), Times.Exactly(applicationsToImport.Count()));
        _mockReferenceGenerator.VerifyNoOtherCalls();

        _mockReferenceRepository.Verify(x => x.GetNextApplicationReferenceIdValueAsync(_now.ToDateTimeUtc().Year, It.IsAny<CancellationToken>()), Times.Exactly(applicationsToImport.Count()));

        foreach (var applicationSource in applicationsToImport)
        {
            var applicationProperty = properties.Single(p => p.Name.Equals(applicationSource.Flov2PropertyName, StringComparison.InvariantCultureIgnoreCase));

            var applicationId = _fellingLicenceApplicationsContext
                .FellingLicenceApplications
                .Single(x => x.LinkedPropertyProfile.PropertyProfileId == applicationProperty.Id)
                .Id;

            var applicationEntity = await _repository.GetAsync(
                applicationId,
                CancellationToken.None);

            _mockBus.Verify(x => x.Publish(
                It.Is<CentrePointCalculationMessage>(c => c.UserId == userId && c.WoodlandOwnerId == woodlandOwnerId && c.ApplicationId == applicationEntity.Value.Id),
                It.IsAny<CancellationToken>()), Times.Once);

            var matchedFellingSource = felling.Where(x => x.ApplicationId == applicationSource.ApplicationId);
            var matchedRestockingSource = restocking.Where(x => matchedFellingSource.Select(f => f.ProposedFellingId).Contains(x.ProposedFellingId));

            AssertApplication(
                applicationEntity.Value,
                woodlandOwnerId,
                applicationProperty,
                applicationSource,
                expectedReference,
                userId,
                matchedFellingSource,
                matchedRestockingSource);

            VerifyAudit(applicationEntity.Value.Id, woodlandOwnerId, userId);
        }
        _mockAuditService.VerifyNoOtherCalls();
    }

    private ImportApplicationsService CreateSut(bool busThrows = false)
    {
        FixtureInstance.Customize<DateOnly>(composer => composer.FromFactory<DateTime>(DateOnly.FromDateTime));

        _mockClock.Reset();
        _mockClock.Setup(x => x.GetCurrentInstant()).Returns(_now);

        _mockReferenceRepository.Reset();
        _mockReferenceGenerator.Reset();

        _mockAuditService.Reset();
        Context = new RequestContext(
            Guid.NewGuid().ToString(),
            new RequestUserModel(
                UserFactory.CreateExternalApplicantIdentityProviderClaimsPrincipal(localAccountId: Guid.NewGuid())));

        _mockBus.Reset();
        if (busThrows)
        {
            _mockBus.Setup(x => x.Publish(It.IsAny<CentrePointCalculationMessage>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new MassTransitException("error"));
        }
        else
        {
            _mockBus.Setup(x => x.Publish(It.IsAny<CentrePointCalculationMessage>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
        }

        _fellingLicenceApplicationsContext = TestFellingLicenceApplicationsDatabaseFactory.CreateDefaultTestContext();
        _repository = new ExternalUserContextFlaRepository(
            _fellingLicenceApplicationsContext,
            _mockReferenceGenerator.Object,
            _mockReferenceRepository.Object);

        var options = new FellingLicenceApplicationOptions
        {
            PostFix = ReferenceSuffix
        };

        return new ImportApplicationsService(
            _repository,
            _mockAuditService.Object,
            _mockClock.Object,
            _mockBus.Object,
            new OptionsWrapper<FellingLicenceApplicationOptions>(options),
            new NullLogger<ImportApplicationsService>());
    }

    private ApplicationSource GenerateValidApplication(PropertyIds property)
    {
        if (property == null)
        {
            throw new ArgumentNullException(nameof(property));
        }

        return FixtureInstance.Build<ApplicationSource>()
            .With(x => x.Flov2PropertyName, property.Name.ToLower)
            .Create();
    }

    private List<ProposedFellingSource> GenerateValidFelling(
        PropertyIds property, 
        ApplicationSource application,
        int count = 3)
    {
        List<ProposedFellingSource> results = [];

        for (int i = 0; i < count; i++)
        {
            var compartment = property.CompartmentIds.RandomElement();
            var fellingOperation = FixtureInstance.Create<FellingOperationType>();

            while (fellingOperation == FellingOperationType.None
                || results.Any(x => x.Flov2CompartmentName == compartment.CompartmentName && x.OperationType == fellingOperation))
            {
                fellingOperation = FixtureInstance.Create<FellingOperationType>();
            }

            int? numberOfTrees = fellingOperation == FellingOperationType.FellingIndividualTrees
                ? FixtureInstance.Create<int>()
                : null;

            var isTpo = FixtureInstance.Create<bool>();
            var tpoRef = isTpo ? FixtureInstance.Create<string>() : null;

            var isCa = FixtureInstance.Create<bool>();
            var caRef = isCa ? FixtureInstance.Create<string>() : null;

            var isRestocking = fellingOperation != FellingOperationType.Thinning && FixtureInstance.Create<bool>();
            var noRestocking = isRestocking is true ? null : FixtureInstance.Create<string>();

            var species = string.Join(',', FixtureInstance.CreateMany<string>());

            var felling = FixtureInstance.Build<ProposedFellingSource>()
                .With(x => x.ApplicationId, application.ApplicationId)
                .With(x => x.Flov2CompartmentName, compartment.CompartmentName)
                .With(x => x.OperationType, fellingOperation)
                .With(x => x.NumberOfTrees, numberOfTrees)
                .With(x => x.IsPartOfTreePreservationOrder, isTpo)
                .With(x => x.TreePreservationOrderReference, tpoRef)
                .With(x => x.IsWithinConservationArea, isCa)
                .With(x => x.ConservationAreaReference, caRef)
                .With(x => x.IsRestocking)
                .With(x => x.NoRestockingReason, noRestocking)
                .With(x => x.Species, species)
                .Create();

            results.Add(felling);
        }

        return results;
    }

    private List<ProposedRestockingSource> GenerateValidRestocking(
        PropertyIds property,
        ApplicationSource application,
        ProposedFellingSource felling,
        int count = 3)
    {
        List<ProposedRestockingSource> results = [];

        var validRestockingTypes = felling.OperationType.AllowedRestockingForFellingType(false).ToList();
        validRestockingTypes.Remove(TypeOfProposal.DoNotIntendToRestock);

        if (count > validRestockingTypes.Count)
        {
            count = validRestockingTypes.Count;
        }

        for (int i = 0; i < count; i++)
        {
            var operation = validRestockingTypes.RandomElement();
            while (results.Any(x => x.RestockingProposal == operation))
            {
                operation = validRestockingTypes.RandomElement();
            }

            var compartment = operation.IsAlternativeCompartmentRestockingType()
                ? property.CompartmentIds.Where(x => x.CompartmentName != felling.Flov2CompartmentName).RandomElement()
                : null;

            double? density = operation.IsNumberOfTreesRestockingType()
                ? null
                : FixtureInstance.Create<double>();

            int? numberOfTrees = operation.IsNumberOfTreesRestockingType()
                ? FixtureInstance.Create<int>()
                : null;

            string[] speciesList = [
                FixtureInstance.Create<string>(),
                FixtureInstance.Create<int>().ToString(),
                FixtureInstance.Create<string>(),
                FixtureInstance.Create<int>().ToString(),
                FixtureInstance.Create<string>(),
                FixtureInstance.Create<int>().ToString()
            ];      
            var species = string.Join(',', speciesList);

            var restocking = FixtureInstance.Build<ProposedRestockingSource>()
                .With(x => x.ProposedFellingId, felling.ProposedFellingId)
                .With(x => x.RestockingProposal, operation)
                .With(x => x.Flov2CompartmentName, compartment?.CompartmentName)
                .With(x => x.SpeciesAndPercentages, species)
                .With(x => x.RestockingDensity, density)
                .With(x => x.NumberOfTrees, numberOfTrees)
                .Create();

            results.Add(restocking);
        }

        return results;
    }

    private void AssertApplication(
        FellingLicenceApplication applicationEntity,
        Guid woodlandOwnerId,
        PropertyIds property,
        ApplicationSource applicationToImport,
        string expectedReference,
        Guid userId,
        IEnumerable<ProposedFellingSource> fellingSource,
        IEnumerable<ProposedRestockingSource> restockingSource)
    {
        Assert.Equal(expectedReference, applicationEntity.ApplicationReference);
        Assert.Equal(woodlandOwnerId, applicationEntity.WoodlandOwnerId);
        Assert.Equal(property.Id, applicationEntity.LinkedPropertyProfile.PropertyProfileId);
        Assert.Equal(userId, applicationEntity.CreatedById);
        Assert.Equal(_now.ToDateTimeUtc(), applicationEntity.CreatedTimestamp);
        Assert.Equal(_now.ToDateTimeUtc(), applicationEntity.DateReceived);
        Assert.Equal(applicationToImport.ProposedFellingStart.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc), applicationEntity.ProposedFellingStart);
        Assert.Equal(applicationToImport.ProposedFellingEnd.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc), applicationEntity.ProposedFellingEnd);

        if (fellingSource.Any())
        {
            var fellingCpts = fellingSource.Select(x => x.Flov2CompartmentName).Distinct().Count();
            Assert.Equal(fellingCpts, applicationEntity.FellingLicenceApplicationStepStatus.CompartmentFellingRestockingStatuses.Count);

            foreach (var felling in fellingSource)
            {
                var fellingCpt = property.CompartmentIds.Single(x => x.CompartmentName == felling.Flov2CompartmentName);

                var compartmentStepStatus = applicationEntity.FellingLicenceApplicationStepStatus.CompartmentFellingRestockingStatuses
                    .SingleOrDefault(x => x.CompartmentId == fellingCpt.Id);
                Assert.NotNull(compartmentStepStatus);
                Assert.True(compartmentStepStatus.Status);

                var fellingEntity = applicationEntity.LinkedPropertyProfile.ProposedFellingDetails
                    .SingleOrDefault(x => x.PropertyProfileCompartmentId == fellingCpt.Id && x.OperationType == felling.OperationType);
                
                Assert.NotNull(fellingEntity);

                Assert.Equal(felling.AreaToBeFelled, fellingEntity.AreaToBeFelled);
                Assert.Equal(felling.NumberOfTrees, fellingEntity.NumberOfTrees);
                Assert.Equal(felling.IsPartOfTreePreservationOrder, fellingEntity.IsPartOfTreePreservationOrder);
                Assert.Equal(felling.IsWithinConservationArea, fellingEntity.IsWithinConservationArea);
                Assert.Equal(felling.TreeMarking, fellingEntity.TreeMarking);
                Assert.Equal(felling.EstimatedTotalFellingVolume, fellingEntity.EstimatedTotalFellingVolume);
                Assert.Equal(felling.Species, fellingEntity.FellingSpecies.Select(x => x.Species).ToDelimitedString());
                if (felling.OperationType == FellingOperationType.Thinning)
                {
                    Assert.Null(fellingEntity.IsRestocking);
                }
                else
                {
                    Assert.Equal(felling.IsRestocking, fellingEntity.IsRestocking);
                }

                if (fellingEntity.IsRestocking is false)
                {
                    Assert.Equal(felling.NoRestockingReason, fellingEntity.NoRestockingReason);
                }
                else
                {
                    Assert.Null(fellingEntity.NoRestockingReason);
                }
                
                Assert.Equal(felling.TreePreservationOrderReference, fellingEntity.TreePreservationOrderReference);
                Assert.Equal(felling.ConservationAreaReference, fellingEntity.ConservationAreaReference);
                Assert.Null(fellingEntity.FellingOutcomes);

                var fellingStepStatus = compartmentStepStatus.FellingStatuses.SingleOrDefault(x => x.Id == fellingEntity.Id);
                Assert.NotNull(fellingStepStatus);
                Assert.True(fellingStepStatus.Status);

                foreach (var restocking in
                         restockingSource.Where(x => x.ProposedFellingId == felling.ProposedFellingId))
                {
                    var proposal = restocking.RestockingProposal;

                    var restockingCpt = proposal.IsAlternativeCompartmentRestockingType()
                        ? property.CompartmentIds.SingleOrDefault(x => x.CompartmentName == restocking.Flov2CompartmentName)
                        : fellingCpt;

                    var restockingEntity = fellingEntity.ProposedRestockingDetails
                        .SingleOrDefault(x => x.RestockingProposal == restocking.RestockingProposal);

                    Assert.NotNull(restockingEntity);

                    Assert.Equal(restockingCpt.Id, restockingEntity.PropertyProfileCompartmentId);
                    Assert.Equal(restocking.AreaToBeRestocked, restockingEntity.Area);
                    Assert.Equal(restocking.RestockingProposal, restockingEntity.RestockingProposal);
                    if (proposal == TypeOfProposal.CreateDesignedOpenGround)
                    {
                        Assert.Null(restockingEntity.NumberOfTrees);
                        Assert.Null(restockingEntity.RestockingDensity);
                        Assert.Empty(restockingEntity.RestockingSpecies);
                    }
                    else
                    {
                        Assert.Equal(restocking.RestockingDensity, restockingEntity.RestockingDensity);
                        Assert.Equal(restocking.NumberOfTrees, restockingEntity.NumberOfTrees);
                        Assert.NotNull(restockingEntity.RestockingSpecies);
                        var expectedSpecies = restocking.SpeciesAndPercentages.Split(',');
                        var i = 0;
                        while (i < expectedSpecies.Length)
                        {
                            var species = expectedSpecies[i];
                            var percentage = int.Parse(expectedSpecies[i + 1]);
                            i += 2;
                            Assert.Contains(restockingEntity.RestockingSpecies,
                                x => x.Species == species && x.Percentage == percentage);
                        }
                    }

                    Assert.Null(restockingEntity.RestockingOutcomes);
                    
                    var restockingCompartmentStepStatus = fellingStepStatus.RestockingCompartmentStatuses
                        .SingleOrDefault(x => x.CompartmentId == restockingCpt.Id);
                    Assert.NotNull(restockingCompartmentStepStatus);
                    Assert.True(restockingCompartmentStepStatus.Status);

                    var restockingStepStatus = restockingCompartmentStepStatus.RestockingStatuses
                        .SingleOrDefault(x => x.Id == restockingEntity.Id);
                    Assert.NotNull(restockingStepStatus);
                    Assert.True(restockingStepStatus.Status);
                }
            }
        }
        else
        {
            Assert.Empty(applicationEntity.LinkedPropertyProfile.ProposedFellingDetails);
            Assert.Null(applicationEntity.FellingLicenceApplicationStepStatus.SelectCompartmentsStatus);
            Assert.Empty(applicationEntity.FellingLicenceApplicationStepStatus.CompartmentFellingRestockingStatuses);
        }
        
        Assert.True(applicationEntity.FellingLicenceApplicationStepStatus.ApplicationDetailsStatus);
        Assert.True(applicationEntity.FellingLicenceApplicationStepStatus.OperationsStatus);

        Assert.Single(applicationEntity.StatusHistories);
        Assert.Contains(applicationEntity.StatusHistories,
            x => x.Status == FellingLicenceStatus.Draft && x.Created == _now.ToDateTimeUtc());

        Assert.Single(applicationEntity.AssigneeHistories);
        Assert.Contains(applicationEntity.AssigneeHistories,
            x => x.AssignedUserId == userId && x.Role == AssignedUserRole.Author && x.TimestampAssigned == _now.ToDateTimeUtc());
    }

    private void VerifyAudit(Guid applicationId, Guid woodlandOwnerId, Guid userId)
    {
        _mockAuditService.Verify(x => x.PublishAuditEventAsync(
            It.Is<AuditEvent>(a => a.EventName == AuditEvents.CreateFellingLicenceApplication
                                   && a.UserId == userId
                                   && a.ActorType == Context.ActorType
                                   && a.CorrelationId == Context.RequestId
                                   && a.SourceEntityType == SourceEntityType.FellingLicenceApplication
                                   && a.SourceEntityId == applicationId
                                   && JsonSerializer.Serialize(a.AuditData, _serializerOptions) ==
                                   JsonSerializer.Serialize(new
                                   {
                                       WoodlandOwnerId = woodlandOwnerId
                                   }, _serializerOptions)
            ), It.IsAny<CancellationToken>()), Times.Once);
    }
}